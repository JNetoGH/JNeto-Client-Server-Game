namespace JNeto_Server;

/// <summary>
/// Sent from ServerClient to GameClient.
/// </summary>
public enum ServerPacketsId
{
	Welcome = 1,
	SpawnPlayer,
	PlayerPosition,
	PlayerRotation
}