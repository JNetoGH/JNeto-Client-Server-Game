using UnityEngine;

/// <summary>
/// Defines all the methods that sends packets to the GameClients through the network.
/// </summary>
public class GameClientPacketSender : MonoBehaviour
{
    
    /// <summary>
    /// Sends a packet to the ServerClient, using TCP.
    /// </summary>
    private static void SendTcpDataToServerClient(Packet packet)
    {
        // Inserts the length of the packet's content (the a byte list) at the start of the buffer.
        // It's an int with 4 bytes.
        packet.WriteLength();
        // Sends the packet data (internally as a byte array) to the ServerClient.
        GameClient.Instance.tcpCommunicator.SendDataToServerClient(packet);
    }
    
    /// <summary>
    /// Sends a packet to the ServerClient, using UDP.
    /// </summary>
    private static void SendUdpDataToServerClient(Packet packet)
    {
        // Inserts the length of the packet's content (the a byte list) at the start of the buffer.
        // It's an int with 4 bytes.
        packet.WriteLength();
        // Sends the packet data (internally as a byte array) to the ServerClient.
        GameClient.Instance.udpCommunicator.SendDataSeverClient(packet);
    }

    #region Packets
    
    /// <summary>
    /// What this GameClient sends back to the ServerClient,
    /// once it has received the welcome packet (the one sent when they are both connected).
    /// </summary>
    public static void WelcomeResponse()
    {
        // Creates a new packet, then, insets the game client ID and username.
        // This constructor creates a packet inserting at the start a 4-bytes int, representing its PacketHandler ID.
        using Packet packet = new Packet((int)ClientPacketsId.WelcomeResponse);
        packet.Write(GameClient.Instance.clientId);
        packet.Write(GameClient.Instance.username);
        
        // Calls the default send method to send this new packet in TCP.
        SendTcpDataToServerClient(packet);
        
        Debug.Log($"This Game Client {GameClient.Instance}\n" +
                  "has received the TCP welcome packet and responded back with its username.");
    }

    /// <summary>
    /// Sends the player inputs to its ServerClient counterpart.
    /// </summary>
    /// <param name="inputs"></param>
    public static void PlayerMovement(bool[] inputs)
    {
        // This constructor creates a packet inserting at the start a 4-bytes int, representing its PacketHandler ID.
        using Packet packet = new Packet((int)ClientPacketsId.PlayerMovement);
        packet.Write(inputs.Length);
        foreach (bool input in inputs)
            packet.Write(input);
        packet.Write(GameManager.players[GameClient.Instance.clientId].transform.rotation);
        
        // Calls the default send method to send this new packet in TCP.
        SendUdpDataToServerClient(packet);
    }
    #endregion
}
