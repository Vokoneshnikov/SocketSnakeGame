using System;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeGame.Client;

public sealed class ListSessionsResponseHandler : IClientCommandHandler
{
    public Task InvokeAsync(byte[] payload, CancellationToken ct = default)
    {
        using var reader = new PacketReader(payload);

        ushort count = reader.ReadUShort();
        Console.WriteLine($"[Client] ListSessionsResponse: {count} session(s)");

        for (int i = 0; i < count; i++)
        {
            Guid sessionId = reader.ReadGuid();
            ushort players = reader.ReadUShort();
            Console.WriteLine($"  - {sessionId} (players: {players})");
        }

        return Task.CompletedTask;
    }
}