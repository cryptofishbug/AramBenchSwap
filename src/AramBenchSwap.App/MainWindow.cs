using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Threading;
using AramBenchSwap.Core;
using Forms = System.Windows.Forms;

namespace AramBenchSwap.App
{
    public sealed class MainWindow : Window, IDisposable
    {
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _placementTimer;
        private readonly HttpLcuTransport _transport;
        private readonly Dictionary<int, ImageSource> _iconCache;
        private readonly WrapPanel _benchPanel;
        private readonly TextBlock _status;
        private readonly Forms.NotifyIcon _trayIcon;

        private LcuConnection _connection;
        private LcuClient _client;
        private ChampSelectSession _currentSession;
        private string _lastBenchKey;
        private bool _refreshing;
        private bool _manualOpen = true;
        private bool _allowClose;
        private readonly Style _championButtonStyle;

        public MainWindow()
        {
            _transport = new HttpLcuTransport();
            _iconCache = new Dictionary<int, ImageSource>();

            Title = string.Empty;
            Width = WindowPlacement.CalculateOverlayWidth(356);
            SizeToContent = SizeToContent.Height;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Background = Brushes.Transparent;

            _championButtonStyle = CreateChampionButtonStyle();

            var root = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 18, 24)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 80, 96)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                SnapsToDevicePixels = true
            };
            root.MouseLeftButtonDown += OnPanelMouseLeftButtonDown;

            var content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            _benchPanel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            _status = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(190, 198, 210)),
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Text = "Waiting for League Client..."
            };

            content.Children.Add(_benchPanel);
            content.Children.Add(_status);
            root.Child = content;
            Content = root;

            _trayIcon = CreateTrayIcon();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += delegate { RefreshState(); };
            _timer.Start();

            _placementTimer = new DispatcherTimer();
            _placementTimer.Interval = TimeSpan.FromMilliseconds(100);
            _placementTimer.Tick += delegate
            {
                if (IsVisible)
                {
                    PositionNearLeagueClientTop();
                }
            };
            _placementTimer.Start();
        }

        public void Dispose()
        {
            _timer.Stop();
            _placementTimer.Stop();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            if (_allowClose)
            {
                e.Cancel = false;
            }
            else
            {
                _manualOpen = false;
                Hide();
            }
            base.OnClosing(e);
        }

        private Forms.NotifyIcon CreateTrayIcon()
        {
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Show", null, delegate
            {
                _manualOpen = true;
                ShowNearLeagueClientTop();
            });
            menu.Items.Add("Exit", null, delegate
            {
                _allowClose = true;
                Application.Current.Shutdown();
            });

            var icon = new Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Text = "ARAM Bench Swap",
                ContextMenuStrip = menu,
                Visible = true
            };

            icon.DoubleClick += delegate
            {
                _manualOpen = true;
                ShowNearLeagueClientTop();
            };
            return icon;
        }

        private void RefreshState()
        {
            if (_refreshing)
            {
                return;
            }

            _refreshing = true;
            try
            {
                LcuConnection connection;
                if (!LeagueClientLocator.TryReadConnection(out connection))
                {
                    SetWaiting("Waiting for League Client...");
                    return;
                }

                if (_connection == null || _connection.Port != connection.Port || _connection.Password != connection.Password)
                {
                    _connection = connection;
                    _client = new LcuClient(_connection, _transport);
                    _iconCache.Clear();
                }

                var gameflowPhase = _client.GetGameflowPhase();
                if (gameflowPhase != "ChampSelect")
                {
                    _currentSession = null;
                    _lastBenchKey = null;
                    _benchPanel.Children.Clear();
                    Hide();
                    return;
                }

                _currentSession = _client.GetChampSelectSession();
                var windowState = BenchWindowState.Decide(gameflowPhase, _currentSession, false);
                if (windowState.ShouldRenderBench)
                {
                    RenderBench(_currentSession.BenchChampions);
                    _status.Text = windowState.Status;
                    _status.Visibility = Visibility.Collapsed;
                    _benchPanel.Margin = new Thickness(0);
                    ShowNearLeagueClientTop();
                }
                else if (windowState.ShouldShow)
                {
                    _benchPanel.Children.Clear();
                    _lastBenchKey = null;
                    _status.Text = windowState.Status;
                    _status.Visibility = Visibility.Visible;
                    _benchPanel.Margin = new Thickness(0, 0, 0, 5);
                    ShowNearLeagueClientTop();
                }
                else
                {
                    SetWaiting("Waiting for ARAM bench...");
                }
            }
            catch (Exception ex)
            {
                _status.Text = "LCU error: " + ex.Message;
                _trayIcon.Text = "ARAM Bench Swap: LCU error";
                Hide();
            }
            finally
            {
                _refreshing = false;
            }
        }

        private void SetWaiting(string message)
        {
            _currentSession = null;
            _lastBenchKey = null;
            _benchPanel.Children.Clear();
            _status.Text = message;
            _status.Visibility = Visibility.Visible;
            _benchPanel.Margin = new Thickness(0, 0, 0, 5);
            _trayIcon.Text = "ARAM Bench Swap";
            if (!_manualOpen)
            {
                Hide();
            }
            else
            {
                ShowNearLeagueClientTop();
            }
        }

        private void RenderBench(IEnumerable<BenchChampion> benchChampions)
        {
            var champions = benchChampions.ToList();
            var key = string.Join(",", champions.Select(champion => champion.ChampionId.ToString()).ToArray());
            if (key == _lastBenchKey)
            {
                return;
            }

            _lastBenchKey = key;
            _benchPanel.Children.Clear();

            foreach (var champion in champions)
            {
                var button = CreateChampionButton(champion.ChampionId);
                _benchPanel.Children.Add(button);
            }
        }

        private Button CreateChampionButton(int championId)
        {
            var button = new Button
            {
                Width = 48,
                Height = 48,
                Margin = new Thickness(3),
                Padding = new Thickness(0),
                Tag = championId,
                ToolTip = "Swap champion " + championId,
                Style = _championButtonStyle
            };

            var icon = LoadChampionIcon(championId);
            if (icon != null)
            {
                button.Content = new Image
                {
                    Source = icon,
                    Stretch = Stretch.UniformToFill
                };
            }
            else
            {
                button.Content = new TextBlock
                {
                    Text = championId.ToString(),
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            button.Click += OnChampionClicked;
            return button;
        }

        private ImageSource LoadChampionIcon(int championId)
        {
            ImageSource cached;
            if (_iconCache.TryGetValue(championId, out cached))
            {
                return cached;
            }

            if (_connection == null)
            {
                return null;
            }

            try
            {
                var url = _connection.BaseUrl + "/lol-game-data/assets/v1/champion-icons/" + championId + ".png";
                var bytes = _transport.GetBytes(url, _connection.Password);
                using (var stream = new MemoryStream(bytes))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    _iconCache[championId] = image;
                    return image;
                }
            }
            catch
            {
                return null;
            }
        }

        private void OnChampionClicked(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var championId = (int)button.Tag;
            button.IsEnabled = false;

            try
            {
                if (_client == null || _currentSession == null)
                {
                    _status.Text = "No active bench session.";
                    return;
                }

                var result = _client.SwapBenchChampion(_currentSession, championId);
                _status.Text = result.Message;
                _lastBenchKey = null;
                RefreshState();
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private void ShowNearLeagueClientTop()
        {
            if (!IsVisible)
            {
                Show();
            }

            PositionNearLeagueClientTop();
        }

        private void PositionNearLeagueClientTop()
        {
            UpdateLayout();
            var source = PresentationSource.FromVisual(this);
            var dpiScaleX = source == null ? 1.0 : source.CompositionTarget.TransformToDevice.M11;
            var dpiScaleY = source == null ? 1.0 : source.CompositionTarget.TransformToDevice.M22;
            var anchor = TryGetLeagueClientBounds(dpiScaleX, dpiScaleY) ??
                new WindowBounds(SystemParameters.WorkArea.Left, SystemParameters.WorkArea.Top, SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);
            var position = WindowPlacement.CalculateAboveTopCenter(anchor, ActualWidth, ActualHeight, 8, SystemParameters.WorkArea.Top);
            if (Math.Abs(Left - position.Left) > 0.5)
            {
                Left = position.Left;
            }

            if (Math.Abs(Top - position.Top) > 0.5)
            {
                Top = position.Top;
            }
        }

        private static WindowBounds TryGetLeagueClientBounds(double dpiScaleX, double dpiScaleY)
        {
            foreach (var process in Process.GetProcessesByName("LeagueClientUx"))
            {
                using (process)
                {
                    var handle = process.MainWindowHandle;
                    if (handle == IntPtr.Zero || !IsWindowVisible(handle))
                    {
                        continue;
                    }

                    NativeRect rect;
                    if (!GetWindowRect(handle, out rect))
                    {
                        continue;
                    }

                    var width = rect.Right - rect.Left;
                    var height = rect.Bottom - rect.Top;
                    if (width > 200 && height > 200)
                    {
                            return new WindowBounds(rect.Left, rect.Top, width, height, dpiScaleX, dpiScaleY);
                    }
                }
            }

            return null;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static Style CreateChampionButtonStyle()
        {
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "Chrome";
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(28, 34, 43)));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(88, 101, 121)));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            borderFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);

            var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            borderFactory.AppendChild(presenterFactory);

            var template = new ControlTemplate(typeof(Button));
            template.VisualTree = borderFactory;

            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(126, 146, 176)), "Chrome"));
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(34, 42, 54)), "Chrome"));
            template.Triggers.Add(hoverTrigger);

            var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(45, 56, 72)), "Chrome"));
            template.Triggers.Add(pressedTrigger);

            var disabledTrigger = new Trigger { Property = Button.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
            template.Triggers.Add(disabledTrigger);

            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Button.TemplateProperty, template));
            style.Setters.Add(new Setter(Button.FocusVisualStyleProperty, null));
            return style;
        }

        private void OnPanelMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed || IsInsideButton(e.OriginalSource as DependencyObject))
            {
                return;
            }

            DragMove();
        }

        private static bool IsInsideButton(DependencyObject source)
        {
            while (source != null)
            {
                if (source is Button)
                {
                    return true;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }
    }
}
