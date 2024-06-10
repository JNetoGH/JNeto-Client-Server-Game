using System.Net;
using System.Net.Sockets;
using static JNeto_Server.ServerPacketHandlers;

namespace JNeto_Server;

public class Server
{
    public static int MaxPlayers { get; private set; }
    public static int ServerPort { get; private set; }
    
    /// <summary>
    /// Stores the GameClients as SeverClients using their IDs as keys.
    /// </summary>
    public static Dictionary<int, ServerClient> ServerClients { get; private set; } = new();
    
    /// <summary>
    /// Stores the PacketHandler delegate implementations using their IDs as key.
    /// </summary>
    public static Dictionary<int, PacketHandler> PacketHandlers { get; private set; }

    /// <summary>
    /// It is used to generate a socket connected to the GameClient to be passed to the ServerClient.
    /// </summary>
    /// <remarks>
    /// Used to create TCP server applications that listen for incoming connections.
    /// It encapsulates a Socket object configured for TCP and listens for incoming
    /// client connection requests on a specific IP address and port.
    /// </remarks>
    private static TcpListener _tcpListener;
    
    /// <summary>
    /// This UDP socket manages all UDP communications for the server.
    /// Once a UDP msg is received, the server will delegate its handling and response to the ServerClient with same ID.
    /// The UDP messages that arrive at the server, always follow this format including the clients ID:
    /// [Client ID - 4bytes] [Content Length - 4bytes] [Packet Handler ID - 4bytes] [value 1] [value 2] [value 3]..
    /// </summary>
    /// <remarks>
    /// This centralized approach was chosen, because typically only one UdpClient is used in servers,
    /// because giving each ServerClient a UdpClient socket can lead to issues with ports being closed.
    /// </remarks>
    private static UdpClient _udpSocket;

    /// <summary>
    /// Holds a string that represents the server's initial stats.
    /// </summary>
    private static string StartingStatus => 
        $"JNeto's Game Server Starting Status\n" +
        $"Port ---------- {ServerPort}\n" +
        $"Max Players --- {MaxPlayers}";
    
