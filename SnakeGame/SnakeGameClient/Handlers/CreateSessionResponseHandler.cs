using System;
using System.Threading;
using System.Threading.Tasks;
using SnakeGame.Client;

namespace SnakeGame.Client;

public sealed class CreateSessionResponseHandler : IClientCommandHandler
{
    private readonly ClientState _state;

    public CreateSessionResponseHandler(ClientState state)
    {
        _state = state;
    }

    public Task InvokeAsync(byte[] payload, CancellationToken ct = default)
    {
        using var reader = new PacketReader(payload);

        bool success = reader.ReadBool();
        Guid sessionId = reader.ReadGuid();
        ushort playerId = reader.ReadUShort();

        Console.WriteLine($"[Client] CreateSessionResponse: success={success}, session={sessionId}, player={playerId}");

        if (success)
        {
            _state.CurrentSessionId = sessionId;
            _state.PlayerId = playerId;
        }

        return Task.CompletedTask;
    }
}