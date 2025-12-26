using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeGame.Client;

public sealed class ClientCommandDispatcher
{
    private readonly Dictionary<Command, IClientCommandHandler> _handlers = new();

    public ClientCommandDispatcher(ClientState state)
    {
        _handlers[Command.ServerHello]           = new ServerHelloHandler(state);
        _handlers[Command.CreateSessionResponse] = new CreateSessionResponseHandler(state);
        _handlers[Command.JoinSessionResponse]   = new JoinSessionResponseHandler(state);
        _handlers[Command.ListSessionsResponse]  = new ListSessionsResponseHandler(state);
        _handlers[Command.GameStateSnapshot]     = new GameStateSnapshotHandler(state);
    }

    public async Task DispatchAsync(
        Command command,
        byte[] payload,
        CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue(command, out var handler))
            return;

        await handler.InvokeAsync(payload, ct);
    }
}