    /// <summary>
    /// Sets all dependencies to enable the sever to listen to incoming GameClients an start accepting them.
    /// </summary>
    /// <param name="maxPlayers">Max amount of clients.</param>
    /// <param name="port">The listener's port</param>
    public static void Start(int maxPlayers, int port)
    {
        Console.WriteLine("\nSetting JNeto's Game Server up...");
        
        // Setting Properties.
        MaxPlayers = maxPlayers;
        ServerPort = port;
        
        // Setting the ServerClients dictionary.
        ServerClients = new Dictionary<int, ServerClient>();
        InitializeServerData();
        
        // Setting the TPC listener to accept new GameClients.
        _tcpListener = new TcpListener(IPAddress.Any, ServerPort);
        _tcpListener.Start();
        
        // Setting the TPC listener to manage the clients UDP communications.
        _udpSocket = new UdpClient(ServerPort);
        _udpSocket.BeginReceive(new AsyncCallback(OnUdpReceiveCallback), null);
        
        // Starts asynchronously accepting new GameClients.
        // Requires a AsyncCallback(IAsyncResult) delegate to handle the connection in a discrete thread.
        _tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnTcpConnectCallback), null);
        
        // Printing the server's initial state.
        Console.WriteLine("\n" + StartingStatus);
    }

    /// <summary>
    /// Sets the dictionaries required by the Server.
    /// </summary>
    private static void InitializeServerData()
    {
        for (int i = 1; i <= MaxPlayers; i++)
            ServerClients.Add(i, new ServerClient(i));
        
        PacketHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ClientPacketsId.WelcomeResponse, WelcomeResponse },
            { (int)ClientPacketsId.PlayerMovement, PlayerMovement },
        };
        Console.WriteLine("Initialized packets.");
    }
    
    /// <summary>
    /// Invoked when a new GameClient connection is accepted by the server.
    /// Assigns the GameClient as a ServerClient to a free slot by passing the TcpClient socket, obtained from the listener.
    /// This socket will be used by the ServerClient to send and receive data to the GameClient in the game. 
    /// </summary>
    /// <remarks>
    /// This callback is called asynchronously, on a separate thread from the thread that initiated the accept operation.
    /// This allows the application to continue executing other code while waiting for new connections.
    /// </remarks>
    /// <param name="asyncResult">The result of the asynchronous operation.</param>
    private static void OnTcpConnectCallback(IAsyncResult asyncResult)
    {
        // Accepts the connection to the GameClient and gets a socket from the listener,
        // that is connected to the GameClient is the game.
        // Internally, the TcpClient class uses a Socket object to handle its communication with the server.
        TcpClient client = _tcpListener.EndAcceptTcpClient(asyncResult);
        
        // Once a GameClient connects, calls the method again keep listening to new GameClient connections asynchronously.
        _tcpListener.BeginAcceptTcpClient(OnTcpConnectCallback, null);
        Console.WriteLine($"\nIncoming connection from {client.Client.RemoteEndPoint}...");
        
        for (int i = 1; i <= MaxPlayers; i++)
        {
            // Slots are free when their TcpCommunicator.Socket is null.
            // These are only assigned when the ServerClient has been connected to its GameClient counter part.
            if (ServerClients[i].tcpCommunicator.socket == null)
            {
                // Connects the ServerClient to a free slot by passing the socket obtained from the listener.
                // From now on, this ServerClient socket is no longer null.
                ServerClients[i].tcpCommunicator.ConnectToGameClient(client);
                return;
            }
        }
        Console.WriteLine($"{client.Client.RemoteEndPoint} failed to connect: Server full!");
    }

    /// <summary>
    /// Asynchronously handles all UDP receiving messages.
    /// </summary>
    /// <param name="asyncResult"></param>
    private static void OnUdpReceiveCallback(IAsyncResult asyncResult)
    {
        try
        {
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            
            // This method returns all received bytes,
            // and will also set the endpoint to be the same as the on the data came from.
            byte[] data = _udpSocket.EndReceive(asyncResult, ref clientEndPoint);
            
            // Calls the method again keep receiving data from new GameClient. 
            _udpSocket.BeginReceive(OnUdpReceiveCallback, null);

            // Makes sure that there is an actual packet to handle.
            // they should at least have 4 bytes (an integer saying its content's length).
            if (data.Length < 4)
                return;

            using Packet packet = new Packet(data);
            int clientId = packet.ReadInt();

            // There can't be a 0 id, the dictionary's keys start at 1.
            if (clientId == 0)
                return;

            // If it is null, it means it hasn't been connected yet with UDP, and this packet is the empty one,
            // sent by the GameClient, at its connection methods to open up the local port.
            // Therefore, it must be connected but no data should be handled.
            if (ServerClients[clientId].udpCommunicator.endPoint == null)
            {
                ServerClients[clientId].udpCommunicator.ConnectToGameClient(clientEndPoint);
                return;
            }

            // SECURITY CHECK
            // Checks if the endpoint stored for the GameClient matches the endpoint where the packet came from
            // Without this check a hacker could theoretically pretend to be another GameClient by simply sending a
            // client Id than what belongs to them.
            if (ServerClients[clientId].udpCommunicator.endPoint.ToString().Equals(clientEndPoint.ToString()));
                ServerClients[clientId].udpCommunicator.HandleData(packet);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error receiving UDP data: {e}");
        }
    }

    /// <summary>
    /// Sends a packet in UDP to a specific GameClient endpoint.
    /// Called by the ServerClient.
    /// </summary>
    /// <param name="gameClientEndPoint">The GameClient to send the packet</param>
    /// <param name="packet">The packet to be sent on the network as a byte array.</param>
    public static void SendUdpDataToGameClient(IPEndPoint gameClientEndPoint, Packet packet)
    {
        try
        {
            if (gameClientEndPoint != null)
                _udpSocket.BeginSend(packet.ToArray(), packet.Length(), gameClientEndPoint, null, null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending data to {gameClientEndPoint} via UDP: {e}");
        }
    }
}