namespace JNeto_Server;

/// <summary>
/// Defines all the methods that sends packets to the GameClients through the network.
/// </summary
public class ServerPacketSender
{
    
    #region Packets
    
    /// <summary>
    /// Sends an welcome packet to a specific GameClient.
    /// </summary>
    /// <param name="targetClientId">The client id.</param>
    /// <param name="msg">The msg of the packet.</param>
    public static void Welcome(int targetClientId, string msg)
    {
        // This constructor creates a packet inserting at the start a 4-bytes int, representing its PacketHandler ID.
        using Packet packet = new Packet((int)ServerPacketsId.Welcome);
        packet.Write(msg);
        packet.Write(targetClientId);
        SendTcpData(targetClientId, packet);
    }
    
    /// <summary>
    /// Sends the information of the player to be spawned to a specific GameClient using TCP.
    /// This uses TCP because this is an important msg that is sent just once per player that needs to be spawned.
    /// So the server could not bet losing this packet.
    /// </summary>
    /// <param name="targetClientId">The one to send the packet.</param>
    /// <param name="player">The player object to be spawned.</param>
    public static void SpawnPlayer(int targetClientId, Player player)
    {
        // This constructor creates a packet inserting at the start a 4-bytes int, representing its PacketHandler ID.
        using Packet packet = new Packet((int)ServerPacketsId.SpawnPlayer);
        packet.Write(player.clientId);
        packet.Write(player.username);
        packet.Write(player.position);
        packet.Write(player.rotation);
        SendTcpData(targetClientId, packet);
    }

    public static void PlayerPosition(Player player)
    {
        // This constructor creates a packet inserting at the start a 4-bytes int, representing its PacketHandler ID.
        using Packet packet = new Packet((int)ServerPacketsId.PlayerPosition);
        packet.Write(player.clientId);
        packet.Write(player.position);
        SendUdpDataToAll(packet);
    }

    public static void PlayerRotation(Player player)
    {
        // This constructor creates a packet inserting at the start a 4-bytes int, representing its PacketHandler ID.
        using Packet packet = new Packet((int)ServerPacketsId.PlayerRotation);
        packet.Write(player.clientId);
        packet.Write(player.rotation);
        SendUdpDataToAll(player.clientId, packet);
    }
    
    #endregion

    /// <summary>
    /// Sends a packet to a specific GameClient, using TCP.
    /// </summary>
    /// <param name="targetClientId">The client to send the data.</param>
    /// <param name="packet">The data to be sent.</param>
    private static void SendTcpData(int targetClientId, Packet packet)
    {
        // Inserts the length of the packet's content (the byte list) at the start of the buffer.
        // It's an int with 4 bytes.
        packet.WriteLength();
        
        // Sends the packet (internally as byte array) to the GameClient.
        Server.ServerClients[targetClientId].tcpCommunicator.SendDataToGameClient(packet);
    }
    
    /// <summary>
    /// Sends a packet to a specific GameClient, using UDP.
    /// </summary>
    /// <param name="targetClientId">The client to send the data.</param>
    /// <param name="packet">The data to be sent.</param>
    private static void SendUdpData(int targetClientId, Packet packet)
    {
        // Inserts the length of the packet's content (the byte list) at the start of the buffer.
        // It's an int with 4 bytes.
        packet.WriteLength();
		
        // Sends the packet (internally as byte array) to the GameClient.
        Server.ServerClients[targetClientId].udpCommunicator.SendDataToGameClient(packet);
    }
    
    /// <summary>
    /// Sends a packet to all GameClients, using UDP.
    /// </summary>
    /// <param name="packet">The data to be sent.</param>
    private static void SendUdpDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
            Server.ServerClients[i].udpCommunicator.SendDataToGameClient(packet);
    }
    
    /// <summary>
    /// Sends a packet to a all GameClients except one, using UDP.
    /// </summary>
    /// <param name="exceptClient">The client to be excluded.</param>
    /// <param name="packet">The data to be sent.</param>
    private static void SendUdpDataToAll(int exceptClient, Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
            if (i != exceptClient)
                Server.ServerClients[i].udpCommunicator.SendDataToGameClient(packet);
    }
   
}