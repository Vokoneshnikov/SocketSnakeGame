using System.Drawing;
using System.Windows.Forms;
using SnakeGame.Client;
using System;

namespace SnakeGame.Client;

public sealed class GameForm : Form
{
    private readonly ClientState _state;
    private readonly Timer _timer;

    // масштаб из игровых координат в пиксели
    private const float Scale = 0.5f; // подгони под размер мира/окна
    private const float SnakeRadius = 3f;
    private const float FoodRadius = 4f;

    public GameForm(ClientState state)
    {
        _state = state;

        DoubleBuffered = true;
        Width = 1200;
        Height = 800;
        Text = "Snake Client";

        _timer = new Timer
        {
            Interval = 50 // ~20 FPS
        };
        _timer.Tick += (_, _) => Invalidate();
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Console.WriteLine($"[Render] tick={_state.LastTick}, players={_state.Players.Count}, foods={_state.Foods.Count}");

        var g = e.Graphics;
        g.Clear(Color.Black);

        // рисуем еду
        foreach (var food in _state.Foods)
        {
            float x = food.X * Scale;
            float y = food.Y * Scale;

            g.FillEllipse(Brushes.Red,
                x - FoodRadius, y - FoodRadius,
                FoodRadius * 2, FoodRadius * 2);
        }

        // рисуем игроков
        foreach (var p in _state.Players)
        {
            if (p.Segments.Count == 0)
                continue;

            Brush brush = p.IsAlive ? Brushes.LimeGreen : Brushes.Gray;

            foreach (var (sx, sy) in p.Segments)
            {
                float x = sx * Scale;
                float y = sy * Scale;

                g.FillEllipse(brush,
                    x - SnakeRadius, y - SnakeRadius,
                    SnakeRadius * 2, SnakeRadius * 2);
            }

            // голова — другим цветом
            var head = p.Segments[^1];
            float hx = head.X * Scale;
            float hy = head.Y * Scale;
            g.FillEllipse(Brushes.Yellow,
                hx - SnakeRadius - 1, hy - SnakeRadius - 1,
                (SnakeRadius + 1) * 2, (SnakeRadius + 1) * 2);
        }
    }
}
