using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using SnakeGame;

namespace SnakeGame.Server;

public sealed class GameLoop
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions;
    private readonly ConcurrentDictionary<Guid, SessionConnections> _sessionConnections;
    private readonly Random _random = new();

    private int _tick;

    private const float EatRadius = 10f;
    private const float RespawnDelay = 3f;      // сек до респавна
    private const float CollisionRadius = 6f;   // радиус попадания головы в сегмент

    // Жёсткий максимум длины змеи (в тех же единицах, что MaxLength)
    private const float HardMaxLength = 2000f;

    // Жёсткий максимум количества еды, которая может выпасть с трупа
    private const int MaxFoodFromCorpse = 200;

    // Жёсткий потолок общего количества еды в мире
    private const int HardFoodLimit = 500;

    // Сколько сегментов максимум у игрока (для защиты от роста списка)
    private const int MaxSegmentsPerPlayer = 2000;

    // Сколько сегментов максимум отправляем в снапшоте на клиента
    private const int MaxSegmentsInSnapshot = 500;

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

        // 1. движение, стены, еда, респавн
        foreach (var player in world.Players)
        {
            // респавн после смерти
            if (!player.IsAlive && player.PendingRespawn)
            {
                player.TimeSinceDeath += dt;
                if (player.TimeSinceDeath >= RespawnDelay)
                {
                    RespawnPlayer(session, player);
                }
                continue;
            }

            if (!player.IsAlive || player.Segments.Count == 0)
                continue;

            var head = player.Head;
            float newX = head.X + (float)(Math.Cos(player.Angle) * player.Speed * dt);
            float newY = head.Y + (float)(Math.Sin(player.Angle) * player.Speed * dt);

            // коллизия со стенами
            if (newX < 0 || newX > world.Width || newY < 0 || newY > world.Height)
            {
                HandleDeath(world, player);
                continue;
            }

            // движение
            player.Segments.Add(new WormSegment { X = newX, Y = newY });

            // ограничиваем список сегментов жёстким пределом
            if (player.Segments.Count > MaxSegmentsPerPlayer)
            {
                int toRemove = player.Segments.Count - MaxSegmentsPerPlayer;
                player.Segments.RemoveRange(0, toRemove);
            }

            // поедание еды
            CheckFoodCollision(world, player, newX, newY);

            // длина и хвост
            player.CurrentLength = player.Segments.Count * player.SegmentSpacing;

            while (player.CurrentLength > player.MaxLength && player.Segments.Count > 1)
            {
                player.Segments.RemoveAt(0);
                player.CurrentLength = player.Segments.Count * player.SegmentSpacing;
            }
        }

        // 2. коллизии голов с сегментами других игроков (без своего тела)
        CheckPlayersCollision(world);

        // 3. спавн еды
        SpawnFoodIfNeeded(world);

        // 4. снапшот
        BroadcastSnapshot(session, tick);
    }

    private void CheckPlayersCollision(GameWorld world)
    {
        if (world.Players.Count < 2)
            return;

        float radius2 = CollisionRadius * CollisionRadius;

        var killed = new List<Player>();

        foreach (var player in world.Players)
        {
            if (!player.IsAlive || player.Segments.Count == 0)
                continue;

            var head = player.Head;
            float hx = head.X;
            float hy = head.Y;

            foreach (var other in world.Players)
            {
                if (other == player)
                    continue;

                // НЕ учитываем трупы как препятствия
                if (!other.IsAlive || other.Segments.Count == 0)
                    continue;

                for (int i = 0; i < other.Segments.Count; i++)
                {
                    var seg = other.Segments[i];

                    float dx = seg.X - hx;
                    float dy = seg.Y - hy;
                    float dist2 = dx * dx + dy * dy;

                    if (dist2 <= radius2)
                    {
                        killed.Add(player);
                        goto NextPlayer;
                    }
                }
            }

        NextPlayer:
            ;
        }

        foreach (var p in killed.Distinct())
        {
            HandleDeath(world, p);
        }
    }

    private void HandleDeath(GameWorld world, Player player)
    {
        if (!player.IsAlive)
            return;

        player.IsAlive = false;
        player.Speed = 0f;
        player.PendingRespawn = true;
        player.TimeSinceDeath = 0f;

        // сбрасываем счёт при смерти
        player.Score = 0;

        // Ограниченная "полоска" еды по телу
        int totalSegments = player.Segments.Count;
        if (totalSegments == 0)
            return;

        int foodSpawned = 0;

        // Если сегментов мало, можно брать каждый; если много — разреженно
        int step = totalSegments <= MaxFoodFromCorpse
            ? 1
            : Math.Max(1, totalSegments / MaxFoodFromCorpse);

        for (int i = 0; i < totalSegments && foodSpawned < MaxFoodFromCorpse; i += step)
        {
            var seg = player.Segments[i];

            int nextId = world.Foods.Count == 0
                ? 1
                : world.Foods[^1].Id + 1;

            world.Foods.Add(new Food
            {
                Id = nextId,
                X = seg.X,
                Y = seg.Y
            });

            foodSpawned++;
        }

        // после массового добавления следим, чтобы еды не стало слишком много
        if (world.Foods.Count > HardFoodLimit)
        {
            int toRemove = world.Foods.Count - HardFoodLimit;
            world.Foods.RemoveRange(0, toRemove);
        }
    }

    private void RespawnPlayer(GameSession session, Player player)
    {
        var world = session.World;

        player.PendingRespawn = false;
        player.TimeSinceDeath = 0f;
        player.IsAlive = true;

        // счёт уже обнулён в HandleDeath, при респавне его не трогаем

        player.MaxLength = 100f;
        player.CurrentLength = 0f;
        player.Speed =100;
        player.Angle = 0f;

        player.Segments.Clear();

        float margin = 20f; // отступ от стен, чтобы не появляться прямо у границы

        float spawnX = (float)_random.NextDouble() * (world.Width  - 2 * margin) + margin;
        float spawnY = (float)_random.NextDouble() * (world.Height - 2 * margin) + margin;

        player.Segments.Add(new WormSegment
        {
            X = spawnX,
            Y = spawnY
        });

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

                // очки всегда растут
                player.Score += 1;

                // длина растёт только пока не достигнут жёсткий максимум
                if (player.MaxLength < HardMaxLength)
                {
                    player.MaxLength = Math.Min(player.MaxLength + 20f, HardMaxLength);
                }
            }
        }
    }

    private void SpawnFoodIfNeeded(GameWorld world)
    {
        while (world.Foods.Count < world.MaxFoodCount)
        {
            int nextId = world.Foods.Count == 0
                ? 1
                : world.Foods[^1].Id + 1;

            var food = new Food
            {
                Id = nextId,
                X = (float)_random.NextDouble() * world.Width,
                Y = (float)_random.NextDouble() * world.Height
            };

            world.Foods.Add(food);
        }

        // Жёсткая подстраховка от переполнения еды
        if (world.Foods.Count > HardFoodLimit)
        {
            int toRemove = world.Foods.Count - HardFoodLimit;
            world.Foods.RemoveRange(0, toRemove);
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

            // Ограничиваем количество сегментов, отправляемых клиенту
            int realCount = p.Segments.Count;
            int limited = Math.Min(realCount, MaxSegmentsInSnapshot);
            ushort segmentCount = (ushort)limited;
            writer.WriteUShort(segmentCount);

            if (limited == 0)
                goto AfterSegments;

            int step = realCount <= limited
                ? 1
                : Math.Max(1, realCount / limited);

            int written = 0;
            for (int i = 0; i < realCount && written < limited; i += step, written++)
            {
                writer.WriteFloat(p.Segments[i].X);
                writer.WriteFloat(p.Segments[i].Y);
            }
            AfterSegments:
            ;
        }

        var foods = session.World.Foods;
        writer.WriteUShort((ushort)Math.Min(foods.Count, ushort.MaxValue));

        int foodsToSend = Math.Min(foods.Count, ushort.MaxValue);
        for (int i = 0; i < foodsToSend; i++)
        {
            var f = foods[i];
            writer.WriteInt(f.Id);
            writer.WriteFloat(f.X);
            writer.WriteFloat(f.Y);
        }

        byte[] packet = writer.BuildPacket(Command.GameStateSnapshot);

        if (!_sessionConnections.TryGetValue(session.SessionId, out var conns))
            return;

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
