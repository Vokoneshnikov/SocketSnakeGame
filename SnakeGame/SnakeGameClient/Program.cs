using System;
using System.Threading;
using System.Windows.Forms;
using SnakeGame.Client;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var state = new ClientState();
        var dispatcher = new ClientCommandDispatcher(state);
        var client = new NetworkClient(dispatcher);
        state.Network = client;

        var cts = new CancellationTokenSource();

        // Запускаем подключение (async) и сразу показываем форму
        _ = client.ConnectAsync("127.0.0.1", 5000, cts.Token);

        Application.Run(new GameForm(state));
        Console.WriteLine("Press ENTER to start server...");
        Console.ReadLine();

        cts.Cancel();
        client.Dispose();
    }
    
}