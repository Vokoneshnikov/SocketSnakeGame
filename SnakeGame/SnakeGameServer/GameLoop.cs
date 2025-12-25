using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;

namespace SnakeGame.Server;

public sealed class GameLoop
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions;
    private readonly ConcurrentDictionary<Guid, SessionConnections> _sessionConnections;
    private readonly Random _random = new();
    private int _tick;

    // радиус поедания
    private const float EatRadius = 10f;

    public GameLoop(
        ConcurrentDictionary<Guid, GameSession> sessions,
        ConcurrentDictionary<Guid, SessionConnections> sessionConnections)
    {
        _sessions = sessions;
        _sessionConnections = sessionConnections;
    }

    public Task RunAsync(CancellationToken ct)
    {
        return Task.Run(async () =>
        {
            var sw = new Stopwatch();
            sw.Start();
            long last = sw.ElapsedMilliseconds;

            while (!ct.IsCancellationRequested)
            {
                long now = sw.ElapsedMilliseconds;
                float dt = (now - last) / 1000f;
                last = now;

                _tick++;

                foreach (var session in _sessions.Values)
                {
                    TickSession(session, dt, _tick);
                }

                await Task.Delay(50, ct); // ~20 тиков/сек
            }
        }, ct);
    }

    private void TickSession(GameSession session, float dt, int tick)
    {
        var world = session.World;

        foreach (var player in world.Players)
        {
            if (!player.IsAlive || player.Segments.Count == 0)
                continue;

            var head = player.Head;
            float newX = head.X + (float)(Math.Cos(player.Angle) * player.Speed * dt);
            float newY = head.Y + (float)(Math.Sin(player.Angle) * player.Speed * dt);

            // 1. коллизия со стенами
            if (newX < 0 || newX > world.Width || newY < 0 || newY > world.Height)
            {
                player.IsAlive = false;
                player.Speed = 0;
                continue;
            }

            // 2. движение
            player.Segments.Add(new WormSegment { X = newX, Y = newY });

            // 3. поедание еды
            CheckFoodCollision(world, player, newX, newY);

            // 4. длина и хвост
            player.CurrentLength = player.Segments.Count * player.SegmentSpacing;

            while (player.CurrentLength > player.MaxLength && player.Segments.Count > 1)
            {
                player.Segments.RemoveAt(0);
                player.CurrentLength = player.Segments.Count * player.SegmentSpacing;
            }

            // TODO: коллизии с телами
        }

        // 5. спавн еды по MaxFoodCount
        SpawnFoodIfNeeded(world);

        // BroadcastSnapshot(session, tick);
    }

    private void CheckFoodCollision(GameWorld world, Player player, float headX, float headY)
    {
        if (world.Foods.Count == 0)
            return;

        float eatR2 = EatRadius * EatRadius;

        for (int i = world.Foods.Count - 1; i >= 0; i--)
        {
            var food = world.Foods[i];

            float dx = food.X - headX;
            float dy = food.Y - headY;
            float dist2 = dx * dx + dy * dy;

            if (dist2 <= eatR2)
            {
                world.Foods.RemoveAt(i);

                player.MaxLength += 20f;
                player.Score += 1;
            }
        }
    }

    private void SpawnFoodIfNeeded(GameWorld world)
    {
        // используем MaxFoodCount из GameWorld
        while (world.Foods.Count < world.MaxFoodCount)
        {
            int nextId = world.Foods.Count == 0
                ? 1
                : world.Foods[^1].Id + 1; // можно заменить на свой генератор Id

            var food = new Food
            {
                Id = nextId,
                X = (float)_random.NextDouble() * world.Width,
                Y = (float)_random.NextDouble() * world.Height
            };

            world.Foods.Add(food);
        }
    }

    private async void BroadcastSnapshot(GameSession session, int tick)
    {
        using var writer = new PacketWriter();

        writer.WriteInt(tick);

        var players = session.World.Players;
        writer.WriteUShort((ushort)players.Count);

        foreach (var p in players)
        {
            writer.WriteUShort(p.Id);
            writer.WriteBool(p.IsAlive);

            var head = p.Head ?? new WormSegment { X = 0, Y = 0 };
            writer.WriteFloat(head.X);
            writer.WriteFloat(head.Y);

            writer.WriteFloat(p.Angle);
            writer.WriteInt(p.Score);

            ushort segmentCount = (ushort)Math.Min(p.Segments.Count, ushort.MaxValue);
            writer.WriteUShort(segmentCount);

            for (int i = 0; i < segmentCount; i++)
            {
                writer.WriteFloat(p.Segments[i].X);
                writer.WriteFloat(p.Segments[i].Y);
            }
        }

        var foods = session.World.Foods;
        writer.WriteUShort((ushort)foods.Count);

        foreach (var f in foods)
        {
            writer.WriteInt(f.Id);
            writer.WriteFloat(f.X);
            writer.WriteFloat(f.Y);
        }

        byte[] packet = writer.BuildPacket(Command.GameStateSnapshot);

        if (!_sessionConnections.TryGetValue(session.SessionId, out var conns))
        {
            
            Console.WriteLine($"[Server] No connections for session {session.SessionId}");
            return;
        }
        Console.WriteLine($"[Server] Broadcast tick={tick} to {conns.Connections.Count} connections");
        foreach (var conn in conns.Connections)
        {
            try
            {
                await conn.Socket.SendAsync(packet, SocketFlags.None);
            }
            catch
            {
                // TODO: убрать мёртвые подключения
            }
        }
    }
}
