using System;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeGame.Client;

public sealed class ServerHelloHandler : IClientCommandHandler
{
    private readonly ClientState _state;

    public ServerHelloHandler(ClientState state)
    {
        _state = state;
    }

    public async Task InvokeAsync(byte[] payload, CancellationToken ct = default)
    {
        using var reader = new PacketReader(payload);

        bool success = reader.ReadBool();
        byte version = reader.ReadByte();
        ushort reservedPlayerId = reader.ReadUShort();

        _state.HandshakeOk = success;
        _state.ProtocolVersion = version;

        Console.WriteLine($"[Client] ServerHello: success={success}, version={version}");

        // Если рукопожатие прошло, сразу просим сервер создать игровую сессию
        if (success && _state.Network != null)
        {
            Console.WriteLine("[Client] Sending CreateSessionRequest after ServerHello");

            await _state.Network.SendPacketAsync(
                Command.CreateSessionRequest,
                w =>
                {
                    // служебный байт, чтобы не ломать серверный обработчик
                    w.WriteByte(0);
                },
                ct);
        }
    }
}