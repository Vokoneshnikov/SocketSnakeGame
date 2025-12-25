using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SnakeGame;
using SnakeGame.Server;

internal class Program
{
    private static async Task Main()
    {
        var sessions = new ConcurrentDictionary<Guid, GameSession>();
        var sessionConnections = new ConcurrentDictionary<Guid, SessionConnections>();

        var dispatcher = new CommandDispatcher(sessionConnections);

        var server = new SocketServer(IPAddress.Any, 5000, dispatcher, sessions);
        var gameLoop = new GameLoop(sessions, sessionConnections);

        var cts = new CancellationTokenSource();

        Console.WriteLine("Snake server prepared. Press ENTER to start...");
        Console.ReadLine(); // Пауза перед запуском

        Console.WriteLine("Snake server starting...");

        await Task.WhenAll(
            server.StartAsync(cts.Token),
            gameLoop.RunAsync(cts.Token));
    }
}