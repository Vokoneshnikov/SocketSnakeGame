namespace SnakeGame;

using System.IO;
using System.Text;

public sealed class PacketWriter : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly BinaryWriter _writer;

    public PacketWriter()
    {
        _stream = new MemoryStream();
        _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
    }

    // Работаем только с payload

    public void WriteByte(byte value)      => _writer.Write(value);
    public void WriteBool(bool value)      => _writer.Write(value);
    public void WriteInt(int value)        => _writer.Write(value);
    public void WriteUShort(ushort value)  => _writer.Write(value);
    public void WriteFloat(float value)    => _writer.Write(value);

    public void WriteGuid(Guid value)      => _writer.Write(value.ToByteArray());

    public void WriteString(string value)
    {
        if (value == null)
        {
            _writer.Write((ushort)0);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > ushort.MaxValue)
            throw new InvalidOperationException("String too long for this protocol.");

        _writer.Write((ushort)bytes.Length);
        _writer.Write(bytes);
    }

    public byte[] ToPayloadArray()
    {
        _writer.Flush();
        return _stream.ToArray();
    }

    /// <summary>
    /// Собирает полный пакет [Command][Length][Payload].
    /// </summary>
    public byte[] BuildPacket(Command command)
    {
        var payload = ToPayloadArray();
        if (payload.Length > ushort.MaxValue)
            throw new InvalidOperationException("Payload too large for this protocol.");

        var result = new byte[3 + payload.Length];
        result[0] = (byte)command;

        // Length (ushort, little-endian)
        ushort length = (ushort)payload.Length;
        result[1] = (byte)(length & 0xFF);
        result[2] = (byte)((length >> 8) & 0xFF);

        Buffer.BlockCopy(payload, 0, result, 3, payload.Length);
        return result;
    }

    public void Dispose()
    {
        _writer.Dispose();
        _stream.Dispose();
    }
}
