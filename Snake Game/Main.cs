using Snake_Game.Platforms;

namespace Snake_Game
{
    internal class MainEntry
    {
        public static void Main(string[] args)
        {
            if (OperatingSystem.IsWindows())
            {
                SnakeGame_Windows snake_Windows = new SnakeGame_Windows();
                snake_Windows.RunWindow();
            }
            else if (OperatingSystem.IsAndroid())
            {
                SnakeGame_Android snakeGame_Android = new SnakeGame_Android();
                snakeGame_Android.RunWindow();
            }
        }
    }
}