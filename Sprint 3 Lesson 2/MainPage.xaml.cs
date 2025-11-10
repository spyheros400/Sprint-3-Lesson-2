using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace Sprint_3_Lesson_2;

public partial class MainPage : ContentPage
{
    private readonly List<PlatformInfo> _platforms = new();
    private readonly IDispatcherTimer _timer;

    private bool _initialized;
    private bool _gameRunning;
    private bool _gameOver;
    private bool _moveLeft;
    private bool _moveRight;
    private double _playerX;
    private double _playerY;
    private double _playerVelocityY;

    private const double Gravity = 0.7;
    private const double JumpVelocity = -12;
    private const double HorizontalSpeed = 5;

    public MainPage()
    {
        InitializeComponent();

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnGameTick;

        GameArea.SizeChanged += OnGameAreaSizeChanged;

        _platforms.Add(new PlatformInfo(Platform1, 3));
        _platforms.Add(new PlatformInfo(Platform2, -2.5));
        _platforms.Add(new PlatformInfo(Platform3, 2));
    }

    private void OnGameAreaSizeChanged(object? sender, EventArgs e)
    {
        if (GameArea.Width <= 0 || GameArea.Height <= 0)
        {
            return;
        }

        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ResetGameState();
    }

    private void ResetGameState()
    {
        _timer.Stop();
        _gameRunning = false;
        _gameOver = false;
        _moveLeft = false;
        _moveRight = false;
        _playerVelocityY = 0;

        var height = GameArea.Height;
        var width = GameArea.Width;

        if (height <= 0 || width <= 0)
        {
            return;
        }

        var platformSpacing = height / 4;

        for (int i = 0; i < _platforms.Count; i++)
        {
            double y = height - platformSpacing * (i + 1);
            double x = width * (0.2 + 0.3 * i);
            _platforms[i].X = x;
            _platforms[i].Y = y;
        }

        if (_platforms.Count > 0)
        {
            var first = _platforms[0];
            _playerX = first.X + first.Width / 2 - Player.WidthRequest / 2;
            _playerY = first.Y - Player.HeightRequest;
        }
        else
        {
            _playerX = width / 2 - Player.WidthRequest / 2;
            _playerY = height - Player.HeightRequest - 10;
        }

        UpdateVisuals();

        StartButton.IsVisible = true;
        ResetButton.IsVisible = false;
    }

    private void OnStartButtonClicked(object? sender, EventArgs e)
    {
        if (!_initialized)
        {
            return;
        }

        _gameRunning = true;
        _gameOver = false;
        _playerVelocityY = 0;
        _moveLeft = false;
        _moveRight = false;
        StartButton.IsVisible = false;
        ResetButton.IsVisible = false;
        _timer.Start();
    }

    private void OnResetButtonClicked(object? sender, EventArgs e)
    {
        ResetGameState();
    }

    private void OnGameAreaPointerPressed(object? sender, PointerEventArgs e)
    {
        if (!_gameRunning || _gameOver)
        {
            return;
        }

        if (IsPlayerGrounded())
        {
            _playerVelocityY = JumpVelocity;
        }
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        AttachKeyboardEvents();
    }

    private void OnPageUnloaded(object? sender, EventArgs e)
    {
        DetachKeyboardEvents();
    }

    private void AttachKeyboardEvents()
    {
        var window = this.GetParentWindow();
        if (window is null)
        {
            return;
        }

        window.KeyDown -= OnWindowKeyDown;
        window.KeyDown += OnWindowKeyDown;
        window.KeyUp -= OnWindowKeyUp;
        window.KeyUp += OnWindowKeyUp;
    }

    private void DetachKeyboardEvents()
    {
        var window = this.GetParentWindow();
        if (window is null)
        {
            return;
        }

        window.KeyDown -= OnWindowKeyDown;
        window.KeyUp -= OnWindowKeyUp;
    }

    private void OnWindowKeyDown(object? sender, KeyboardEventArgs e)
    {
        if (!_gameRunning || _gameOver)
        {
            return;
        }

        if (IsLeftKey(e.Key))
        {
            _moveLeft = true;
            e.Handled = true;
        }
        else if (IsRightKey(e.Key))
        {
            _moveRight = true;
            e.Handled = true;
        }
        else if (IsJumpKey(e.Key) && IsPlayerGrounded())
        {
            _playerVelocityY = JumpVelocity;
            e.Handled = true;
        }
    }

    private void OnWindowKeyUp(object? sender, KeyboardEventArgs e)
    {
        if (IsLeftKey(e.Key))
        {
            _moveLeft = false;
            e.Handled = true;
        }
        else if (IsRightKey(e.Key))
        {
            _moveRight = false;
            e.Handled = true;
        }
    }

