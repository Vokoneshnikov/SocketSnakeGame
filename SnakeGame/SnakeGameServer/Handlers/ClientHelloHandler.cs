using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace SnakeGame.Handlers;

public sealed class ClientHelloHandler : ICommandHandler
{
    private const byte SupportedProtocolVersion = 1;

    public async Task Invoke(
        ClientConnection sender,
        ConcurrentDictionary<Guid, GameSession> gameSessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        Console.WriteLine("[Server] ClientHelloHandler.Invoke");

        if (payload == null)
        {
            Console.WriteLine("[Server] ClientHello payload is null");
            return;
        }

        Console.WriteLine("[Server] RAW ClientHello payload: " +
                          BitConverter.ToString(payload));

        using var reader = new PacketReader(payload);

        byte version = reader.ReadByte();
        string nickname = reader.ReadString();

        Console.WriteLine($"[Server] Parsed ClientHello: version={version}, nickname={nickname}");

        bool ok = version == SupportedProtocolVersion;

        using var writer = new PacketWriter();
        writer.WriteBool(ok);
        writer.WriteByte(SupportedProtocolVersion);
        writer.WriteUShort(0); // reserved

        byte[] packet = writer.BuildPacket(Command.ServerHello);

        Console.WriteLine("[Server] RAW SEND ServerHello: " +
                          BitConverter.ToString(packet));

        try
        {
            await sender.Socket.SendAsync(packet, SocketFlags.None, ct);
            Console.WriteLine("[Server] ServerHello sent");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Server] Error sending ServerHello: " + ex);
        }

        if (!ok)
        {
            Console.WriteLine("[Server] Protocol version mismatch (connection left open for debug)");
        }
        else
        {
            Console.WriteLine("[Server] ClientHello OK");
        }
    }
}