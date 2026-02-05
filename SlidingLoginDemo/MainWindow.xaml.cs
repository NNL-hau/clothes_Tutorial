using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySqlConnector;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;

namespace SlidingLoginDemo
{
    public partial class MainWindow : Window
    {
        private bool _isSignUp = false;

        private const int SlideMs = 550;
        private const int FadeMs = 350;
        private const double FormShift = 26;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                WireButtons();
                ApplyInitialLayout();
                InitializeSnow();
            };

            SizeChanged += (_, __) =>
            {
                SetOverlayHalfWidth();
                OverlayTransform.X = _isSignUp ? 0 : (Card.ActualWidth / 2.0);
            };
        }

        private void WireButtons()
        {
            GoSignUpBtn.Click += (_, __) => ShowSignUp();
            GoSignInBtn.Click += (_, __) => ShowSignIn();
        }
        private async void GoogleSignIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // [IMPORTANT] Replace these with your actual Client ID and Client Secret from Google Cloud Console
                string clientId = "1043095171292-sn0sagtrr1ltofjnh4vu3ileu4dpqn4c.apps.googleusercontent.com";
                string clientSecret = "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";
                

                if (clientId.Contains("YOUR_CLIENT_ID_HERE"))
                {
                    MessageBox.Show("Vui lòng cấu hình Client ID và Client Secret trong MainWindow.xaml.cs");
                    return;
                }

                string[] scopes = { "email", "profile" };

                UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    scopes,
                    "user",
                    CancellationToken.None);

                var settings = new GoogleJsonWebSignature.ValidationSettings();
                // Note: Getting user info after authorization. 
                // For a desktop app, we often use the IdToken if available, 
                // but here we demonstrate the standard flow.
                
                // In a real scenario, you might want to call the UserInfo API
                // For simplicity, we'll assume the authentication was successful if credential is not null
                if (credential != null && credential.Token != null)
                {
                    // For this demo, let's pretend we got the email from the token/profile
                    // In a production app, you'd use the access token to call https://www.googleapis.com/oauth2/v2/userinfo
                    
                    // Since WPF GoogleWebAuthorizationBroker doesn't easily return the IdToken for payload extraction without extra steps,
                    // we'll just show a success message and explain how to link it.
                    
                    MessageBox.Show("Thực hiện đăng nhập Google thành công! Credential đã được nhận.");
                    
                    // Logic to check/create user in database would go here:
                    // string email = "user_from_google@gmail.com";
                    // ... check database and log in ...
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đăng nhập Google: {ex.Message}");
            }
        }


        private void ApplyInitialLayout()
        {
            SetOverlayHalfWidth();

            _isSignUp = false;
            OverlayTransform.X = Card.ActualWidth / 2.0;

            OverlayRightText.Opacity = 1;
            OverlayLeftText.Opacity = 0;

            // ✅ Fix click overlay
            OverlayRightText.IsHitTestVisible = true;
            OverlayLeftText.IsHitTestVisible = false;

            SignInPanel.Visibility = Visibility.Visible;
            SignUpPanel.Visibility = Visibility.Collapsed;

            SignInPanel.Opacity = 1;
            SignUpPanel.Opacity = 0;

            SignInPanel.IsHitTestVisible = true;
            SignUpPanel.IsHitTestVisible = false;

            SignInTransform.X = 0;
            SignUpTransform.X = 0;
        }

        private void SetOverlayHalfWidth()
        {
            Overlay.Width = Card.ActualWidth / 2.0;
        }

      private void ShowSignUp()
{
    if (_isSignUp) return;
    _isSignUp = true;

    SignUpPanel.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                WatermarkService.RefreshWatermarks(SignUpPanel);
            }), System.Windows.Threading.DispatcherPriority.Loaded);


            AnimateX(OverlayTransform, 0, SlideMs);

    Fade(OverlayRightText, 0, FadeMs);
    Fade(OverlayLeftText, 1, FadeMs);

    // ✅ Fix click overlay
    OverlayRightText.IsHitTestVisible = false;
    OverlayLeftText.IsHitTestVisible = true;

    SignInPanel.IsHitTestVisible = false;
    SignUpPanel.IsHitTestVisible = true;

    FadeAndCollapse(SignInPanel, 0, FadeMs);
    Fade(SignUpPanel, 1, FadeMs);

    AnimateX(SignInTransform, -FormShift, SlideMs);
    SignUpTransform.X = +FormShift;
    AnimateX(SignUpTransform, 0, SlideMs);
            WatermarkService.HideWatermarks(SignInPanel);

        }

        private void ShowSignIn()
        {
            if (!_isSignUp) return;
            _isSignUp = false;

            SignInPanel.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                WatermarkService.RefreshWatermarks(SignInPanel);
            }), System.Windows.Threading.DispatcherPriority.Loaded);


            AnimateX(OverlayTransform, Card.ActualWidth / 2.0, SlideMs);

            Fade(OverlayRightText, 1, FadeMs);
            Fade(OverlayLeftText, 0, FadeMs);

            // ✅ Fix click overlay
            OverlayRightText.IsHitTestVisible = true;
            OverlayLeftText.IsHitTestVisible = false;

            SignInPanel.IsHitTestVisible = true;
            SignUpPanel.IsHitTestVisible = false;

            Fade(SignInPanel, 1, FadeMs);
            FadeAndCollapse(SignUpPanel, 0, FadeMs);

            AnimateX(SignUpTransform, +FormShift, SlideMs);
            SignInTransform.X = -FormShift;
            AnimateX(SignInTransform, 0, SlideMs);
            WatermarkService.HideWatermarks(SignUpPanel);

        }

        private static void AnimateX(DependencyObject target, double to, int ms)
        {
            var anim = new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(ms),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            if (target is TranslateTransform tt)
                tt.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        private static void Fade(UIElement el, double to, int ms)
        {
            var anim = new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(ms),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            el.BeginAnimation(OpacityProperty, anim);
        }

        private static void FadeAndCollapse(UIElement el, double to, int ms)
        {
            var anim = new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(ms),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            anim.Completed += (_, __) =>
            {
                if (to == 0)
                    el.Visibility = Visibility.Collapsed;
            };

            el.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            string username = UserSignIn.Text.Trim();
            string password = PassIn.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter username and password.");
                return;
            }

            try
            {
                // Fetch hashed password from DB
                string query = "SELECT Password FROM Users WHERE Username = @Username";
                var parameters = new MySqlParameter[]
                {
                    new MySqlParameter("@Username", username)
                };

                object? result = DatabaseHelper.ExecuteScalar(query, parameters);
                
                if (result != null)
                {
                    string hashedPassword = result.ToString() ?? "";
                    
                    // Verify password using BCrypt
                    if (BCrypt.Net.BCrypt.Verify(password, hashedPassword))
                    {
                        MessageBox.Show("Sign In successful!");
                        return;
                    }
                }

                MessageBox.Show("Invalid username or password.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during sign in: {ex.Message}");
            }
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            string name = NameUp.Text.Trim();
            string address = AddressUp.Text.Trim();
            string phone = PhoneUp.Text.Trim();
            string password = PassUp.Password.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please fill in Username and Password.");
                return;
            }

            try
            {
                // Check if user already exists
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                var checkParams = new MySqlParameter[]
                {
                    new MySqlParameter("@Username", name)
                };

                long count = Convert.ToInt64(DatabaseHelper.ExecuteScalar(checkQuery, checkParams) ?? 0);
                if (count > 0)
                {
                    MessageBox.Show("User with this username already exists.");
                    return;
                }

                // Hash password using BCrypt
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                string insertQuery = "INSERT INTO Users (Username, Password, Address, Phone) VALUES (@Username, @Password, @Address, @Phone)";
                var insertParams = new MySqlParameter[]
                {
                    new MySqlParameter("@Username", name),
                    new MySqlParameter("@Password", hashedPassword),
                    new MySqlParameter("@Address", address),
                    new MySqlParameter("@Phone", phone)
                };

                int result = DatabaseHelper.ExecuteNonQuery(insertQuery, insertParams);

                if (result > 0)
                {
                    MessageBox.Show("Sign Up successful! You can now sign in.");
                    ShowSignIn();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during sign up: {ex.Message}");
            }
        }
        private readonly List<Snowflake> _snowflakes = new List<Snowflake>();
        private readonly Random _random = new Random();
        private System.Windows.Threading.DispatcherTimer? _snowTimer;
        private readonly string[] _snowflakeChars = { "❄", "❅", "❆" };
        private double _animationTime = 0;

        private void InitializeSnow()
        {
            _snowTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(20)
            };
            _snowTimer.Tick += OnSnowTick;
            _snowTimer.Start();
        }

        private void OnSnowTick(object? sender, EventArgs e)
        {
            if (SnowField == null || SnowField.ActualWidth <= 0 || SnowField.ActualHeight <= 0) return;

            _animationTime += 0.02;

            try
            {
                // Spawn new flakes
                if (_snowflakes.Count < 50 && _random.NextDouble() < 0.2)
                {
                    double size = _random.Next(14, 30);
                    double depth = size / 30.0;

                    var textBlock = new TextBlock
                    {
                        Text = _snowflakeChars[_random.Next(_snowflakeChars.Length)],
                        FontSize = size,
                        Foreground = new SolidColorBrush(Color.FromArgb(
                            (byte)(_random.Next(150, 230) * depth), 
                            255, 255, 255)),
                        IsHitTestVisible = false,
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    var transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new RotateTransform(_random.Next(0, 360)));
                    textBlock.RenderTransform = transformGroup;
                    
                    var flake = new Snowflake(textBlock)
                    {
                        X = _random.NextDouble() * SnowField.ActualWidth,
                        Y = -40,
                        Speed = (_random.NextDouble() * 1.5 + 0.8) * depth,
                        HorizontalSpeed = _random.NextDouble() * 0.4 - 0.2,
                        RotationSpeed = (_random.NextDouble() * 4.0 - 2.0) * depth,
                        OscillationAmplitude = _random.NextDouble() * 2.0 + 1.0,
                        OscillationFrequency = _random.NextDouble() * 1.5 + 0.5,
                        OscillationPhase = _random.NextDouble() * Math.PI * 2
                    };
                    
                    _snowflakes.Add(flake);
                    SnowField.Children.Add(textBlock);
                }

                // Update existing flakes
                for (int i = _snowflakes.Count - 1; i >= 0; i--)
                {
                    var flake = _snowflakes[i];
                    flake.Y += flake.Speed;
                    
                    double sway = Math.Sin(_animationTime * flake.OscillationFrequency + flake.OscillationPhase) * flake.OscillationAmplitude;
                    flake.X += flake.HorizontalSpeed + sway;

                    if (flake.Shape.RenderTransform is TransformGroup tg && tg.Children[0] is RotateTransform rt)
                    {
                        rt.Angle += flake.RotationSpeed;
                    }

                    if (flake.Y > SnowField.ActualHeight + 40)
                    {
                        SnowField.Children.Remove(flake.Shape);
                        _snowflakes.RemoveAt(i);
                    }
                    else
                    {
                        Canvas.SetLeft(flake.Shape, flake.X);
                        Canvas.SetTop(flake.Shape, flake.Y);
                    }
                }
            }
            catch (Exception) { }
        }

        private class Snowflake
        {
            public TextBlock Shape { get; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Speed { get; set; }
            public double HorizontalSpeed { get; set; }
            public double RotationSpeed { get; set; }
            public double OscillationAmplitude { get; set; }
            public double OscillationFrequency { get; set; }
            public double OscillationPhase { get; set; }

            public Snowflake(TextBlock shape)
            {
                Shape = shape;
            }
        }
    }
}