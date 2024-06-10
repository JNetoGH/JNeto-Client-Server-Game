using System.Numerics;

namespace JNeto_Server;


/// <summary>
/// The serve's logical representation od the Player.
/// </summary>
public class Player
{
    public int clientId;
    public string username;

    public Vector3 position;
    public Quaternion rotation;

    private float moveSpeed = 5f / Program.UpdatesPerSec;
    private bool[] inputs;

    public Player(int id, string username, Vector3 spawnPosition)
    {
        this.clientId = id;
        this.username = username;
        this.position = spawnPosition;
        this.rotation = Quaternion.Identity;
        this.inputs = new bool[4];
    }

    public void Update()
    {
        Vector2 inputDirection = Vector2.Zero;
        if (inputs[0])
            inputDirection.Y += 1;
        if (inputs[1])
            inputDirection.Y -= 1;
        if (inputs[2])
            inputDirection.X += 1;
        if (inputs[3])
            inputDirection.X -= 1;
        Move(inputDirection);
    }

    private void Move(Vector2 inputDirection)
    {
        Vector3 forward = Vector3.Transform(new Vector3(0, 0, 1), rotation);
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));

        Vector3 moveDirection = right * inputDirection.X + forward * inputDirection.Y;
        position += moveDirection * moveSpeed;

        ServerPacketSender.PlayerPosition(this);
        ServerPacketSender.PlayerRotation(this);
    }

    public void SetInput(bool[] inputs, Quaternion rotation)
    {
        this.inputs = inputs;
        this.rotation = rotation;
    }
}