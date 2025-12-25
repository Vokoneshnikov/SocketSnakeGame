using System.Collections.Concurrent;
using System.Net.Sockets;
using SnakeGame.Handlers;

namespace SnakeGame.Server.Handlers;

public sealed class CreateSessionRequestHandler : ICommandHandler
{
    private readonly ConcurrentDictionary<Guid, SessionConnections> _sessionConnections;

    public CreateSessionRequestHandler(ConcurrentDictionary<Guid, SessionConnections> sessionConnections)
    {
        _sessionConnections = sessionConnections;
    }

    public async Task Invoke(
        ClientConnection sender,
        ConcurrentDictionary<Guid, GameSession> gameSessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        Console.WriteLine("[Server] CreateSessionRequest from " + sender.ConnectionId);

        // безопасно игнорируем служебный байт, если он есть
        if (payload is { Length: > 0 })
        {
            using var tmpReader = new PacketReader(payload);
            _ = tmpReader.ReadByte();
        }

        var sessionId = Guid.NewGuid();
        var session = new GameSession(sessionId, width: 1000f, height: 1000f);

        bool added = gameSessions.TryAdd(sessionId, session);
        Console.WriteLine($"[Server] session added={added}, id={sessionId}");

        bool success = added;
        ushort playerId = 0;

        if (added)
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
            Console.WriteLine($"[Server]  player {playerId} added, connections={conns.Connections.Count}");
        }

        using var writer = new PacketWriter();
        writer.WriteBool(success);
        writer.WriteGuid(sessionId);
        writer.WriteUShort(playerId);

        byte[] packet = writer.BuildPacket(Command.CreateSessionResponse);
        await sender.Socket.SendAsync(packet, SocketFlags.None, ct);
    }

}
