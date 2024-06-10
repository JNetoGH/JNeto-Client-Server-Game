using System.Net.Sockets;
using static JNeto_Server.ServerClient;

namespace JNeto_Server;

/// <summary>
/// JNeto's implementation of a TCP protocol handler class.
/// Manages connection, packet sending and received packet handling.
/// </summary>
public class ServerClientTcpCommunicator
{
	
	private readonly int _clientId;
	
	//  Stores the Tcp Client returned from the tpc listener.
	public TcpClient socket;

	// Provides a stream-based interface for reading from and writing to a network socket.
	private NetworkStream _stream;

	// The converted msg from array of bytes to a packet.
	private Packet _receivedData;
	
	// The received packet as an array of bytes.
	private byte[] _receiveBuffer;

	public ServerClientTcpCommunicator(int clientId)
	{
		this._clientId = clientId;
	}

	/// <summary>
	/// Starts to asynchronously receive packets from the GameClient as byte arrays,
	/// using the obtained socket's network stream.
	/// This same network stream will be used to send packets back to the ServerClient as byte arrays.
	/// </summary>
	/// <param name="socket">
	/// The socket used to get the network stream, is an instance of the newly connected client.
	/// This socket was set by the TCP listener that the game has connected.
	/// </param>
	public void ConnectToGameClient(TcpClient socket)
	{
		this.socket = socket;
		this.socket.ReceiveBufferSize = DataBufferSize;
		this.socket.SendBufferSize = DataBufferSize;

		_stream = this.socket.GetStream();

		_receivedData = new Packet();
		_receiveBuffer = new byte[DataBufferSize];

		_stream.BeginRead(_receiveBuffer, 0, DataBufferSize, OnReceiveTcpCallback, null);

		// Sends a welcome packet to the connected client and Logs it.
		string msg = $"Successfully connected the new GameClient (ip/port: {socket.Client.RemoteEndPoint}) to JNeto's Game Server using TCP.\n" +
		             $"The Generated ID is ({_clientId}), the server is waiting for the welcome reply with the client's username.";
		Console.WriteLine(msg);
		ServerPacketSender.Welcome(_clientId, msg);
	}

	/// <summary>
	/// Called by the Methods in the class ServerClientPacketSender.
	/// Uses the network stream, set by the connection method, to send packets as byte arrays to the GameClient in the game.
	/// </summary>
	/// <param name="packet">The packet to be sent on the network as a byte array.</param>
	public void SendDataToGameClient(Packet packet)
	{
		try
		{
			if (socket != null)
				_stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
		}
		catch (Exception e)
		{
			Console.WriteLine($"Error sending data to Game Client (id: {_clientId}) via TCP: \n{e}");
		}
	}
	
	/// <summary>
	/// Invoked when data is received from the GameClient.
	/// </summary>
	/// <remarks>
	/// This callback is called asynchronously, on a separate thread from the thread that initiated the read operation.
	/// This allows the application to continue executing other code while waiting for data to be received.
	/// </remarks>
	private void OnReceiveTcpCallback(IAsyncResult asyncResult)
	{
		try
		{
			int byteLength = _stream.EndRead(asyncResult);
			if (byteLength <= 0)
			{
				// TODO: disconnect
				return;
			}
			
			byte[] data = new byte[byteLength];
			Array.Copy(_receiveBuffer, data, byteLength);

			_receivedData.Reset(HandleData(data));
			_stream.BeginRead(_receiveBuffer, 0, DataBufferSize, OnReceiveTcpCallback, null);
		}
		catch (Exception e)
		{
			Console.WriteLine($"Error receiving TCP data: {e}");
			// TODO: disconnect
		}
	}

	/// <summary>
	/// Prepares the received data to be used by the appropriate PacketHandler methods.
	/// </summary>
	/// <param name="receivedData">The received data.</param>
	private bool HandleData(byte[] receivedData)
	{
		int packetLength = 0;

		_receivedData.SetBytes(receivedData);

		// Checks if has the content length, the first 4 bytes of the packet.
		if (_receivedData.UnreadLength() >= 4)
		{
			// The first chunk sent is an int (4 bytes) representing the length of the packet's content.
			// If the content length isn't bigger than 0, packet contains no data.
			packetLength = _receivedData.ReadInt();
			if (packetLength <= 0)
				return true;
		}

		// As long as this loop keeps running, it means the received data contains another complete packet that can be handled.
		// While packet contains data AND packet data length doesn't exceed the length of the packet we're reading.
		while (packetLength > 0 && packetLength <= _receivedData.UnreadLength())
		{
			// The new packet will have a size equal to the length of the content of the received packet.
			// So it won't have the content length, and it's no more needed.
			// [Packet Handler ID - int 4bytes] [value 1] [value 2] [value 3]...
			byte[] packetBytes = _receivedData.ReadBytes(packetLength);
			
			// Since the code of the Game Client runs asynchronously in discrete threads.
			// the ThreadManager class will ensure it runs in the main thread.
			ThreadManager.AddExecutionOnMainThread(() =>
			{
				using Packet packet = new Packet(packetBytes);
				int packetHandlerId = packet.ReadInt();
				Server.PacketHandlers[packetHandlerId](_clientId, packet);
			});

			// Resets the new packet length.
			packetLength = 0;
			
			// Checks if has the content length, the first 4 bytes of the packet.
			if (_receivedData.UnreadLength() >= 4)
			{
				// The first chunk sent is an int (4 bytes) representing the length of the packet's content.
				// If the content length isn't bigger than 0, packet contains no data.
				packetLength = _receivedData.ReadInt();
				if (packetLength <= 0)
					return true;
			}
		}

		// If it contains partial data from another packet, the _receivedPacket should not be reset.
		if (packetLength <= 1)
			return true;
		
		return false;
	}
}
