using System.Collections.Concurrent;
using System.Net.Sockets;

namespace SnakeGame.Handlers;

public sealed class PingHandler : ICommandHandler
{
    public async Task Invoke(
        ClientConnection sender,
        ConcurrentDictionary<Guid, GameSession> sessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        Console.WriteLine("[Server] Ping received");

        using var writer = new PacketWriter();
        writer.WriteInt(payload?.Length ?? 0); // просто возвращаем длину
        byte[] packet = writer.BuildPacket(Command.Pong);

        await sender.Socket.SendAsync(packet, SocketFlags.None, ct);
        Console.WriteLine("[Server] Pong sent");
    }
}