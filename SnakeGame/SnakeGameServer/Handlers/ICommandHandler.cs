using System.Collections.Concurrent;

namespace SnakeGame.Handlers;

public interface ICommandHandler
{
    Task Invoke(
        ClientConnection sender,
        ConcurrentDictionary<Guid, GameSession> gameSessions,
        byte[]? payload = null,
        CancellationToken ct = default);
}
