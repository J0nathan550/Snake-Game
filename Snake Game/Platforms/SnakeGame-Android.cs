using Newtonsoft.Json;
using SFML.Audio;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace Snake_Game.Platforms
{
    internal class SnakeGame_Android
    {
        #region Window

        private RenderWindow? window;
        private VideoMode videoMode = new VideoMode(800, 600);
        private ContextSettings settings = new ContextSettings() { AntialiasingLevel = 1 };
        private static Random random = new Random();
        private Clock deltaClock = new Clock();

        private Vector2f startSwipePosition = new Vector2f();
        private Vector2f endSwipePosition = new Vector2f();

        #endregion

        #region Game Variables

        private const float snakeSize = 20;
        private RectangleShape apple = new RectangleShape()
        {
            Size = new Vector2f(20, 20),
            FillColor = Color.Red,
            Position = new Vector2f(-50, 0),
        };
        private RectangleShape massiveApple = new RectangleShape()
        {
            Size = new Vector2f(40, 40),
            FillColor = Color.Red,
            Position = new Vector2f(-100, 0),
        };
        private enum Direction
        {
            NONE,
            LEFT,
            RIGHT,
            UP,
            DOWN
        }
        class SaveSystem
        {
            public int bestScore = 0;
        }
        private SaveSystem saveSystem = new SaveSystem();
        private Direction snakeDirection = Direction.RIGHT;
        private Text scoreText = new Text();
        private Sound? hitSound, gameOverSound;
        private SoundBuffer? soundBuffer;
        private int score = 0;
        private int massiveAppleCounter = 0;
        private int massiveAppleHitter = random.Next(5, 15);
        private float snakeSpeed = 20f;
        private float massiveAppleVanishTime = 10.0f;
        private float massiveAppleAnimation = 0.5f;
        private float snakeDelay = 0f;
        private float updateDiscordStatus = 1.0f;
        private bool isGameOver = true;
        private bool massiveAppleAppear = false;
        private bool addPiece = false;
        private List<RectangleShape> snake = new List<RectangleShape>();

        #endregion

        public void RunWindow()
        {
            window = new RenderWindow(videoMode, "Snake Game - by J0nathan550!", Styles.Close, settings);

            window.Closed += Window_Closed;
            window.TouchBegan += Window_TouchBegan;
            window.TouchEnded += Window_TouchEnded;

            Image iconImage = new Image("Assets/snake.png");
            window.SetIcon(iconImage.Size.X, iconImage.Size.Y, iconImage.Pixels);

            Font font = new Font("Assets/Fixedsys.ttf");
            scoreText = new Text($"Score: {score}", font, 24)
            {
                Position = new Vector2f(videoMode.Width / 2 - 60, videoMode.Height / 2 - 300),
                FillColor = Color.Red
            };

            soundBuffer = new SoundBuffer("Assets/score.ogg");
            hitSound = new Sound();
            hitSound.SoundBuffer = soundBuffer;
            soundBuffer = new SoundBuffer("Assets/gameOver.ogg");
            gameOverSound = new Sound();
            gameOverSound.SoundBuffer = soundBuffer;

            snake.Add(new RectangleShape()
            {
                Size = new Vector2f(snakeSize, snakeSize),
                FillColor = Color.Green,
                Position = new Vector2f(videoMode.Width / 2, videoMode.Height / 2),
            });

            Welcome();

            while (window.IsOpen)
            {
                window.DispatchEvents();

                window.Clear();

                float deltaTime = deltaClock.Restart().AsSeconds();

                Update(deltaTime);

                window.Draw(scoreText);

                window.Draw(apple);
                window.Draw(massiveApple);

                for (int i = 0; i < snake.Count; i++)
                {
                    window.Draw(snake[i]);
                }

                window.Display();
            }
        }

        private void Update(float deltaTime)
        {
            if (massiveAppleAppear)
            {
                massiveAppleVanishTime -= deltaTime;
                massiveAppleAnimation -= deltaTime;
                if (massiveAppleVanishTime <= 0)
                {
                    massiveApple.Position = new Vector2f(-100, 0);
                    massiveAppleAppear = false;
                    massiveAppleVanishTime = 10;
                }
                if (massiveAppleAnimation <= 0)
                {
                    if (massiveApple.Size.X == 40 && massiveApple.Size.Y == 40)
                    {
                        massiveApple.Size = new Vector2f(20, 20);
                    }
                    else
                    {
                        massiveApple.Size = new Vector2f(40, 40);
                    }
                    massiveAppleAnimation = 0.5f;
                }
            }

            snakeDelay += deltaTime * snakeSpeed;
            if (snakeDelay < 1)
            {
                return;
            }

            Vector2f lastPiecePos = MoveSnake();
            snakeDelay = 0;


            if (CollidesApple(apple))
            {
                hitSound?.Play();
                apple.Position = new Vector2f(random.Next(0, (int)videoMode.Width - 100), random.Next(0, (int)videoMode.Height - 100));
                score++;
                massiveAppleCounter++;
                if (massiveAppleCounter == massiveAppleHitter)
                {
                    massiveApple.Position = new Vector2f(random.Next(0, (int)videoMode.Width - 100), random.Next(0, (int)videoMode.Height - 100));
                    massiveAppleAppear = true;
                    massiveAppleVanishTime = 10.0f;
                    massiveAppleCounter = 0;
                    massiveAppleHitter = random.Next(5, 15);
                }
                scoreText.DisplayedString = $"Score: {score}";

                // Add tail piece
                addPiece = true;
            }

            if (CollidesApple(massiveApple))
            {
                hitSound?.Play();
                score += random.Next(50, 150);
                scoreText.DisplayedString = $"Score: {score}";
                massiveApple.Position = new Vector2f(-100, 0);
                massiveAppleAppear = false;
                massiveAppleHitter = random.Next(5, 15);
                massiveAppleVanishTime = 10.0f;
                addPiece = true;
            }

            if (CollidesWithTail())
            {
                GameOver();
            }

            //if (window != null)
            //{
            //    if (CollidesWithWindowBounds(window))
            //    {
            //        GameOver();
            //    }
            //}

            // adding piece
            if (addPiece)
            {
                var newTailPiece = new RectangleShape()
                {
                    Size = new Vector2f(snakeSize, snakeSize),
                    FillColor = Color.Green,
                    Position = lastPiecePos
                };
                snake.Add(newTailPiece);
                addPiece = false;
            }
        }

        private bool CollidesApple(RectangleShape other)
        {
            return snake[0].GetGlobalBounds().Intersects(other.GetGlobalBounds());
        }

        private bool CollidesWithTail()
        {
            for (int i = 0; i < snake.Count; i++)
            {
                for (int j = i + 1; j < snake.Count; j++)
                {
                    if (snake[i].Position.Equals(snake[j].Position))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //private static bool CollidesWithWindowBounds(RenderWindow window)
        //{
        //    FloatRect windowBounds = new FloatRect(0, 0, window.Size.X, window.Size.Y);

        //    return !windowBounds.Contains(snake.GetGlobalBounds().Left, snake.GetGlobalBounds().Top)
        //        || !windowBounds.Contains(snake.GetGlobalBounds().Left + snake.GetGlobalBounds().Width, snake.GetGlobalBounds().Top)
        //        || !windowBounds.Contains(snake.GetGlobalBounds().Left, snake.GetGlobalBounds().Top + snake.GetGlobalBounds().Height)
        //        || !windowBounds.Contains(snake.GetGlobalBounds().Left + snake.GetGlobalBounds().Width, snake.GetGlobalBounds().Top + snake.GetGlobalBounds().Height);
        //}

        private Vector2f MoveSnake()
        {
            //scoreText.DisplayedString = $"Position X: {snake[0].Position.X}\nPosition Y: {snake[0].Position.Y}";
            Vector2f lastPiece = snake[snake.Count - 1].Position;
            for (int i = snake.Count - 1; i > 0; i--)
            {
                snake[i].Position = snake[i - 1].Position;
            }
            switch (snakeDirection)
            {
                case Direction.NONE:
                    break;
                case Direction.LEFT:
                    snake[0].Position = new Vector2f(snake[0].Position.X - snakeSize, snake[0].Position.Y);
                    break;
                case Direction.RIGHT:
                    snake[0].Position = new Vector2f(snake[0].Position.X + snakeSize, snake[0].Position.Y);
                    break;
                case Direction.UP:
                    snake[0].Position = new Vector2f(snake[0].Position.X, snake[0].Position.Y - snakeSize);
                    break;
                case Direction.DOWN:
                    snake[0].Position = new Vector2f(snake[0].Position.X, snake[0].Position.Y + snakeSize);
                    break;
            }
            if (window != null && snake[0].Position.X < 0)
            {
                snake[0].Position = new Vector2f(window.Size.X - snakeSize, snake[0].Position.Y);
            }
            else if (window != null && snake[0].Position.X >= window.Size.X - snakeSize)
            {
                snake[0].Position = new Vector2f(0, snake[0].Position.Y);
            }
            if (window != null && snake[0].Position.Y < 0)
            {
                snake[0].Position = new Vector2f(snake[0].Position.X, window.Size.Y - snakeSize);
            }
            else if (window != null && snake[0].Position.Y >= window.Size.Y - snakeSize)
            {
                snake[0].Position = new Vector2f(snake[0].Position.X, 0);
            }
            return lastPiece;
        }

        private void GameOver()
        {
            gameOverSound?.Play();
            snake.Clear();
            snake.Add(new RectangleShape()
            {
                Size = new Vector2f(snakeSize, snakeSize),
                FillColor = Color.Green,
                Position = new Vector2f(videoMode.Width / 2, videoMode.Height / 2),
            });
            if (score > saveSystem.bestScore)
            {
                saveSystem.bestScore = score;
                string json = JsonConvert.SerializeObject(saveSystem, Formatting.Indented);
                File.WriteAllText("save.json", json);
            }
            scoreText.DisplayedString = $"Game Over!\nScore: {score}\nBest Score: {saveSystem.bestScore}\nPress 'R' to restart game!";
            score = 0;
            massiveAppleAppear = false;
            massiveAppleCounter = 0;
            massiveAppleAnimation = 0.5f;
            massiveAppleVanishTime = 10.0f;
            massiveApple.Position = new Vector2f(-100, 0);
            massiveAppleHitter = random.Next(5, 15);
            snakeDirection = Direction.NONE;
            isGameOver = true;
        }

        private void Welcome()
        {
            try
            {
                string info = File.ReadAllText("save.json");
                saveSystem = JsonConvert.DeserializeObject<SaveSystem>(info);
            }
            catch
            {
                FileStream file = File.Create("save.json");
                file.Close();
                string json = JsonConvert.SerializeObject(saveSystem, Formatting.Indented);
                File.WriteAllText("save.json", json);
                if (saveSystem != null)
                {
                    saveSystem.bestScore = 0;
                }
            }
            if (saveSystem != null)
            {
                scoreText.DisplayedString = $"Welcome to Snake Game!\nScore: {score}\nBest Score: {saveSystem.bestScore}\nPress 'R' to start game!";
                snakeDirection = Direction.NONE;
            }
        }

        private int countTouch = 0;
        private void Window_TouchBegan(object? sender, TouchEventArgs e)
        {
            if (e.Finger == 0)
            {
                if (!isGameOver)
                {
                    startSwipePosition = new Vector2f(e.X, e.Y);    
                }
            }
        }

        private void Window_TouchEnded(object? sender, TouchEventArgs e)
        {
            if (e.Finger == 0)
            {
                if (!isGameOver)
                {
                    endSwipePosition = new Vector2f(e.X, e.Y);
                    CheckSwipeDirection(startSwipePosition, endSwipePosition);
                }
                else
                {
                    countTouch++;
                    if (countTouch == 2)
                    {
                        isGameOver = false;
                        snakeDirection = Direction.RIGHT;
                        apple.Position = new Vector2f(random.Next(0, (int)videoMode.Width - 100), random.Next(0, (int)videoMode.Height - 100));
                        scoreText.DisplayedString = $"Score: {score}";
                        countTouch = 0;
                    }
                }
            }
        }

        private void CheckSwipeDirection(Vector2f start, Vector2f end)
        {
            float deltaX = end.X - start.X;
            float deltaY = end.Y - start.Y;

            if (deltaX > 0 && Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                if (snakeDirection == Direction.LEFT)
                {
                    return;
                }
                snakeDirection = Direction.RIGHT;
            }
            else if (deltaX < 0 && Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                if (snakeDirection == Direction.RIGHT)
                {
                    return;
                }
                snakeDirection = Direction.LEFT;
            }
            else if (deltaY > 0 && Math.Abs(deltaY) > Math.Abs(deltaX))
            {
                if (snakeDirection == Direction.UP)
                {
                    return;
                }
                snakeDirection = Direction.DOWN;
            }
            else if (deltaY < 0 && Math.Abs(deltaY) > Math.Abs(deltaX))
            {
                if (snakeDirection == Direction.DOWN)
                {
                    return;
                }
                snakeDirection = Direction.UP;
            }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            window?.Close();
        }
    }
}