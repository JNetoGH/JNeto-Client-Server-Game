using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// JNeto's implementation of a TCP protocol handler class.
/// Manages connection, packet sending and received packet handling.
/// </summary>
public class GameClientTcpCommunicator
{
    
    /// <summary>
    /// Stores the TCP Client.
    /// </summary>
    public TcpClient Socket  { get; private set; }

    private NetworkStream _stream;
    private Packet _receivedData;
    private byte[] _receiveBuffer;

    /// <summary>
    /// Creates a socket using the server information in the GameClient class, in order to try to connect to the server.
    /// Once connected, starts to asynchronously receive packets as byte arrays from the ServerClient,
    /// using the socket's network stream.
    /// This same network stream will be used to send packets back to the ServerClient as byte arrays.
    /// </summary>
    public void ConnectToServer()
    {
        // Sets the socket to be connected to the JNeto Game Server.
        Socket = new TcpClient();
        Socket.ReceiveBufferSize = GameClient.DataBufferSize;
        Socket.SendBufferSize = GameClient.DataBufferSize;

        // Sets the byte array for the received data.
        _receiveBuffer = new byte[GameClient.DataBufferSize];
        
        // Tries to connect to the server.
        // Requires an AsyncCallback(IAsyncResult) delegate to handle the connection in a discrete thread.
        Socket.BeginConnect(GameClient.Instance.serverIp, GameClient.Instance.serverPort, OnConnectCallback, Socket);
    }
    
    /// <summary>
    /// Called by the Methods in the class GameClientPacketSender.
    /// Uses the network stream, set by the connection methods, to send packets as byte arrays to the ServerClient in the server.
    /// </summary>
    /// <param name="packet">The packet to be sent on the network as a byte array.</param>
    public void SendDataToServerClient(Packet packet)
    {
        try
        {
            // These Sockets are only assigned when the client has been connected.
            if (Socket != null)
                _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
        }
        catch (Exception e)
        {
            Debug.Log($"Error sending data to server via TCP: {e}");
        }
    }

    /// <summary>
    /// An AsyncCallback delegate that references the method to invoke when the operation is complete.
    /// </summary>
    /// <param name="asyncResult">The result of the asynchronous operation.</param>
    private void OnConnectCallback(IAsyncResult asyncResult)
    {
        Socket.EndConnect(asyncResult);

        // Checks if the client is in fact connected.
        if (!Socket.Connected)
            return;
        
        // Initializes the translated received packet holder.
        _receivedData = new Packet();
        
        // Gets the network stream from the socket.
        // Starts asynchronous reading the received data from the network stream.
        // Requires a AsyncCallback(IAsyncResult) delegate to handle the data reception in a discrete thread.
        _stream = Socket.GetStream();
        _stream.BeginRead(_receiveBuffer, 0, GameClient.DataBufferSize, OnReceiveDataCallback, null);
        Debug.Log($"The GameClient's TCP communicator's socket is now reading " +
                  $"from server (ip/port: {(IPEndPoint)Socket.Client.LocalEndPoint}), using TCP");
    }

    /// <summary>
    /// Invoked when data is received form the ServerClient.
    /// </summary>
    /// <remarks>
    /// This callback is called asynchronously, on a separate thread from the thread that initiated the read operation.
    /// This allows the application to continue executing other code while waiting for data to be received.
    /// </remarks>
    /// <param name="asyncResult">The result of the asynchronous operation.</param>
    private void OnReceiveDataCallback(IAsyncResult asyncResult)
    {
        try
        {
            // Number of bytes read from the stream.
            int byteLength = _stream.EndRead(asyncResult);
            
            bool hasReceivedData = byteLength > 0;
            if (!hasReceivedData)
                // TODO: disconnect the client
                return;  

            // In case has received data, copy the received bytes into a new array.
            byte[] data = new byte[byteLength];
            Array.Copy(_receiveBuffer, data, byteLength);

            // Handles the data received into a packet.
            // TPC is stream based, sends continuous flow of data in which all chunks of data are guaranteed to arrive,
            // and in the correct order, but these chunks aren't guaranteed to be delivered in one piece,
            // so before resetting the packet, it's necessary to make sure all data have been gathered,
            // including when it's split in more than one piece, in order to avoid losing data.
            bool resetPacket = HandleData(data);
            _receivedData.Reset(resetPacket);
            
            // Calls the method again to keep reading data from the stream (from the ServerClient).
            _stream.BeginRead(_receiveBuffer, 0, GameClient.DataBufferSize, OnReceiveDataCallback, null);
        }
        catch
        {
            // TODO: disconnect
        }
    }

    /// <summary>
    /// Prepares the received data to be used by the appropriate PacketHandler methods.
    /// </summary>
    /// <param name="receivedData">The received data.</param>
    private bool HandleData(byte[] data)
    {
        int packetLength = 0;

        _receivedData.SetBytes(data);

        if (_receivedData.UnreadLength() >= 4)
        {
            packetLength = _receivedData.ReadInt();
            if (packetLength <= 0)
                return true;
        }

        while (packetLength > 0 && packetLength <= _receivedData.UnreadLength())
        {
            byte[] packetBytes = _receivedData.ReadBytes(packetLength);
            ThreadManager.AddExecutionOnMainThread(() =>
            {
                using Packet packet = new Packet(packetBytes);
                int packetId = packet.ReadInt();
                GameClient.PacketHandlers[packetId](packet);
            });

            packetLength = 0;
            if (_receivedData.UnreadLength() >= 4)
            {
                packetLength = _receivedData.ReadInt();
                if (packetLength <= 0)
                    return true;
            }
        }

        if (packetLength <= 1)
            return true;
        
        return false;
    }
}
