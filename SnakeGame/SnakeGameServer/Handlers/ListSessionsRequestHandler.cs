using System.Collections.Concurrent;
using System.Net.Sockets;
namespace SnakeGame.Handlers;

public sealed class ListSessionsRequestHandler : ICommandHandler
{
    public Task Invoke(
        ClientConnection sender,
        ConcurrentDictionary<Guid, GameSession> gameSessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        using var writer = new PacketWriter();

        // ограничим до ushort
        ushort count = (ushort)Math.Min(gameSessions.Count, ushort.MaxValue);
        writer.WriteUShort(count);

        int written = 0;

        foreach (var kvp in gameSessions)
        {
            if (written >= count)
                break;

            Guid sessionId = kvp.Key;
            GameSession session = kvp.Value;

            writer.WriteGuid(sessionId);
            writer.WriteUShort((ushort)session.Players.Count);

            written++;
        }

        byte[] packet = writer.BuildPacket(Command.ListSessionsResponse);
        return sender.Socket.SendAsync(packet, SocketFlags.None, ct).AsTask();
    }
}