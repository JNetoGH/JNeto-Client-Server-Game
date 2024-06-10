using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// JNeto's implementation of a UDP protocol handler class.
/// Manages connection, packet sending and received packet handling.
/// </summary>
public class GameClientUdpCommunicator
{
    
    /// <summary>
    /// Stores the UdpClient.
    /// </summary>
    public UdpClient Socket { get; private set; }
    
    /// <summary>
    /// End point to the server.
    /// Represents a network endpoint as an IP address and a port number.
    /// </summary>
    public IPEndPoint endPoint;

    public GameClientUdpCommunicator()
    {
        // Sets the end point to the server.
        endPoint = new IPEndPoint(IPAddress.Parse(GameClient.Instance.serverIp), GameClient.Instance.serverPort);
    }

    public void ConnectToServer(int localPort)
    {
        // Binds the UDP socket do the local port (the client's one).
        Socket = new UdpClient(localPort);
        Socket.Connect(endPoint);
        Socket.BeginReceive(OnReceiveUdpCallback, null);
        
        // This packet whole purpose is to initiate connection with the server, and to open up the local port,
        // so this GameClient can receive messages in UDP, the Send method will send this client's ID together.
        using Packet packet = new Packet();
        SendDataSeverClient(packet);
    }
    
    /// <summary>
    /// Called by the Methods in the class GameClientPacketSender.
    /// Uses the network stream, set by the connection methods, to send packets as byte arrays to the ServerClient in the server.
    /// </summary>
    /// <param name="packet">The packet to be sent on the network as a byte array.</param>
    public void SendDataSeverClient(Packet packet)
    {
        try
        {
            // Inserts the client's ID at the start of the packet
            // because in contrast with TCP in UDP you can't determinate who sent the msg.
            packet.InsertInt(GameClient.Instance.clientId);
            
            // Converts the packet to a byte array and sends it to the ServerClient in the server.
            // These Sockets are only assigned when the client has been connected.
            if (Socket != null)
                Socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
        }
        catch (Exception e)
        {
            Debug.Log($"Error sending data to server via UDP: {e}");
        }
    }

    /// <summary>
    /// Invoked when data is received by the game client.
    /// </summary>
    /// <remarks>
    /// This callback is called asynchronously, on a separate thread from the thread that initiated the read operation.
    /// This allows the application to continue executing other code while waiting for data to be received.
    /// </remarks>
    private void OnReceiveUdpCallback(IAsyncResult asyncResult)
    {
        try
        {
            // In case has received data, copy the received bytes into a new array.
            byte[] data = Socket.EndReceive(asyncResult, ref endPoint);
            
            // Calls the method again to keep getting data from the stream.
            Socket.BeginReceive(OnReceiveUdpCallback, null);
            
            // Makes sure that there is an actual packet to handle.
            // they should at least have 4 bytes (an integer saying its content's length).
            if (data.Length < 4)
                // Should not disconnect because losing data is somewhat common in UDP.
                return;
            
            // In contrast to TCP, UDP won't ever split a packet, so, all that checking done to TCP, isn't required.
            HandleData(data);
        }
        catch (Exception e)
        {
            // TODO: disconnect
        }
    }

    /// <summary>
    /// Prepares received data to be used by the appropriate packet handler methods.
    /// </summary>
    /// <param name="receivedData">The received data.</param>
    private void HandleData(byte[] receivedData)
    {
        // Once a read operation is made the position jumps the amount of bytes of the value read.
        
        // 1 - Creates a packet using the bytes array
        //        Read Pos ↓
        // [Content Length - 4bytes] [Packet Handler ID - 4bytes] [value 1] [value 2] [value 3]...
        using Packet packet = new Packet(receivedData);
        
        // 2 - Gets the content length and also moves over the first 4 bytes (content length as int).
        // The content length sent in the packet, does not count itself (int 4 bytes), only the rest.
        //                                    Read Pos ↓
        // [Content Length - int 4bytes] [Packet Handler ID - int 4bytes] [value 1] [value 2] [value 3]...
        int packetLength = packet.ReadInt();
        
        // 3 - Puts the rest of data into the byte array to be passed to the main thread using lambda.
        // The content length sent in the packet, does not count itself (int 4 bytes), only the rest, so it's left out.
        // [Packet Handler ID - int 4bytes] [value 1] [value 2] [value 3]...
        receivedData = packet.ReadBytes(packetLength);

        ThreadManager.AddExecutionOnMainThread(() =>
        {
            // Creates a new pure packet using the array with the Packet Handler id and values.
            //             Read Pos ↓
            // [Packet Handler ID - int 4bytes] [value 1] [value 2] [value 3]...
            using Packet newPacket = new Packet(receivedData);
            
            // 4 - Gets the PacketHandler ID and moves over the next 4 bytes (PacketHandler ID as int).
            //                             Read Pos ↓
            // [Packet Handler ID - int 4bytes] [value 1] [value 2] [value 3]...
            int packetHandlerId = newPacket.ReadInt();
            
            // 5 - Sends the packet to the handler with the read position on the values already.
            //                            Read Pos ↓
            // [Packet Handler ID - int 4bytes] [value 1] [value 2] [value 3]...
            GameClient.PacketHandlers[packetHandlerId](newPacket);
        });
    }
    
}