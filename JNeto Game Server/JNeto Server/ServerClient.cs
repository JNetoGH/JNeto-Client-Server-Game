using System.Numerics;

namespace JNeto_Server;

/// <summary>
/// This class is the server's client version, which sends and receives packets to the GameClient in the game.
/// </summary>
public class ServerClient
{
    
    /// <summary>
    /// The size of the socket's receive and send buffers.
    /// 4 mb is mostly enough, the default was 8 mb.
    /// </summary>
    public const int DataBufferSize = 4096;
    
    public int clientId;
    public Player player;
    public string username = "undefined";
    
    /// <summary>
    /// The object holding the JNeto's TCP Communicator.
    /// Manages connection, packet sending and received packet handling between the ServerClient and GameClient.
    /// </summary>
    public ServerClientTcpCommunicator tcpCommunicator;
    
    /// <summary>
    /// The object holding the JNeto's UDP Communicator.
    /// Manages connection, packet sending and received packet handling between the ServerClient and GameClient.
    /// </summary>
    /// <remarks>
    /// Unlike the ServerClientTcpCommunicator, where each ServerClient gets it's own socket,
    /// the ServerClientUdpCommunicator uses a centralized UdpClient socket in the server.
    /// </remarks>
    public ServerClientUdpCommunicator udpCommunicator;

    public ServerClient(int clientClientId)
    {
        clientId = clientClientId;
        tcpCommunicator = new ServerClientTcpCommunicator(clientId);
        udpCommunicator = new ServerClientUdpCommunicator(clientId);
    }

    public override string ToString()
    {
        return $"(username: {username}) (ID: {clientId}) (ip/port {udpCommunicator.endPoint})";
    }
    
    /// <summary>
    /// Instantiates a the player object.
    /// Tells to every other connected GameClients that a new player has arrived.
    /// Sends the new player's information to every player (including himself).
    /// </summary>
    /// <param name="clientUsername"></param>
    public void SendIntoGame(string clientUsername)
    {
        player = new Player(clientId, clientUsername, new Vector3(0, 0, 0));
        Console.WriteLine($"\nSending TCP packets for spawning player: {ToString()}");
        
        // Tells to every other connected GameClients that a new player has arrived.
        foreach (ServerClient client in Server.ServerClients.Values)
        {
            if (client.player == null) 
                continue;

            if (client.clientId == clientId) 
                continue;
            
            ServerPacketSender.SpawnPlayer(clientId, client.player);
        }
        
        // Sends the new player's information to every player (including himself).
        foreach (ServerClient client in Server.ServerClients.Values)
        {
            if (client.player == null) 
                continue;
            
            // the parameters are passed in a different way than the one above.
            ServerPacketSender.SpawnPlayer(client.clientId, player);
        }
    }
    
}