using SnakeGame;
public class Player
{
    public ushort Id { get; set; }
    public string Name { get; set; }

    public float Angle { get; set; }
    public float Speed { get; set; }

    public float MaxLength { get; set; }
    public float CurrentLength { get; set; }
    public bool IsAlive { get; set; } = true;
    public int Score { get; set; }

    public float SegmentRadius { get; set; } = 4f;
    public float SegmentSpacing { get; set; } = 6f;

    public List<WormSegment> Segments { get; } = new List<WormSegment>();
    
    public WormSegment Head => Segments.Count > 0 ? Segments[^1] : null;

    // новое:
    public bool PendingRespawn { get; set; }
    public float TimeSinceDeath { get; set; }
}