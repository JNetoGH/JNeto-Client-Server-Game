namespace JNeto_Server;

/// <summary>
/// Launches the JNeto Game Server and the Game loop Thread
/// </summary>
class Program
{
    
    private const string WindowName = "JNeto Game Server";
    private const int MaxPlayers = 50;
    private const int ServerPort = 26950;
    
    // Game Loop
    private static bool isRunning = false;
    public const int UpdatesPerSec = 60;
    public const float MsPerTick = 1000f / UpdatesPerSec;
    
    static void Main(string[] args)
    {
        Console.Title = WindowName;
        isRunning = true;

        Thread gameLoopThread = new Thread(new ThreadStart(GameLoop));
        gameLoopThread.Start();

        // For testing any port is fine.
        // For releasing check unused ports at: https://en.wikipedia.org/wiki/List_of_TCP_and_UDP_port_numbers
        Server.Start(MaxPlayers, ServerPort);
    }

    private static void GameLoop()
    {
        Console.WriteLine($"Main thread started. Running at {UpdatesPerSec} ticks per second.");
        DateTime nextLoop = DateTime.Now;

        while (isRunning)
        {
            while (nextLoop < DateTime.Now)
            {
                foreach (ServerClient _client in Server.ServerClients.Values)
                    if (_client.player != null)
                        _client.player.Update();
                
                // Updates the actions in the thread manager.
                ThreadManager.UpdateMain();

                nextLoop = nextLoop.AddMilliseconds(MsPerTick);
                
                if (nextLoop > DateTime.Now)
                    Thread.Sleep(nextLoop - DateTime.Now);
            }
        }
    }
}