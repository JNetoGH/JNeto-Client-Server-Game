using System.Numerics;

namespace JNeto_Server;

public class ServerPacketHandlers
{
    
    /// <summary>
    /// The delegate called to handle the received packet when the ServerClient receives a msg form the ServerClient.
    /// </summary>
    public delegate void PacketHandler(int fromClientId, Packet packet);
    
    /// <summary>
    /// What the ServerClient does once the GameClient responds the welcome packet sent.
    /// </summary>
    public static void WelcomeResponse(int fromClientId, Packet packet)
    {
        int clientIdCheck = packet.ReadInt();
        string username = packet.ReadString();

        // Assigns the username to the required target.
        Server.ServerClients[clientIdCheck].username = username;
        
        Console.WriteLine($"\nGame Client (username: {username}) (ID: {clientIdCheck})\n" +
                          $"responded the TCP welcome msg back with its username.");
        
        // This should NEVER happens unless there is something wrong with the code.
        if (fromClientId != clientIdCheck)
            Console.WriteLine($"Player \"{username}\" (ID: {fromClientId}) has assumed the wrong client ID ({clientIdCheck})!");
        
        Server.ServerClients[fromClientId].SendIntoGame(username);
    }

    /// <summary>
    /// What the ServerClient does once the GameClient sends movement.
    /// </summary>
    public static void PlayerMovement(int fromClientId, Packet packet)
    {
        bool[] inputs = new bool[packet.ReadInt()];
        for (int i = 0; i < inputs.Length; i++)
            inputs[i] = packet.ReadBool();
        
        Quaternion rotation = packet.ReadQuaternion();
        Server.ServerClients[fromClientId].player.SetInput(inputs, rotation);
    }
}