using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using static GameClientPacketHandlers;

/// <summary>
/// This class is the game's client version, which sends and receives packets to the ServerClient in the server.
/// </summary>
public class GameClient : MonoBehaviour
{
    /// <summary>
    /// Singleton implementation.
    /// </summary>
    public static GameClient Instance { get; private set; }
    
    [Header("Server")]
    public string serverIp = "127.0.0.1";
    public int serverPort = 26950;
    [ShowNonSerializedField] public const int DataBufferSize = 4096;
    
    [Header("Game Client")]
    [ReadOnly, SerializeField] public int clientId = 0;
    [ReadOnly, SerializeField] public string username = "undefined";

    [Header("Protocols")]
    [ReadOnly, SerializeField] public Color tcpConnectionStatus = Color.red;
    [ReadOnly, SerializeField] public Color udpConnectionStatus = Color.red;
    
    /// <summary>
    /// The object holding the JNeto's TCP Communicator, it enables many operations using a socket..
    /// Manages connection, packet sending and received packet handling between the ServerClient and GameClient.
    /// </summary>
    public GameClientTcpCommunicator tcpCommunicator;
    
    /// <summary>
    /// The object holding the JNeto's UDP Communicator, it enables many operations using a socket..
    /// Manages connection, packet sending and received packet handling between the ServerClient and GameClient.
    /// </summary>
    public GameClientUdpCommunicator udpCommunicator;
    
    /// <summary>
    /// Stores the packets IDs and their handlers.
    /// </summary>
    public static Dictionary<int, PacketHandler> PacketHandlers { get; private set; }
 
    private void Awake()
    {
        SetSingleton();
    }
    
    private void Start()
    {
        tcpCommunicator = new GameClientTcpCommunicator();
        udpCommunicator = new GameClientUdpCommunicator();
    }
    
    public override string ToString()
    {
        return $"(username: {username}) (ID: {clientId}) (ip/port {udpCommunicator.endPoint})";
    }
    
    /// <summary>
    /// Populates the singleton with an instance.
    /// </summary>
    private void SetSingleton()
    {
        if (Instance is null)
        {
            Debug.Log($"Set a new instance of {nameof(GameClient)} to the singleton.");
            Instance = this;
        }
        else
        {
            Debug.Log($"instance of {nameof(GameClient)} already set, destroying object.");
            Destroy(this);
        }
    }
    
    /// <summary>
    /// Called by the Ui Manager.
    /// Connects to the JNeto Game Server using the JNeto's protocol class.
    /// </summary>
    public void ConnectToJNetoGameServer()
    {
        InitializeClientData();
        tcpCommunicator.ConnectToServer();
    }
    
    /// <summary>
    /// Sets the PacketsHandlers dictionary.
    /// </summary>
    private void InitializeClientData()
    {
        PacketHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPacketsId.Welcome, Welcome },
            { (int)ServerPacketsId.SpawnPlayer, SpawnPlayer },
            { (int)ServerPacketsId.PlayerPosition, PlayerPosition },
            { (int)ServerPacketsId.PlayerRotation, PlayerRotation },
        };
        Debug.Log("Initialized packets.");
    }
}
