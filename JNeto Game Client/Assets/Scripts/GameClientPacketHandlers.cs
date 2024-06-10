using System.Net;
using UnityEngine;

/// <summary>
/// Defines the implementations of the delegate called when receiving packets from its ServerClient.
/// </summary>
public class GameClientPacketHandlers : MonoBehaviour
{
    
    /// <summary>
    /// The delegate called to handle the received packet when the GameClient receives a msg form the ServerClient.
    /// </summary>
    public delegate void PacketHandler(Packet packet);
    
    /// <summary>
    /// - Logs the welcome msg and sends the client's ID.
    /// = Sends back a response, that sends the GameClient's username.
    /// - Initiates an UDP connection to the server.
    /// </summary>
    /// <param name="packet">the packet received from the network.</param>
    public static void Welcome(Packet packet)
    {
        string msg = packet.ReadString();
        int myId = packet.ReadInt();

        Debug.Log($"TCP message from server: {msg}");
        GameClient.Instance.clientId = myId;
        
        // Sends back a response to the JNeto Game server.
        GameClientPacketSender.WelcomeResponse();
        GameClient.Instance.tcpConnectionStatus = Color.green;
        
        // Initiates the UDP connection by passing the same part that the TcpCommunicator is using.
        GameClient.Instance.udpCommunicator.
            ConnectToServer(((IPEndPoint)GameClient.Instance.tcpCommunicator.Socket.Client.LocalEndPoint).Port);
        GameClient.Instance.udpConnectionStatus = Color.green;
    }

    /// <summary>
    /// Gathers the information of the player to be spawned, and spawns it.
    /// </summary>
    /// <param name="packet">the packet received from the network.</param>
    public static void SpawnPlayer(Packet packet)
    {
        int clientId = packet.ReadInt();
        string username = packet.ReadString();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();
        GameManager.instance.SpawnPlayer(clientId, username, position, rotation);
        Debug.Log($"TCP Spawning packet received from server: spawning client ID ({clientId})");
    }

    /// <summary>
    /// teleports the player to the given position.
    /// </summary>
    /// <param name="packet">the packet received from the network.</param>
    public static void PlayerPosition(Packet packet)
    {
        int id = packet.ReadInt();
        Vector3 position = packet.ReadVector3();
        GameManager.players[id].transform.position = position;
    }

    /// <summary>
    /// rotates the player to the given rotation.
    /// </summary>
    /// <param name="packet">the packet received from the network.</param>
    public static void PlayerRotation(Packet packet)
    {
        int id = packet.ReadInt();
        Quaternion rotation = packet.ReadQuaternion();
        GameManager.players[id].transform.rotation = rotation;
    }
    
}
