using System;
using System.Collections.Generic;
using SnakeGame;
using SnakeGame.Client;

public sealed class ClientState
{
    public bool HandshakeOk { get; set; }
    public byte ProtocolVersion { get; set; }
    public Guid? CurrentSessionId { get; set; }
    public ushort? PlayerId { get; set; }

    public int LastTick { get; set; }

    public List<ClientPlayer> Players { get; } = new();
    public List<ClientFood> Foods { get; } = new();
    
    public NetworkClient? Network { get; set; }

    public object SyncRoot { get; } = new();

    // новое: находимся ли в лобби (нет активной сессии)
    public bool InLobby => CurrentSessionId == null;
}