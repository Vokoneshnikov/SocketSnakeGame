namespace SnakeGame;

public static class PackageParser
{
    public static PackageParseResult TryParse(
        ReadOnlySpan<byte> data,
        out Command command,
        out byte[]? payload,
        out int totalPacketLength) // сколько байт занимает весь пакет
    {
        command = default;
        payload = null;
        totalPacketLength = 0;

        // Нужно минимум 3 байта: Command (1) + Length (2)
        if (data.Length < 3)
            return PackageParseResult.NotEnoughData;

        var cmdByte = data[0];

        // Проверяем, что это допустимое значение enum Command
        if (!Enum.IsDefined(typeof(Command), (byte)cmdByte))
            return PackageParseResult.InvalidCommand;

        command = (Command)cmdByte;

        // Читаем длину payload (ushort, little-endian)
        ushort length = (ushort)(data[1] | (data[2] << 8));

        totalPacketLength = 3 + length;

        // В буфере пока недостаточно данных для всего пакета
        if (data.Length < totalPacketLength)
            return PackageParseResult.NotEnoughData;

        // Payload идёт сразу после заголовка
        payload = data.Slice(3, length).ToArray();

        return PackageParseResult.Ok;
    }
}