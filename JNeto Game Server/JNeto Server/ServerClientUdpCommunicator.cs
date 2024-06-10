using System.Net;

namespace JNeto_Server;

/// <summary>
/// JNeto's implementation of a UDP protocol handler class.
/// Manages connection, packet sending and received packet handling.
/// </summary>
/// <remarks>
/// These actions, except the received packet handling, are made using a centralized UdpClient socket in the server.
/// In the GameClient the communicator has its own socket just like for TCP.
/// </remarks>
public class ServerClientUdpCommunicator
{
	/// <summary>
	/// Endpoint to the GameClient.
	/// Represents a network endpoint as an IP address and a port number.
	/// </summary>
	public IPEndPoint endPoint;
	
	private readonly int _clientId;

	public ServerClientUdpCommunicator(int clientId)
	{
		this._clientId = clientId;
	}

	public void ConnectToGameClient(IPEndPoint ipEndPoint)
	{
		this.endPoint = ipEndPoint;
		ServerClient serverClient = Server.ServerClients[_clientId];
		Console.WriteLine($"\nSuccessfully established UDP connection to" +
		                  $" GameClient (ID: {_clientId}) (Ip/port: {ipEndPoint})");
	}

	public void SendDataToGameClient(Packet packet)
	{
		Server.SendUdpDataToGameClient(endPoint, packet);
	}

	public void HandleData(Packet packetData)
	{
		int packetLength = packetData.ReadInt();
		byte[] packetBytes = packetData.ReadBytes(packetLength);
		ThreadManager.AddExecutionOnMainThread(() =>
		{
			// Creates a new "pure packet" using the array with the Packet Handler id and values.
			//             Read Pos ↓
			// [Packet Handler ID - int 4bytes] [value 1] [value 2] [value 3]...
			using Packet packet = new Packet(packetBytes);
			int packetHandlerId = packet.ReadInt();
			Server.PacketHandlers[packetHandlerId](_clientId, packet);
		});
	}
}