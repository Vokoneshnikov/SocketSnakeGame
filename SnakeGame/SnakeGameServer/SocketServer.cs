namespace SnakeGame;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public sealed class SocketServer
{
    private readonly Socket _listener;
    private readonly CommandDispatcher _dispatcher;
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions;
    private readonly ConcurrentDictionary<Socket, ClientConnection> _connections = new();

    public SocketServer(IPAddress ipAddress, int port, CommandDispatcher dispatcher, ConcurrentDictionary<Guid, GameSession> sessions)
    {
        _dispatcher = dispatcher;
        _sessions = sessions;

        _listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(new IPEndPoint(ipAddress, port));
        _listener.Listen(backlog: 100);
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var clientSocket = await _listener.AcceptAsync(ct);

            var connection = new ClientConnection
            {
                ConnectionId = Guid.NewGuid(),
                Socket = clientSocket
            };

            _connections[clientSocket] = connection;

            _ = HandleClientAsync(connection, ct); // не ждём, обрабатываем параллельно
        }
    }

    private async Task HandleClientAsync(ClientConnection connection, CancellationToken ct)
{
    var socket = connection.Socket;
    var buffer = new byte[8192];
    int bytesInBuffer = 0;

    try
    {
        while (!ct.IsCancellationRequested)
        {
            int received = await socket.ReceiveAsync(
                new ArraySegment<byte>(buffer, bytesInBuffer, buffer.Length - bytesInBuffer),
                SocketFlags.None,
                ct);

            if (received == 0)
                break; // клиент отключился

            bytesInBuffer += received;

            // Пытаемся вытащить из буфера все целые пакеты
            while (true)
            {
                var result = PackageParser.TryParse(
                    buffer.AsSpan(0, bytesInBuffer),   // ВАЖНО: только заполненная часть буфера
                    out var command,
                    out var payload,
                    out var packetLength);

                if (result == PackageParseResult.NotEnoughData)
                    break;

                if (result != PackageParseResult.Ok || payload == null)
                {
                    Console.WriteLine($"[Server] Parse error: result={result}, payloadNull={payload == null}");
                    bytesInBuffer = 0;
                    break;
                }

                Console.WriteLine($"[Server] Received command={command}, payloadLength={payload.Length}");
                await _dispatcher.DispatchAsync(connection, _sessions, command, payload, ct);

                // сдвигаем оставшиеся данные
                int remaining = bytesInBuffer - packetLength;
                if (remaining > 0)
                    Buffer.BlockCopy(buffer, packetLength, buffer, 0, remaining);

                bytesInBuffer = remaining;

                if (bytesInBuffer == 0)
                    break;
            }
        }
    }
    finally
    {
        _connections.TryRemove(socket, out _);
        socket.Dispose();
        // здесь позже можно вызвать DisconnectHandler
    }
}
    

}
