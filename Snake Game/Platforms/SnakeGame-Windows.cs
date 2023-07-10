using DiscordRPC;
using Newtonsoft.Json;
using SFML.Audio;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace Snake_Game.Platforms
{
    internal class SnakeGame_Windows
    {
        #region Window

        private RenderWindow? window;
        private VideoMode videoMode = new VideoMode(800, 600);
        private ContextSettings settings = new ContextSettings() { AntialiasingLevel = 1 };
        private static Random random = new Random();
        private Clock deltaClock = new Clock();
        private DiscordRpcClient client = new DiscordRpcClient("1127696837634166986");
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
        private Timestamps? timestamps;
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
            window.KeyPressed += Window_KeyPressed;

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

            try
            {
                client.Initialize();
                client.SetPresence(new RichPresence()
                {
                    Details = "In start...",
                    State = $"Score: {score} Best Score: {saveSystem.bestScore}",
                    Assets = new Assets()
                    {
                        LargeImageKey = "snake",
                        LargeImageText = "Snake Game!"
                    },
                    Buttons = new DiscordRPC.Button[]
                    {
                        new DiscordRPC.Button()
                        {
                            Label = "Play Snake Game!",
                            Url = "https://github.com/J0nathan550/Snake-Game"
                        }
                    },
                    Timestamps = Timestamps.Now
                });
            }
            catch
            {
            }

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

            if (!isGameOver)
            {
                updateDiscordStatus -= deltaTime;
                if (updateDiscordStatus <= 0)
                {
                    try
                    {
                        if (timestamps == null)
                        {
                            timestamps = Timestamps.Now;
                        }
                        client.SetPresence(new RichPresence()
                        {
                            Details = "Playing...",
                            State = $"Score: {score}",
                            Assets = new Assets()
                            {
                                LargeImageKey = "snake",
                                LargeImageText = "Snake Game!"
                            },
                            Buttons = new DiscordRPC.Button[]
                            {
                        new DiscordRPC.Button()
                        {
                            Label = "Play Snake Game!",
                            Url = "https://github.com/J0nathan550/Snake-Game"
                        }
                            },
                            Timestamps = timestamps
                        });
                    }
                    catch
                    {
                    }
                    updateDiscordStatus = 1.0f;
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
            try
            {
                timestamps = null;
                client.SetPresence(new RichPresence()
                {
                    Details = "Game Over!",
                    State = $"Score: {score}\nBest Score: {saveSystem.bestScore}",
                    Assets = new Assets()
                    {
                        LargeImageKey = "snake",
                        LargeImageText = "Snake Game!"
                    },
                    Buttons = new DiscordRPC.Button[]
                    {
                        new DiscordRPC.Button()
                        {
                            Label = "Play Snake Game!",
                            Url = "https://github.com/J0nathan550/Snake-Game"
                        }
                    },
                    Timestamps = Timestamps.Now
                });
            }
            catch
            {
            }
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

        private void Window_KeyPressed(object? sender, KeyEventArgs e)
        {
            if (!isGameOver)
            {
                if (e.Code == Keyboard.Key.W || e.Code == Keyboard.Key.Up)
                {
                    if (snakeDirection == Direction.DOWN)
                    {
                        return;
                    }
                    snakeDirection = Direction.UP;
                }
                else if (e.Code == Keyboard.Key.S || e.Code == Keyboard.Key.Down)
                {
                    if (snakeDirection == Direction.UP)
                    {
                        return;
                    }
                    snakeDirection = Direction.DOWN;
                }
                else if (e.Code == Keyboard.Key.A || e.Code == Keyboard.Key.Left)
                {
                    if (snakeDirection == Direction.RIGHT)
                    {
                        return;
                    }
                    snakeDirection = Direction.LEFT;
                }
                else if (e.Code == Keyboard.Key.D || e.Code == Keyboard.Key.Right)
                {
                    if (snakeDirection == Direction.LEFT)
                    {
                        return;
                    }
                    snakeDirection = Direction.RIGHT;
                }
            }
            else
            {
                if (e.Code == Keyboard.Key.R)
                {
                    isGameOver = false;
                    snakeDirection = Direction.RIGHT;
                    apple.Position = new Vector2f(random.Next(0, (int)videoMode.Width - 100), random.Next(0, (int)videoMode.Height - 100));
                    scoreText.DisplayedString = $"Score: {score}";
                }
            }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            window?.Close();
        }
    }
}