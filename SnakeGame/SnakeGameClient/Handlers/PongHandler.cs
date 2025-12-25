using System;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeGame.Client;

public sealed class PongHandler : IClientCommandHandler
{
    public Task InvokeAsync(byte[] payload, CancellationToken ct = default)
    {
        using var reader = new PacketReader(payload);
        int len = reader.ReadInt();
        Console.WriteLine($"[Client] Pong received, payloadLen={len}");
        return Task.CompletedTask;
    }
}