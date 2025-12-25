using System.Collections.Concurrent;
using System.Net.Sockets;

namespace SnakeGame.Handlers;

public sealed class DisconnectHandler : ICommandHandler
{
    public Task Invoke(
        ClientConnection sender,
        ConcurrentDictionary<Guid, GameSession> gameSessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        // По сути то же, что LeaveSession, плюс закрытие сокета

        if (sender.CurrentSessionId is not null && sender.PlayerId is not null)
        {
            var sessionId = sender.CurrentSessionId.Value;
            var playerId = sender.PlayerId.Value;

            if (gameSessions.TryGetValue(sessionId, out var session))
            {
                if (session.Players.Remove(playerId, out var player))
                {
                    session.World.Players.Remove(player);
                    // рассылка PlayerLeft при желании
                }

                if (session.Players.Count == 0)
                {
                    gameSessions.TryRemove(sessionId, out _);
                }
            }
        }

        sender.CurrentSessionId = null;
        sender.PlayerId = null;

        try
        {
            sender.Socket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // игнорируем, если уже закрыт
        }

        sender.Socket.Close();

        return Task.CompletedTask;
    }
}