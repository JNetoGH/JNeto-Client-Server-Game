
JNETO CLIENT-SERVER NETWORK SYSTEM <br>
João Neto, a22200558




# REPORT
this same report is also available in a much better pdf format:

[Project Report - JNeto Client Server.pdf](https://github.com/user-attachments/files/15755024/Project.Report.-.JNeto.Client.Server.pdf)




<br>

# INTRODUCTION
This system was an attempt to create a client-server architecture. The game runs on two ends: one on the server and the other on the game client. The game client was built using Unity, and the server was built using pure C# and the Sockets library.
I chose this approach instead of using a ready-made solution like Unity’s Netcode for GameObjects because it gives me, as a developer, more control over the data sent and the server itself, such as the size of the data array, the protocols used, and the server’s update rate.
The player's inputs are sent to the server in UDP, where they are processed in the server's game loop update logic. I have chosen this input-based approach for its simplicity, allowing me to focus more on building the server and less on the game itself.




<br>

# HOW TO RUN
First, run the server in a terminal using dotnet run or in a code editor. You should see a status message like this:

<img width="284" alt="Screenshot 2024-06-09 113544" src="https://github.com/JNetoGH/JNeto-Client-Server-Game/assets/24737993/387e4b63-108b-4115-85ae-053a1a910bc2">

The server is now open and listening for incoming TCP connections.

Next, run the game using Unity’s editor or a build. Insert a username and click the connect button. The game will connect to the server via TCP and send its username. If everything is alright, the game will then attempt to connect using UDP. The server’s console will print every step taken in the process. Pay special attention to the message indicating that a UDP packet has been sent from the game to the server. Once this message appears on the server, it means that the UDP connection has been successfully established.

<img width="305" alt="Screenshot 2024-06-09 113953" src="https://github.com/JNetoGH/JNeto-Client-Server-Game/assets/24737993/5f1b30f8-32f5-45b6-bd39-20b623da9228">
<img width="847" alt="Screenshot 2024-06-09 114547" src="https://github.com/JNetoGH/JNeto-Client-Server-Game/assets/24737993/a708461f-feea-4f98-8fb6-03b6cec4920e">

If you're using the Unity Editor, you can simply check the colors of each protocol status in the Inspector. Additionally, you can verify if the client ID and username have changed.

<img width="327" alt="Screenshot 2024-06-09 113222" src="https://github.com/JNetoGH/JNeto-Client-Server-Game/assets/24737993/e9b61556-51bc-47bc-ab63-0807db07a322">
<img width="325" alt="Screenshot 2024-06-09 115038" src="https://github.com/JNetoGH/JNeto-Client-Server-Game/assets/24737993/2bcce8c1-89f7-416d-b9a0-4e0d2a353da3">



<br>
<br>

# ARCHITECTURE

There are two versions of the clients: one in server (ServerClient) and the other in the game (GameClient), each version has a socket, and these sockets communicate with one another using packets. 
The connecting (except for the centralized UDP socket), sending, receiving, and handling of received data operations are all performed in the clients, both in the server and in the game, using their TcpCommunicator and UdpCommunicator, which handles the communication using sockets in order to traffic packets.
The server itself  has some other tasks:
- It handles connection acceptance using a TCP listener.
- It stores the GameClients as a ServerClient in a dictionary, using an ID as a key, by assigning them to a free slot and delegating their connection back to the ServerClient passing a socket connected to the GameClient.
- It also has a centralized socket for UDP packets (unlike TCP, where each ServerClient has its own TCP socket). 
The server delegates the handling and reply of the received packets to the ServerClient with the same ID as the one in the UDP packet, that’s why it waits for the TcpCommunicator connection, it establishes the IDs on both ends for each client.
The centralized socket is used by the ServerClients to send UDP packets to the GameClients. 
The GameClients send UDP packets to the centralized socket, which checks the ClientID in the UDP packet and delegates its handling and reply to the appropriate ServerClient.

_NOTE: This centralized approach was chosen, because typically only one UdpClient is used in servers, because giving each ServerClient a UdpClient socket can lead to issues with ports being closed._

![Network Diagram](https://github.com/JNetoGH/JNeto-Client-Server-Game/assets/24737993/a4afe99d-186b-4377-9c26-a305a6709a1f)




<br>

# PACKETS

The packets are built using a class created by Tom Weiland, and it presents some functionalities to make operations over a byte array, such as reading/writing values converting it from bytes or to bytes, and also maintains the tracking of a read position that jumps from data chunk to data chunk.
The packets are basically just a byte array, ( [chunk 0] [chunk 1] [chunk 2] ), therefore, they are highly controllable and easy to debug. For instance, here is a test before and after writing an integer:
NOTE: The length written is the content length and does not count itself (4 bytes), only the rest of the msg.
````
Debug.Log($"Length before the length insertion: {packet.Length()}bytes");
// Inserts the length of the packet's content (the byte list) at the start of the buffer. It's an int with 4 bytes.
packet.WriteLength();
Debug.Log($"Length after the length insertion: {packet.Length()}bytes");
````
Output: the content length will be 21 but the total packet length is 25.
<img width="404" alt="image" src="https://github.com/JNetoGH/JNeto-Client-Server-Game/assets/24737993/604700f8-2449-4141-8bd5-2b0d543bb437">




<br>

# THREADING AND PACKET EXECUTION

It’s a well-known fact in the game developer community that Unity doesn’t handle asynchronism well. However, almost every operation for connection, packet sending, receiving, and handling happens asynchronously, so, in order to create a working system, each version of the application, both on the server and the game, has a ThreadManager class. This class takes functions as callbacks to be executed on the Main Thread. In Unity, this is the regular Update of a MonoBehaviour, and on the server, it’s a in custom thread for the game loop (which can have its update rate customized):
````
private const int UpdatesPerSecond = 30;
private const int MsPerUpdate = 1000/UpdatesPerSecond;
public static void Main(string[] args)
{
   Thread t = new Thread(new ThreadStart(GameLoop));
   t.Start();
   Server.StartServer(MaxPlayers, ServerPort);
}

The packets contain a dictionary with implementations of the delegate void PacketHandler(Packet packet), which is the actual code executed for each packet. These handlers are then sent to the ThreadManager as function callbacks, and the ThreadManager ensures that these PacketHandler functions are executed on the Main Thread:
// Since the code of the Game Client runs asynchronously in discrete threads.
// the ThreadManager class will ensure it runs in the main thread.
ThreadManager.AddExecutionOnMainThread(() =>
{
   // Creates the new packet to be sent to the right PackHandler delegate.
   using Packet newPacket = new Packet(newPacketBytes);
   int packetHandlerId = newPacket.ReadInt();      
   Server.packetHandlers[packetHandlerId].Invoke(fromClientId: _id, packet: newPacket);
});
````




<br>

# CLIENT-SERVER TCP AND UDP CONNECTION 

1 - The server populates the ServerClients dictionary, generating an ID for each client, used as a key and then starts listening in TCP for client connections.

2 - The GameClient connect to the server using TCP,  by creating a Tcp socket, using the script bellow:
````
// Starts asynchronously reading the received data from socket's a network stream connected to the server. 
public void ConnectToServer()
{
   // Sets the socket to be connected to the JNeto Game Server.
   Socket = new TcpClient();
   Socket.ReceiveBufferSize = DataBufferSize;
   Socket.SendBufferSize = DataBufferSize;
   // Sets the byte array for the received data.
   _receivedBufferArray = new byte[DataBufferSize];
   // Tries to connect to the JNeto Game Server.
   // Requires an AsyncCallback(IAsyncResult) delegate to handle the connection in a discrete thread.
   Socket.BeginConnect(Instance.ServerIp, Instance.ServerPort, new AsyncCallback(OnConnectCallback), Socket);
}
````
3 - Once a GameClient connects to the server, the TCP Listener  in the server class,  generates a socket that is passed to the ServerClient, which sets a network stream to send and receive data to the GameClient.

4 - Once both ends can communicate, the ServerClient sends a welcome packet to the client version in the game waiting for a username in return. This Welcome packet contains the following format:

[Content Length - int 4bytes] [Packet Handler ID - int 4 bytes] [Client ID - int 4 bytes] [Txt Msg - string n bytes]

_NOTE: The length sent is the content length and does not count itself (4 bytes), only the rest of the msg._

_NOTE: The Txt Msg is meant to be printed in the client console._

5 - Once the packet is received,  GameClient stores its ID and sends its username back to the ServerClient, using a WelcomeResponse packet.

[Content Length - int 4bytes] [Packet Handler ID - int 4 bytes] [Username - string n bytes]

6 - The server registers the client username to its client version stored internally.

7 - The GameClient proceeds to create a socket to connect to the server using UDP and uses the TcpCommunicator to get its local port, all UDP packets sent from GameClients require their ID.

[Client ID - int 4 bytes] [Content Length - int 4bytes] [Packet Handler ID - int 4 bytes]

_NOTE: It’s done at this point because both the GameClient and the GameServer already have the same ID and can communicate using UDP safely, once the UDP connections can’t assure to keep ports open._

8 -  The server has a centralized socket for UDP packets (unlike for TCP where each ServerClient has its own TCP socket). The server delegates the handling and reply of the received packets to the ServerClient with the same ID as the one in the received UDP packet, and the ServerClients use the centralized socket in the server.

_NOTE: It’s done using the IDs to identify the GameClients (it’s validated by checking if the endpoints match)._

9 -  The Server uses the ServerClient with the same ID of the GameClient that has been just connected to the centralized UdpSocket,  to send it an UdpTest packet.




<br>

# SENDING PACKETS

The packet sending is pretty straight forward.
A method of the ServerPacketSender or GameClientPacketSender class is called, these methods always use a version of the Packet class constructor that takes an int and inserts it at the start of the packet, this int  represents its PacketHandler ID, and then these methods always call another method that writes in its beginning its content length and sends the message to the counterpart client using TCP or UDP.

_NOTE: The length written is the content length and does not count itself (4 bytes), only the rest of the msg._

_NOTE: If it's an UDP packet from the GameClient to the ServerClient, the UdpCommunicator will add the client ID, at the beginning of the packet before sending it._

For UDP only in the GameClients, and in TCP both GameClients and ServerClients, the same approach is used, each client’s Tcp/Udp Communicators have their own socket, which is used to send the packets to its counterpart. 

For UDP, only for  the ServerClients, they use a centralized method in the Server class that sends the packets Server.SendUdpData(endPoint, packet);, this method requires the endpoint, it indicates for what client the packet will  be sent,

Here is a summarized example for TCP:

````
/// <summary>
/// Defines all the methods that send packets to the ServerClients through the network.
/// </summary>
public class GameClientPacketSender : MonoBehaviour
{
   /// <summary>
   /// Sends a packet to the ServerClient, using TCP.
   /// </summary>
   private static void SendTcpDataToServerClient(Packet packet)
   {
       // Inserts the length of the packet's content (the a byte list) at the start of the buffer, it's an int with 4 bytes.
       packet.WriteLength();
       // Sends the packet data (internally as a byte array) to the ServerClient.
       GameClient.Instance.TcpCommunicator.SendDataToServerClient(packet);
   }
  
   /// <summary>
   /// What this GameClient sends back to the ServerClient,
   /// once it has received the welcome packet (the one sent when they are both connected).
   /// </summary>
   public static void WelcomeResponse()
   {
       // Packet is IDisposable, so it needs to be disposable manually or used in a "using" statement.
      // This constructor creates a packet inserting at the start a 4-bytes int, representing its PacketHandler ID.
       using Packet packet = new Packet((int) ClientPackets.welcomeReceived);
       // Inserts the game client ID.
       packet.Write(GameClient.Instance.ClientId);
       // Inserts this GameClient's username.
       packet.Write(GameClient.Instance.ClientUsername);
       // Calls the default send method to send this new packet.
       SendTcpDataToServerClient(packet);
   }
}
````




<br>

# RECEIVING AND HANDLING PACKETS

When a packet is received, both on the client and the server, for both UDP and TCP, it always follows a semi-rigid structure where the beginning is always the same, serving to identify the packet and its dependencies, the content length does not count itself (int - 4 bytes), being it only the rest of the msg.

The packet arrives as a byte array in TCP and in UDP (to the clients only) like this:
[Content Length - int 4bytes] [Packet Handler ID - int 4 bytes] [value 1] [value 2] [value 3] …

The packet arrives as a byte array in UDP ( to the ServerClient only)  like this:
 [Client ID - int 4 bytes] [Content Length - int 4bytes] [Packet Handler ID - int 4 bytes] [value 1] [value 2] [value 3] …
NOTE: The client ID is required to be sent to the ServerClient, because there's no way in UPD to know who sent it.

Upon arrival at the server or client, the packet's length is read and this information is used to test its integrity or to check if the beginning of another packet was sent along with it, next, the packet's PacketHandlerID is read. This ID identifies which implementation of the delegate 
void PacketHandler(Packet packet), defined at the GameClientReceptionHandler class, should be called to handle this packet. This check is done through an enumerator, e.g. Welcome, SpawnPlayer, etc…
The handling can be better explained in a summarized step by step approach, mind that, with each read operation, the packets read position is moved forward, according to the number of bytes of the value read:
NOTE: It’s almost the same for UDP packets received at the server,  but the packet structure can change a bit, because it must includes the clients ID, once there is no way to know who has sent it like in TCP, and you cannot make sure the data will always be at the same ports, but the packets arrives for handling with its reading position in front of these clients ID bytes, it’s used only previously to check the sender Identity.

1 - Create an intermediary packet with a the data received witch is (byte[] receivedData):
````
using Packet newPacket = new Packet(receivedData);
````
````
     Read Pos ↓
[Length - int 4bytes] [Packet Handler ID - int 4 bytes] [value 1] [value 2] [value 3] ...
````

<br>

2 - Read the length of the content, it does not count itself (4 bytes), it server for testing the packet integrity: 
````
int newPacketLength = newPacket.ReadInt();
````
````
                                    Read Pos ↓                                         
[Length - int 4bytes] [Packet Handler ID - int 4 bytes] [value 1] [value 2] [value 3] ...
````
<br>

3 - Set the received data array to be only the content, excluding the content length, it’s required because the original packet is an IDispoable and can’t be sent into a lambda, so the byte array is used to create a new one:
````
receivedData = newPacket.ReadBytes(newPacketLength);
````
````
[Packet Handler ID - int 4 bytes] [value 1] [value 2] [value 3] …
````

<br>

4 - Creates a new packet into the main thread to be sent to the PacketHandler(Packet packet) using the ID sent with the packet, by the time the packet is sent to the responsible delegate, its reading position is set to only the values sent, since the initial values are only necessary for the identification of the packet and its dependencies.:
````
ThreadManager.AddExecutionOnMainThread(() =>
{
   using Packet purePacket = new Packet(receivedData);
   int newPacketHandlerId = purePacket.ReadInt();
   _packetHandlers[newPacketHandlerId].Invoke(packet: purePacket);
});
````
````
                             Read Pos ↓                                               
[Packet Handler ID - int 4 bytes] [value 1] [value 2] [value 3] ...
````



<br>

# SCALING

In order to add a new packet to be sent, there are two classes, one for each client, GameClientPacketSender (showed before), and ServerPacketSender, they just write in a packet it’s content length, packet handler ID, and values, (if it is made in UDP the UdpCommunicator will write the client ID at the beginning). 
In order to add a new PacketHandler delegate implementation, they need to be assigned in the required target client’s dictionary with a packet handler ID, this ID is the one sent with the packet, the clients Tcp/Udp Communicators, will then check for the packet handler ID sent in the packet, and invoke the corresponding method from the dictionary, here is an example from the GameClient.
````
/// <summary>
/// Sets the PacketsHandlers dictionary.
/// </summary>
private void InitializeClientData()
{
   PacketHandlers = new Dictionary<int, GameClientReceptionHandler.PacketHandler>()
   {
       { (int) PacketHandlerIdServerClient.Welcome, GameClientReceptionHandler.Welcome },
       { (int) PacketHandlerIdServerClient.UdpTest, GameClientReceptionHandler.UdpTest },
   };
   Debug.Log("Initialized the packet handlers");
}
````




<br>

# REFERENCES

Multiplayer Paper: <br>
https://www.gabrielgambetta.com/client-server-game-architecture.html

Github Project that I first read: <br>
https://github.com/manlaig/basic_multiplayer_unity/tree/master

Kevin Kaymak’s youtube channel with a lot of networking stuff: <br>
https://www.youtube.com/@AndroidRetro/playlists

Tom Weiland’s youtube channel, which is also full with a lot of networking stuff: <br>
https://www.youtube.com/@tomweiland




<br>

# EXTRA: SECURITY CHECKS
````
// SECURITY CHECK
// Checks if the endpoint stored for the GameClient matches the endpoint where the packet came from
// Without this check a hacker could theoretically pretend to be another GameClient by simply sending a
// client Id than what belongs to them.
if (currentClient.UdpCommunicator.endPoint.ToString().Equals(gameClientEndPoint.ToString()))
{
   currentClient.UdpCommunicator.HandleData(receivedPacket);
}
````
