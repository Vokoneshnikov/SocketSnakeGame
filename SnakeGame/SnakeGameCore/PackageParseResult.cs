namespace SnakeGame;

public enum PackageParseResult
{
    Ok,
    NotEnoughData,      // данных в буфере пока мало, нужно дочитать
    InvalidCommand,     // байт команды не соответствует enum
    InvalidLength       // длина не согласуется с размером буфера
}