using System.Net.Sockets;

namespace SnakeGame;

public class ClientConnection
{
    public Guid ConnectionId { get; init; }
    public Socket Socket { get; init; }

    public Guid? CurrentSessionId { get; set; }
    public ushort? PlayerId { get; set; }
}
