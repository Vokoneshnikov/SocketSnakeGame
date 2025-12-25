namespace SnakeGame;

public class GameWorld
{
    // Размеры карты в условных единицах
    public float Width { get; }
    public float Height { get; }

    // Объекты
    public List<Player> Players { get; } = new List<Player>();
    public List<Food> Foods { get; } = new List<Food>();

    // Параметры
    public int MaxFoodCount { get; set; } = 200;

    public GameWorld(float width, float height)
    {
        Width = width;
        Height = height;
    }

    // В дальнейшем сюда добавим:
    // - Tick(deltaTime)
    // - SpawnFood()
    // - Проверку коллизий
}
