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

    public Task InvokeAsync(byte[] payload, CancellationToken ct = default)
    {
        using var reader = new PacketReader(payload);

        bool success = reader.ReadBool();
        byte version = reader.ReadByte();
        ushort reservedPlayerId = reader.ReadUShort();

        _state.HandshakeOk = success;
        _state.ProtocolVersion = version;

        Console.WriteLine($"[Client] ServerHello: success={success}, version={version}");

        // дальше клиент остаётся в лобби и ждёт нажатия кнопок Create/Join
        return Task.CompletedTask;
    }
}