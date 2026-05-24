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
        private const double BaseOverlayWidth = 356;
        private const double StatusOverlayWidth = 220;

        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _placementTimer;
        private readonly DispatcherTimer _cooldownTimer;
        private readonly HttpLcuTransport _transport;
        private readonly Dictionary<int, ImageSource> _iconCache;
        private readonly BenchSwapCooldown _benchSwapCooldown;
        private readonly WrapPanel _benchPanel;
        private readonly TextBlock _status;
        private readonly Forms.NotifyIcon _trayIcon;

        private LcuConnection _connection;
        private LcuClient _client;
        private ChampSelectSession _currentSession;
        private string _lastBenchKey;
        private bool _refreshing;
        private bool _allowClose;
        private DisplayMode _displayMode;
        private Forms.ToolStripMenuItem _overlayModeMenuItem;
        private Forms.ToolStripMenuItem _benchOnlyModeMenuItem;
        private readonly Style _championButtonStyle;

        public MainWindow()
        {
            _transport = new HttpLcuTransport();
            _iconCache = new Dictionary<int, ImageSource>();
            _benchSwapCooldown = new BenchSwapCooldown(TimeSpan.FromSeconds(3));
            _displayMode = LoadDisplayMode();

            Title = string.Empty;
            Width = WindowPlacement.CalculateOverlayWidth(BaseOverlayWidth);
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

            _cooldownTimer = new DispatcherTimer();
            _cooldownTimer.Tick += delegate
            {
                _cooldownTimer.Stop();
                _lastBenchKey = null;
                RefreshState();
            };
        }

        public void Dispose()
        {
            _timer.Stop();
            _placementTimer.Stop();
            _cooldownTimer.Stop();
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
                Hide();
            }
            base.OnClosing(e);
        }

        private Forms.NotifyIcon CreateTrayIcon()
        {
            var menu = new Forms.ContextMenuStrip();
            _overlayModeMenuItem = new Forms.ToolStripMenuItem("Overlay mode", null, delegate
            {
                SetDisplayMode(DisplayMode.Overlay);
            });
            _benchOnlyModeMenuItem = new Forms.ToolStripMenuItem("Bench-only mode", null, delegate
            {
                SetDisplayMode(DisplayMode.BenchOnly);
            });

            menu.Items.Add(_overlayModeMenuItem);
            menu.Items.Add(_benchOnlyModeMenuItem);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Refresh", null, delegate
            {
                RefreshState();
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

            UpdateDisplayModeMenu();
            icon.DoubleClick += delegate
            {
                RefreshState();
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
                    var phaseState = BenchWindowState.Decide(gameflowPhase, null, _displayMode);
                    _benchSwapCooldown.Update(gameflowPhase, null, DateTime.UtcNow);
                    _cooldownTimer.Stop();
                    _currentSession = null;
                    _lastBenchKey = null;
                    _benchPanel.Children.Clear();
                    SetTrayStatus(phaseState.Status, System.Drawing.SystemIcons.Application);
                    if (phaseState.ShouldShow)
                    {
                        ShowStatusOnly(phaseState.Status);
                    }
                    else
                    {
                        Hide();
                    }

                    return;
                }

                _currentSession = _client.GetBenchAwareChampSelectSession();
                var now = DateTime.UtcNow;
                _benchSwapCooldown.Update(gameflowPhase, _currentSession, now);
                var windowState = BenchWindowState.Decide(gameflowPhase, _currentSession, _displayMode);
                if (windowState.ShouldRenderBench)
                {
                    SetTrayStatus("ARAM bench ready", System.Drawing.SystemIcons.Information);
                    Width = WindowPlacement.CalculateOverlayWidth(BaseOverlayWidth);
                    var swapCooldownActive = _benchSwapCooldown.IsActive(now);
                    RenderBench(_currentSession.BenchChampions, swapCooldownActive);
                    ScheduleCooldownRefresh(now);
                    _status.Text = windowState.Status;
                    _status.Visibility = Visibility.Collapsed;
                    _benchPanel.Margin = new Thickness(0);
                    ShowNearLeagueClientTop();
                }
                else if (windowState.ShouldShow)
                {
                    _lastBenchKey = null;
                    _benchPanel.Children.Clear();
                    SetTrayStatus(windowState.Status, System.Drawing.SystemIcons.Application);
                    ShowStatusOnly(windowState.Status);
                }
                else
                {
                    _cooldownTimer.Stop();
                    _lastBenchKey = null;
                    _benchPanel.Children.Clear();
                    SetTrayStatus(windowState.Status, System.Drawing.SystemIcons.Application);
                    Hide();
                }
            }
            catch (Exception ex)
            {
                _status.Text = "LCU error: " + ex.Message;
                SetTrayStatus("LCU error", System.Drawing.SystemIcons.Warning);
                Hide();
            }
            finally
            {
                _refreshing = false;
            }
        }

        private void SetWaiting(string message)
        {
            _cooldownTimer.Stop();
            _currentSession = null;
            _lastBenchKey = null;
            _benchPanel.Children.Clear();
            SetTrayStatus(message, System.Drawing.SystemIcons.Application);
            Hide();
        }

        private void SetTrayStatus(string status, System.Drawing.Icon icon)
        {
            var text = "ARAM Bench Swap: " + status;
            if (text.Length > 63)
            {
                text = text.Substring(0, 60) + "...";
            }

            _trayIcon.Icon = icon;
            _trayIcon.Text = text;
        }

        private void ShowStatusOnly(string message)
        {
            _cooldownTimer.Stop();
            Width = StatusOverlayWidth;
            _status.Text = message;
            _status.Visibility = Visibility.Visible;
            _benchPanel.Margin = new Thickness(0);
            ShowNearLeagueClientTop();
        }

        private void ScheduleCooldownRefresh(DateTime nowUtc)
        {
            var delay = CooldownRefreshDelay.Calculate(_benchSwapCooldown.Remaining(nowUtc));
            if (delay <= TimeSpan.Zero)
            {
                _cooldownTimer.Stop();
                return;
            }

            _cooldownTimer.Stop();
            _cooldownTimer.Interval = delay;
            _cooldownTimer.Start();
        }

        private void SetDisplayMode(DisplayMode displayMode)
        {
            _displayMode = displayMode;
            SaveDisplayMode(displayMode);
            UpdateDisplayModeMenu();
            RefreshState();
        }

        private void UpdateDisplayModeMenu()
        {
            if (_overlayModeMenuItem != null)
            {
                _overlayModeMenuItem.Checked = _displayMode == DisplayMode.Overlay;
            }

            if (_benchOnlyModeMenuItem != null)
            {
                _benchOnlyModeMenuItem.Checked = _displayMode == DisplayMode.BenchOnly;
            }
        }

        private static DisplayMode LoadDisplayMode()
        {
            try
            {
                var path = GetSettingsPath();
                if (!File.Exists(path))
                {
                    return DisplayMode.Overlay;
                }

                return DisplayModePreference.Parse(File.ReadAllText(path));
            }
            catch
            {
                return DisplayMode.Overlay;
            }
        }

        private static void SaveDisplayMode(DisplayMode displayMode)
        {
            try
            {
                var path = GetSettingsPath();
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, DisplayModePreference.Format(displayMode));
            }
            catch
            {
            }
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AramBenchSwap",
                "settings.txt");
        }

        private void RenderBench(IEnumerable<BenchChampion> benchChampions, bool swapCooldownActive)
        {
            var champions = benchChampions.ToList();
            var key = swapCooldownActive + "|" + string.Join(",", champions.Select(champion => champion.ChampionId + ":" + champion.IsSelectable).ToArray());
            if (key == _lastBenchKey)
            {
                return;
            }

            _lastBenchKey = key;
            _benchPanel.Children.Clear();

            foreach (var champion in champions)
            {
                var button = CreateChampionButton(champion, swapCooldownActive);
                _benchPanel.Children.Add(button);
            }
        }

        private Button CreateChampionButton(BenchChampion champion, bool swapCooldownActive)
        {
            var button = new Button
            {
                Width = 48,
                Height = 48,
                Margin = new Thickness(3),
                Padding = new Thickness(0),
                Tag = champion.ChampionId,
                ToolTip = GetChampionButtonTooltip(champion, swapCooldownActive),
                Style = _championButtonStyle
            };

            var icon = LoadChampionIcon(champion.ChampionId);
            if (icon != null)
            {
                var image = new Image
                {
                    Source = champion.IsSelectable ? icon : CreateDisabledIcon(icon),
                    Stretch = Stretch.UniformToFill
                };
                if (!champion.IsSelectable)
                {
                    image.Opacity = 0.45;
                }
                else if (swapCooldownActive)
                {
                    image.Opacity = 0.72;
                }

                button.Content = image;
            }
            else
            {
                button.Content = new TextBlock
                {
                    Text = champion.ChampionId.ToString(),
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            button.IsEnabled = champion.IsSelectable && !swapCooldownActive;
            if (button.IsEnabled)
            {
                button.Click += OnChampionClicked;
            }

            return button;
        }

        private static string GetChampionButtonTooltip(BenchChampion champion, bool swapCooldownActive)
        {
            if (!champion.IsSelectable)
            {
                return "Champion is on the bench but not selectable on this account.";
            }

            if (swapCooldownActive)
            {
                return "Bench swaps are available after the client cooldown.";
            }

            return "Swap champion " + champion.ChampionId;
        }

        private static ImageSource CreateDisabledIcon(ImageSource icon)
        {
            var bitmapSource = icon as BitmapSource;
            if (bitmapSource == null)
            {
                return icon;
            }

            var grayscale = new FormatConvertedBitmap();
            grayscale.BeginInit();
            grayscale.Source = bitmapSource;
            grayscale.DestinationFormat = PixelFormats.Gray8;
            grayscale.EndInit();
            grayscale.Freeze();
            return grayscale;
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

                if (_benchSwapCooldown.IsActive(DateTime.UtcNow))
                {
                    _status.Text = "Bench swaps are available after the client cooldown.";
                    return;
                }

                var result = _client.RefreshAndSwapBenchChampion(championId);
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
            disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 1.0));
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
