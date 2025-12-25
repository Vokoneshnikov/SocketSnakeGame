using System.Collections.Concurrent;

namespace SnakeGame.Handlers;

public sealed class LeaveSessionHandler : ICommandHandler
{
    public Task Invoke(
        ClientConnection sender,
        ConcurrentDictionary<Guid, GameSession> gameSessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        if (sender.CurrentSessionId is null || sender.PlayerId is null)
            return Task.CompletedTask;

        var sessionId = sender.CurrentSessionId.Value;
        var playerId = sender.PlayerId.Value;

        if (gameSessions.TryGetValue(sessionId, out var session))
        {
            if (session.Players.Remove(playerId, out var player))
            {
                session.World.Players.Remove(player);
                // здесь можно разослать PlayerLeft другим игрокам
            }

            // если сессия пустая — по желанию можно удалить её
            if (session.Players.Count == 0)
            {
                gameSessions.TryRemove(sessionId, out _);
                // и отправить SessionClosed оставшимся, если бы они были
            }
        }

        sender.CurrentSessionId = null;
        sender.PlayerId = null;

        return Task.CompletedTask;
    }
}