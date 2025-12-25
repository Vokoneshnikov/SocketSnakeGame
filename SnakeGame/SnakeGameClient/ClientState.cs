using System;
using System.Collections.Generic;

namespace SnakeGame.Client;

public sealed class ClientState
{
    public bool HandshakeOk { get; set; }
    public byte ProtocolVersion { get; set; }
    public Guid? CurrentSessionId { get; set; }
    public ushort? PlayerId { get; set; }

    public int LastTick { get; set; }

    // Данные для рендера
    public List<ClientPlayer> Players { get; } = new();
    public List<ClientFood> Foods { get; } = new();

    // Ссылка на сетевой клиент, чтобы хендлеры могли отправлять команды
    public NetworkClient? Network { get; set; }
}
