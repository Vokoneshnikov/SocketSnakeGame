using System.Collections.Generic;

namespace SnakeGame.Client;

public sealed class ClientPlayer
{
    public ushort Id { get; set; }
    public bool IsAlive { get; set; }
    public float Angle { get; set; }
    public int Score { get; set; }

    public List<(float X, float Y)> Segments { get; } = new();
}