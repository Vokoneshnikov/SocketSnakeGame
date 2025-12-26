using System;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeGame.Client;

public sealed class ListSessionsResponseHandler : IClientCommandHandler
{
    private readonly ClientState _state;

    public ListSessionsResponseHandler(ClientState state)
    {
        _state = state;
    }

    public async Task InvokeAsync(byte[] payload, CancellationToken ct = default)
    {
        using var reader = new PacketReader(payload);

        ushort count = reader.ReadUShort();
        Console.WriteLine($"[Client] ListSessionsResponse: {count} session(s)");

        Guid? firstSessionId = null;

        for (int i = 0; i < count; i++)
        {
            Guid sessionId = reader.ReadGuid();
            ushort players = reader.ReadUShort();
            Console.WriteLine($"  - {sessionId} (players: {players})");

            if (firstSessionId == null)
                firstSessionId = sessionId;
        }

        if (firstSessionId != null && _state.Network != null)
        {
            Console.WriteLine($"[Client] Auto-joining first session {firstSessionId}");

            await _state.Network.SendPacketAsync(
                Command.JoinSessionRequest,
                w => w.WriteGuid(firstSessionId.Value),
                ct);
        }
        else
        {
            Console.WriteLine("[Client] No sessions to join");
        }
    }
}