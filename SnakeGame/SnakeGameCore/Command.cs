namespace SnakeGame;

public enum Command : byte
{
    // Подключение / общее
    ClientHello          = 0x01, 
    ServerHello          = 0x02, 
    Disconnect           = 0x03, 

    // Лобби / сессии
    ListSessionsRequest  = 0x10, // клиент просит список сессий
    ListSessionsResponse = 0x11, // сервер отдаёт список сессий
    CreateSessionRequest = 0x12, 
    CreateSessionResponse= 0x13, 
    JoinSessionRequest   = 0x14, 
    JoinSessionResponse  = 0x15, 
    LeaveSession         = 0x16, 
    SessionClosed        = 0x17, 

    // Игровой ввод
    PlayerInput          = 0x20, 

    // Состояние мира
    GameStateSnapshot    = 0x30, 

    // Игровые события
    PlayerJoined         = 0x40, 
    PlayerLeft           = 0x41,
    PlayerDied           = 0x42,
    
    // Тестовые
    Ping                = 0xF0,
    Pong                = 0xF1,

}
