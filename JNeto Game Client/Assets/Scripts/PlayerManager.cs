using NaughtyAttributes;
using UnityEngine;


/// <summary>
/// Serves mostly as a holder for the player's client ID and username.
/// But also serves as a type, that can be used as the representation for all connected players.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [ReadOnly, SerializeField] private int _clientId;
    [ReadOnly, SerializeField] private string _username;

    public string Username
    {
        get => _username;
        set => _username = value;
    }

    public int ClientId
    {
        get => _clientId;
        set => _clientId = value;
    }
}
