namespace SnakeGame;

public sealed class SessionConnections
{
    public Guid SessionId { get; }
    public List<ClientConnection> Connections { get; } = new();

    public SessionConnections(Guid sessionId)
    {
        SessionId = sessionId;
    }
}