    private static bool IsLeftKey(string key)
    {
        return string.Equals(key, "Left", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "ArrowLeft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "A", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRightKey(string key)
    {
        return string.Equals(key, "Right", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "ArrowRight", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "D", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJumpKey(string key)
    {
        return string.Equals(key, "Space", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Spacebar", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Up", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "ArrowUp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "W", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsPlayerGrounded()
    {
        foreach (var platform in _platforms)
        {
            if (Math.Abs((_playerY + Player.HeightRequest) - platform.Y) < 0.5)
            {
                var playerCenter = _playerX + Player.WidthRequest / 2;
                if (playerCenter >= platform.X && playerCenter <= platform.X + platform.Width)
                {
                    return true;
                }
            }
        }

        return _playerY + Player.HeightRequest >= GameArea.Height;
    }

    private void OnGameTick(object? sender, EventArgs e)
    {
        if (!_gameRunning || _gameOver)
        {
            return;
        }

        UpdatePlatforms();
        UpdatePlayer();
        UpdateVisuals();

        if (_playerY > GameArea.Height)
        {
            TriggerGameOver();
        }
    }

    private void UpdatePlatforms()
    {
        foreach (var platform in _platforms)
        {
            platform.X += platform.Velocity;

            if (platform.X <= 0)
            {
                platform.X = 0;
                platform.Velocity = Math.Abs(platform.Velocity);
            }
            else if (platform.X + platform.Width >= GameArea.Width)
            {
                platform.X = GameArea.Width - platform.Width;
                platform.Velocity = -Math.Abs(platform.Velocity);
            }
        }
    }

    private void UpdatePlayer()
    {
        double horizontalDelta = 0;
        if (_moveLeft)
        {
            horizontalDelta -= HorizontalSpeed;
        }

        if (_moveRight)
        {
            horizontalDelta += HorizontalSpeed;
        }

        _playerX += horizontalDelta;

        if (_playerX < 0)
        {
            _playerX = 0;
        }
        else if (_playerX + Player.WidthRequest > GameArea.Width)
        {
            _playerX = Math.Max(0, GameArea.Width - Player.WidthRequest);
        }

        double previousY = _playerY;
        _playerVelocityY += Gravity;
        _playerY += _playerVelocityY;

        bool landed = false;

        foreach (var platform in _platforms)
        {
            var platformRect = new Rect(platform.X, platform.Y, platform.Width, platform.Height);
            var playerRect = new Rect(_playerX, _playerY, Player.WidthRequest, Player.HeightRequest);

            if (_playerVelocityY >= 0 && previousY + Player.HeightRequest <= platformRect.Top)
            {
                if (playerRect.Right >= platformRect.Left && playerRect.Left <= platformRect.Right)
                {
                    if (playerRect.Bottom >= platformRect.Top && playerRect.Bottom <= platformRect.Top + 15)
                    {
                        _playerY = platformRect.Top - Player.HeightRequest;
                        _playerVelocityY = 0;
                        landed = true;
                        _playerX += platform.Velocity;
                        _playerX = Math.Clamp(_playerX, 0, Math.Max(0, GameArea.Width - Player.WidthRequest));
                        break;
                    }
                }
            }
        }

        if (!landed && _playerY + Player.HeightRequest >= GameArea.Height)
        {
            _playerY = GameArea.Height - Player.HeightRequest;
            _playerVelocityY = 0;
            landed = true;
        }

        if (!landed && _playerY > GameArea.Height)
        {
            TriggerGameOver();
        }
    }

    private void UpdateVisuals()
    {
        AbsoluteLayout.SetLayoutBounds(Player, new Rect(_playerX, _playerY, Player.WidthRequest, Player.HeightRequest));
        AbsoluteLayout.SetLayoutFlags(Player, AbsoluteLayoutFlags.None);

        foreach (var platform in _platforms)
        {
            AbsoluteLayout.SetLayoutBounds(platform.View, new Rect(platform.X, platform.Y, platform.Width, platform.Height));
            AbsoluteLayout.SetLayoutFlags(platform.View, AbsoluteLayoutFlags.None);
        }
    }

    private void TriggerGameOver()
    {
        if (_gameOver)
        {
            return;
        }

        _gameOver = true;
        _gameRunning = false;
        _timer.Stop();
        ResetButton.IsVisible = true;
        StartButton.IsVisible = false;
        _moveLeft = false;
        _moveRight = false;
    }

    private sealed class PlatformInfo
    {
        public PlatformInfo(BoxView view, double velocity)
        {
            View = view;
            Velocity = velocity;
        }

        public BoxView View { get; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Velocity { get; set; }

        public double Width => View.WidthRequest;
        public double Height => View.HeightRequest;
    }
}
