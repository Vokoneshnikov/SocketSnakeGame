using System;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeGame.Client;

public sealed class GameStateSnapshotHandler : IClientCommandHandler
{
    private readonly ClientState _state;

    public GameStateSnapshotHandler(ClientState state)
    {
        _state = state;
    }

    public Task InvokeAsync(byte[] payload, CancellationToken ct = default)
    {
        using var reader = new PacketReader(payload);

        int tick = reader.ReadInt();

        lock (_state.SyncRoot)
        {
            _state.LastTick = tick;

            // игроки
            _state.Players.Clear();
            ushort playerCount = reader.ReadUShort();

            Console.WriteLine($"[Client] SNAPSHOT tick={tick}, players={playerCount}");

            for (int i = 0; i < playerCount; i++)
            {
                ushort playerId = reader.ReadUShort();
                bool isAlive = reader.ReadBool();
                float headX = reader.ReadFloat();
                float headY = reader.ReadFloat();
                float angle = reader.ReadFloat();
                int score = reader.ReadInt();

                ushort segmentCount = reader.ReadUShort();

                var p = new ClientPlayer
                {
                    Id = playerId,
                    IsAlive = isAlive,
                    Angle = angle,
                    Score = score
                };

                for (int s = 0; s < segmentCount; s++)
                {
                    float sx = reader.ReadFloat();
                    float sy = reader.ReadFloat();
                    p.Segments.Add((sx, sy));
                }

                _state.Players.Add(p);
            }

            // еда
            _state.Foods.Clear();
            ushort foodCount = reader.ReadUShort();
            Console.WriteLine($"[Client] foods={foodCount}");

            for (int i = 0; i < foodCount; i++)
            {
                int foodId = reader.ReadInt();
                float fx = reader.ReadFloat();
                float fy = reader.ReadFloat();

                _state.Foods.Add(new ClientFood
                {
                    Id = foodId,
                    X = fx,
                    Y = fy
                });
            }
        }

        return Task.CompletedTask;
    }
}
