namespace SnakeGame;

using System.IO;
using System.Text;

public sealed class PacketReader : IDisposable
{
    private readonly BinaryReader _reader;

    public PacketReader(byte[] payload)
    {
        _reader = new BinaryReader(new MemoryStream(payload), Encoding.UTF8, leaveOpen: false);
    }

    public byte ReadByte()        => _reader.ReadByte();
    public bool ReadBool()        => _reader.ReadBoolean();
    public int ReadInt()          => _reader.ReadInt32();
    public ushort ReadUShort()    => _reader.ReadUInt16();
    public float ReadFloat()      => _reader.ReadSingle();

    public Guid ReadGuid()
    {
        var bytes = _reader.ReadBytes(16);
        return new Guid(bytes);
    }

    public string ReadString()
    {
        ushort length = _reader.ReadUInt16();
        if (length == 0)
            return string.Empty;

        var bytes = _reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}
