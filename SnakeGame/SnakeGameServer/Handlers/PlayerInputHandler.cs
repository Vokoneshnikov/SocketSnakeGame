using System.Collections.Concurrent;

namespace SnakeGame.Handlers;

public sealed class PlayerInputHandler : ICommandHandler
{
    public Task Invoke(
        ClientConnection sender,
        ConcurrentDictionary<Guid, GameSession> gameSessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        if (payload == null || sender.CurrentSessionId is null || sender.PlayerId is null)
            return Task.CompletedTask;

        using var reader = new PacketReader(payload);
        float angle = reader.ReadFloat();
        byte flags = reader.ReadByte(); // пока можно не использовать

        if (!gameSessions.TryGetValue(sender.CurrentSessionId.Value, out var session))
            return Task.CompletedTask;

        if (!session.Players.TryGetValue(sender.PlayerId.Value, out var player))
            return Task.CompletedTask;

        player.Angle = angle;
        // сюда же позже можно добавить обработку флагов (ускорение и т.д.)

        return Task.CompletedTask;
    }
}