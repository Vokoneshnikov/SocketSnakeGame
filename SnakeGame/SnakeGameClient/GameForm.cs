using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SnakeGame.Client;

public sealed class GameForm : Form
{
    private readonly ClientState _state;

    private readonly Timer _renderTimer;
    private readonly Timer _inputTimer;

    private float _scale;
    private const float SnakeRadius = 3f;
    private const float FoodRadius  = 4f;

    // управление
    private bool  _turnLeft;
    private bool  _turnRight;
    private float _currentAngle;          // радианы
    private const float TurnSpeed = 2.5f; // рад/сек

    // стартовые кнопки
    private readonly Button _btnCreate;
    private readonly Button _btnJoin;

    // панели
    private readonly DoubleBufferedPanel _gamePanel;
    private readonly DoubleBufferedPanel _sidePanel;

    // размеры мира (подгони под серверные значения)
    private const float WorldWidth  = 2000f;
    private const float WorldHeight = 1200f;

    public GameForm(ClientState state)
    {
        _state = state;

        DoubleBuffered = true;
        Width  = 1200;
        Height = 800;
        Text   = "Snake Client";

        KeyPreview = true;
        KeyDown   += OnKeyDownHandler;
        KeyUp     += OnKeyUpHandler;

        // ===== Стартовый экран: большие кнопки =====

        _btnCreate = new Button
        {
            Text   = "Create",
            Width  = 200,
            Height = 60
        };
        _btnCreate.Click += OnCreateClick;
        Controls.Add(_btnCreate);

        _btnJoin = new Button
        {
            Text   = "Join",
            Width  = 200,
            Height = 60
        };
        _btnJoin.Click += OnJoinClick;
        Controls.Add(_btnJoin);

        // ===== Игровые панели (по умолчанию спрятаны) =====

        _gamePanel = new DoubleBufferedPanel
        {
            BackColor = Color.Black,
            Visible   = false
        };
        _gamePanel.Paint += GamePanel_Paint;
        Controls.Add(_gamePanel);

        _sidePanel = new DoubleBufferedPanel
        {
            BackColor = Color.FromArgb(15, 15, 20),
            Visible   = false
        };
        _sidePanel.Paint += SidePanel_Paint;
        Controls.Add(_sidePanel);

        // пересчёт расположения при изменении размера
        Resize += (_, _) => LayoutControls();
        LayoutControls();

        // таймер рендера
        _renderTimer = new Timer { Interval = 50 }; // ~20 FPS
        _renderTimer.Tick += (_, _) =>
        {
            UpdateUiState();
            _gamePanel.Invalidate();
            _sidePanel.Invalidate();
        };
        _renderTimer.Start();

        // таймер отправки ввода
        _inputTimer = new Timer { Interval = 50 }; // 20 раз/сек
        _inputTimer.Tick += OnInputTick;
        _inputTimer.Start();
    }

    // размещение кнопок и панелей
    private void LayoutControls()
    {
        int w = ClientSize.Width;
        int h = ClientSize.Height;

        // центрируем кнопки
        int centerX = w / 2;
        int centerY = h / 2;

        _btnCreate.Left = centerX - _btnCreate.Width - 10;
        _btnCreate.Top  = centerY - _btnCreate.Height / 2;

        _btnJoin.Left = centerX + 10;
        _btnJoin.Top  = centerY - _btnJoin.Height / 2;

        // панели: 80% / 20%
        int gameWidth = (int)(w * 0.8);
        int sideWidth = w - gameWidth;

        _gamePanel.SetBounds(0, 0, gameWidth, h);
        _sidePanel.SetBounds(gameWidth, 0, sideWidth, h);
    }

    private void SwitchToGameView()
    {
        _btnCreate.Visible = false;
        _btnJoin.Visible   = false;

        _gamePanel.Visible = true;
        _sidePanel.Visible = true;

        LayoutControls();
    }

