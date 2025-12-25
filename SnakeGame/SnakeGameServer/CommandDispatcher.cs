using System.Collections.Concurrent;
using SnakeGame.Handlers;
using SnakeGame.Server;
using SnakeGame.Server.Handlers;

namespace SnakeGame;

public sealed class CommandDispatcher
{
    private readonly Dictionary<Command, ICommandHandler> _handlers = new();

    public CommandDispatcher(ConcurrentDictionary<Guid, SessionConnections> sessionConnections)
    {
        _handlers[Command.ClientHello]          = new ClientHelloHandler();
        _handlers[Command.CreateSessionRequest] = new CreateSessionRequestHandler(sessionConnections);
        _handlers[Command.JoinSessionRequest]   = new JoinSessionRequestHandler(sessionConnections);
        _handlers[Command.ListSessionsRequest]  = new ListSessionsRequestHandler();
        _handlers[Command.LeaveSession]         = new LeaveSessionHandler();
        _handlers[Command.PlayerInput]          = new PlayerInputHandler();
        _handlers[Command.Disconnect]           = new DisconnectHandler();
    }

    public async Task DispatchAsync(
        ClientConnection sender,
        ConcurrentDictionary<Guid, GameSession> sessions,
        Command command,
        byte[] payload,
        CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue(command, out var handler))
            return;

        await handler.Invoke(sender, sessions, payload, ct);
    }
}