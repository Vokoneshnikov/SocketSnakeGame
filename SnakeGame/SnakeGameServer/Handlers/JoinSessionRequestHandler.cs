using System.Collections.Concurrent;
using System.Net.Sockets;
using SnakeGame.Server;

namespace SnakeGame.Handlers;

public sealed class JoinSessionRequestHandler : ICommandHandler
{
    private readonly ConcurrentDictionary<Guid, SessionConnections> _sessionConnections;

    public JoinSessionRequestHandler(ConcurrentDictionary<Guid, SessionConnections> sessionConnections)
    {
        _sessionConnections = sessionConnections;
    }

    public async Task Invoke(
        ClientConnection sender,
        ConcurrentDictionary<Guid, GameSession> gameSessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        if (payload == null || payload.Length < 16)
            return;

        using var reader = new PacketReader(payload);
        Guid sessionId = reader.ReadGuid();

        bool success = gameSessions.TryGetValue(sessionId, out var session);
        ushort playerId = 0;

        if (success && session is not null)
        {
            playerId = (ushort)(session.Players.Count + 1);

            var player = new Player
            {
                Id = playerId,
                Name = "Player" + playerId,
                Angle = 0f,
                Speed = 60f,
                MaxLength = 100f,
                CurrentLength = 0f,
                IsAlive = true
            };

            player.Segments.Add(new WormSegment
            {
                X = session.World.Width / 2f,
                Y = session.World.Height / 2f
            });

            session.Players[playerId] = player;
            session.World.Players.Add(player);

            sender.CurrentSessionId = sessionId;
            sender.PlayerId = playerId;

            var conns = _sessionConnections.GetOrAdd(sessionId, id => new SessionConnections(id));
            conns.Connections.Add(sender);
        }

        using var writer = new PacketWriter();
        writer.WriteBool(success);
        writer.WriteGuid(sessionId);
        writer.WriteUShort(playerId);

        byte[] packet = writer.BuildPacket(Command.JoinSessionResponse);
        await sender.Socket.SendAsync(packet, SocketFlags.None, ct);
    }
}
