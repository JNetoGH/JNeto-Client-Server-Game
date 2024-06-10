using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [SerializeField] private GameObject _startMenu;
    [SerializeField] private TMP_InputField _usernameField;
    [SerializeField] private Button _connectButton;

    private void Awake()
    {
        SetSingleton();
    }

    private void Start()
    {
        AssignConnectButtonEvent();
    }

    private void AssignConnectButtonEvent()
    {
        _connectButton.onClick.AddListener(ConnectToJNetoGameServer);
    }
    
    /// <summary>
    /// Called when the player clicks on the connect button.
    /// </summary>
    public void ConnectToJNetoGameServer()
    {
        _startMenu.SetActive(false);
        _usernameField.interactable = false;
        GameClient.Instance.username = _usernameField.text;
        GameClient.Instance.ConnectToJNetoGameServer();
    }
    
    /// <summary>
    /// Populates the singleton with an instance.
    /// </summary>
    private void SetSingleton()
    {
        if (Instance is null)
        {
            Debug.Log($"Set a new instance of {nameof(UiManager)} to the singleton.");
            Instance = this;
        }
        else
        {
            Debug.Log($"instance of {nameof(UiManager)} already set, destroying object.");
            Destroy(this);
        }
    }
}