    private void UpdateUiState()
    {
        // как только появилась сессия — переключаемся в режим игры
        if (_state.CurrentSessionId != null && !_gamePanel.Visible)
        {
            SwitchToGameView();
        }

        bool inLobby = _state.CurrentSessionId == null;
        _btnCreate.Enabled = inLobby;
        _btnJoin.Enabled   = inLobby;
    }

    private async void OnCreateClick(object? sender, EventArgs e)
    {
        if (_state.Network == null)
            return;

        try
        {
            await _state.Network.SendPacketAsync(
                Command.CreateSessionRequest,
                w => w.WriteByte(0));
            Console.WriteLine("[Client UI] CreateSessionRequest sent");
        }
        catch
        {
        }
    }

    private async void OnJoinClick(object? sender, EventArgs e)
    {
        if (_state.Network == null)
            return;

        try
        {
            await _state.Network.SendPacketAsync(Command.ListSessionsRequest);
            Console.WriteLine("[Client UI] ListSessionsRequest sent");
        }
        catch
        {
        }
    }

    private void OnKeyDownHandler(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left)
        {
            _turnLeft = true;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right)
        {
            _turnRight = true;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void OnKeyUpHandler(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left)
        {
            _turnLeft = false;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right)
        {
            _turnRight = false;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private async void OnInputTick(object? sender, EventArgs e)
    {
        if (_state.Network == null || _state.CurrentSessionId == null || _state.PlayerId == null)
            return;

        float dt = _inputTimer.Interval / 1000f;

        if (_turnLeft && !_turnRight)
            _currentAngle -= TurnSpeed * dt;
        else if (_turnRight && !_turnLeft)
            _currentAngle += TurnSpeed * dt;

        if (_currentAngle > MathF.PI)  _currentAngle -= 2 * MathF.PI;
        if (_currentAngle < -MathF.PI) _currentAngle += 2 * MathF.PI;

        try
        {
            await _state.Network.SendPacketAsync(
                Command.PlayerInput,
                w =>
                {
                    w.WriteFloat(_currentAngle);
                    w.WriteByte(0); // флаги на будущее
                });
        }
        catch
        {
        }
    }

    // ===== Рендер игрового поля =====
    private void GamePanel_Paint(object? sender, PaintEventArgs e)
    {
        // снимок состояния
        System.Collections.Generic.List<ClientFood> foodsSnapshot;
        System.Collections.Generic.List<ClientPlayer> playersSnapshot;
        int tick;
        ushort? localPlayerId = _state.PlayerId;

        lock (_state.SyncRoot)
        {
            tick = _state.LastTick;
            foodsSnapshot   = new System.Collections.Generic.List<ClientFood>(_state.Foods);
            playersSnapshot = new System.Collections.Generic.List<ClientPlayer>(_state.Players.Count);

            foreach (var p in _state.Players)
            {
                var copy = new ClientPlayer
                {
                    Id      = p.Id,
                    IsAlive = p.IsAlive,
                    Angle   = p.Angle,
                    Score   = p.Score
                };
                copy.Segments.AddRange(p.Segments);
                playersSnapshot.Add(copy);
            }
        }

        var g = e.Graphics;
        g.Clear(Color.Black);

        int panelW = _gamePanel.ClientSize.Width;
        int panelH = _gamePanel.ClientSize.Height;

        // масштаб под размер панели
        float scaleX = panelW / WorldWidth;
        float scaleY = panelH / WorldHeight;
        _scale = MathF.Min(scaleX, scaleY);

        float fieldWidth  = WorldWidth  * _scale;
        float fieldHeight = WorldHeight * _scale;

        // округляем, чтобы пиксели совпадали
        fieldWidth  = (float)Math.Round(fieldWidth);
        fieldHeight = (float)Math.Round(fieldHeight);

        // центрируем поле
        float offsetX = (panelW - fieldWidth)  / 2f;
        float offsetY = (panelH - fieldHeight) / 2f;

        // фон поля
        using (var fieldBrush = new SolidBrush(Color.FromArgb(20, 20, 30)))
        {
            g.FillRectangle(fieldBrush, offsetX, offsetY, fieldWidth, fieldHeight);
        }

        // рамка строго того же размера
        using (var borderPen = new Pen(Color.DimGray, 2f))
        {
            g.DrawRectangle(borderPen,
                offsetX,
                offsetY,
                fieldWidth,
                fieldHeight); // один размер с полем [web:228]
        }

        // еда (яблоки)
        foreach (var food in foodsSnapshot)
        {
            float x = offsetX + food.X * _scale;
            float y = offsetY + food.Y * _scale;

            g.FillEllipse(Brushes.Red,
                x - FoodRadius, y - FoodRadius,
                FoodRadius * 2, FoodRadius * 2);
        }

        // игроки
        foreach (var p in playersSnapshot)
        {
            if (p.Segments.Count == 0)
                continue;

            Brush brush = p.IsAlive ? Brushes.LimeGreen : Brushes.Gray;

            if (localPlayerId != null && p.Id == localPlayerId)
                brush = p.IsAlive ? Brushes.Cyan : Brushes.DarkGray;

            foreach (var (sx, sy) in p.Segments)
            {
                float x = offsetX + sx * _scale;
                float y = offsetY + sy * _scale;

                g.FillEllipse(brush,
                    x - SnakeRadius, y - SnakeRadius,
                    SnakeRadius * 2, SnakeRadius * 2);
            }

            var head = p.Segments[^1];
            float hx = offsetX + head.X * _scale;
            float hy = offsetY + head.Y * _scale;
            g.FillEllipse(Brushes.Yellow,
                hx - SnakeRadius - 1, hy - SnakeRadius - 1,
                (SnakeRadius + 1) * 2, (SnakeRadius + 1) * 2);
        }
    }

    // ===== Правая панель: очки / рейтинг / статус =====
    private void SidePanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Color.FromArgb(15, 15, 20));

        System.Collections.Generic.List<ClientPlayer> playersSnapshot;
        int tick;
        ushort? localPlayerId = _state.PlayerId;
        bool inLobby;

        lock (_state.SyncRoot)
        {
            tick     = _state.LastTick;
            inLobby  = _state.InLobby;
            playersSnapshot = new System.Collections.Generic.List<ClientPlayer>(_state.Players.Count);
            foreach (var p in _state.Players)
            {
                var copy = new ClientPlayer
                {
                    Id      = p.Id,
                    IsAlive = p.IsAlive,
                    Angle   = p.Angle,
                    Score   = p.Score
                };
                copy.Segments.AddRange(p.Segments);
                playersSnapshot.Add(copy);
            }
        }

        using var font       = new Font("Consolas", 10f);
        using var brushText  = new SolidBrush(Color.White);
        using var brushLocal = new SolidBrush(Color.Yellow);
        using var brushDead  = new SolidBrush(Color.Gray);

        float x = 10f;
        float y = 10f;

        string stateText = inLobby ? "Lobby" : "Playing";
        g.DrawString($"State: {stateText}", font, brushText, x, y);
        y += 18;
        g.DrawString($"Tick: {tick}", font, brushText, x, y);
        y += 24;

        g.DrawString("Scores:", font, brushText, x, y);
        y += 18;

        foreach (var p in playersSnapshot.OrderByDescending(p => p.Score))
        {
            bool isLocal = localPlayerId != null && p.Id == localPlayerId;
            string status = p.IsAlive ? "" : " (dead)";
            string you    = isLocal ? " (you)" : "";
            string line   = $"P{p.Id}: {p.Score}{you}{status}";

            var brush = p.IsAlive
                ? (isLocal ? brushLocal : brushText)
                : brushDead;

            g.DrawString(line, font, brush, x, y);
            y += 16;
        }
    }
}
