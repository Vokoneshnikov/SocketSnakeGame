namespace SnakeGame;

public class GameSession
{
    public Guid SessionId { get; }
    public string Name { get; set; }

    public GameWorld World { get; }

    public Dictionary<ushort, Player> Players { get; } = new();

    public GameSession(Guid sessionId, float width, float height)
    {
        SessionId = sessionId;
        World = new GameWorld(width, height);
        Name = sessionId.ToString();
    }
}