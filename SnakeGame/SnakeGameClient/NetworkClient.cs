using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeGame.Client;

public sealed class NetworkClient : IDisposable
{
    private readonly Socket _socket;
    private readonly ClientCommandDispatcher _dispatcher;
    private readonly byte[] _buffer = new byte[8192];
    private int _bytesInBuffer;

    public bool IsConnected => _socket.Connected;

    public NetworkClient(ClientCommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        Console.WriteLine($"[Client] Connecting to {host}:{port}...");
        await _socket.ConnectAsync(host, port, ct);
        Console.WriteLine("[Client] Connected (ConnectAsync)");

        // 1) ClientHello
        using (var w = new PacketWriter())
        {
            w.WriteByte(1);                 // protocol version
            w.WriteString("Player1");       // nickname
            byte[] pkt = w.BuildPacket(Command.ClientHello);

            Console.WriteLine("[Client] SEND ClientHello");

            await _socket.SendAsync(pkt, SocketFlags.None, ct);
        }

        // Пинг убран

        _ = ReceiveLoopAsync(ct);
    }


    public async Task SendPacketAsync(
        Command command,
        Action<PacketWriter>? payloadBuilder = null,
        CancellationToken ct = default)
    {
        using var writer = new PacketWriter();
        payloadBuilder?.Invoke(writer);
        byte[] packet = writer.BuildPacket(command);

        Console.WriteLine($"[Client] SEND {command} ({packet.Length} bytes): " +
                          BitConverter.ToString(packet));

        await _socket.SendAsync(packet, SocketFlags.None, ct);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        Console.WriteLine("[Client] ReceiveLoop started");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                int received = await _socket.ReceiveAsync(
                    new ArraySegment<byte>(_buffer, _bytesInBuffer, _buffer.Length - _bytesInBuffer),
                    SocketFlags.None,
                    ct);

                if (received == 0)
                {
                    Console.WriteLine("[Client] Socket closed by server");
                    break;
                }

                _bytesInBuffer += received;

                while (true)
                {
                    var result = PackageParser.TryParse(
                        _buffer.AsSpan(0, _bytesInBuffer),
                        out var command,
                        out var payload,
                        out var packetLength);

                    Console.WriteLine($"[Client] Parse result={result}, bytesInBuffer={_bytesInBuffer}, packetLength={packetLength}");

                    if (result == PackageParseResult.NotEnoughData)
                        break;

                    if (result != PackageParseResult.Ok || payload == null)
                    {
                        Console.WriteLine($"[Client] Parse error: result={result}, payloadNull={payload == null}");
                        Console.WriteLine("[Client] BUFFER DUMP: " +
                                          BitConverter.ToString(_buffer, 0, _bytesInBuffer));
                        _bytesInBuffer = 0;
                        break;
                    }

                    Console.WriteLine($"[Client] Received command={command}, payloadLength={payload.Length}");

                    try
                    {
                        await _dispatcher.DispatchAsync(command, payload, ct);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Client] Error in handler: " + ex);
                    }

                    int remaining = _bytesInBuffer - packetLength;
                    if (remaining > 0)
                        Buffer.BlockCopy(_buffer, packetLength, _buffer, 0, remaining);

                    _bytesInBuffer = remaining;

                    if (_bytesInBuffer == 0)
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Client] ReceiveLoop canceled");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Client] ReceiveLoop exception: " + ex);
        }
    }

    public void Dispose()
    {
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // ignore
        }

        _socket.Dispose();
    }
}
