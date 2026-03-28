using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using CameraStreaming.Models;
using CameraStreaming.Services;
using CameraStreaming.Views;
using WpfWindow = System.Windows.Window;

namespace CameraStreaming
{
    public partial class MainWindow : WpfWindow
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private CameraService _cameraService = new CameraService();
        private CameraConfig _config = new CameraConfig();
        private bool _isConnecting;
        private readonly LocalizationService _lang = LocalizationService.Instance;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            _lang.PropertyChanged += Lang_PropertyChanged;
            UpdateDynamicTexts();
        }

        private void Lang_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateDynamicTexts();
        }

        private void UpdateDynamicTexts()
        {
            runShortcutPrefix.Text = _lang["Shortcut_Prefix"];
            runPreviewDesc.Text = _lang["Shortcut_Preview"];
            runMirrorDesc.Text = _lang["Shortcut_Mirror"];
            txtLoadingMsg.Text = _lang["Connecting"];
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _config = ConfigService.LoadConfig();
            if (string.IsNullOrEmpty(_config.Language))
            {
                _config.Language = LocalizationService.DetectSystemLanguage();
                ConfigService.SaveConfig(_config);
            }
            _lang.Language = _config.Language;
            _cameraService.FrameCaptured += OnFrameCaptured;
            await ConnectCameraAsync();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _cameraService.FrameCaptured -= OnFrameCaptured;
            _cameraService.Dispose();
        }

        private void ShowLoading(string message)
        {
            _isConnecting = true;
            btnSettings.IsEnabled = false;
            btnRefresh.IsEnabled = false;
            txtLoadingMsg.Text = message;
            loadingOverlay.Visibility = Visibility.Visible;
        }

        private void HideLoading()
        {
            _isConnecting = false;
            btnSettings.IsEnabled = true;
            btnRefresh.IsEnabled = true;
            loadingOverlay.Visibility = Visibility.Collapsed;
        }

        private async System.Threading.Tasks.Task ConnectCameraAsync()
        {
            ShowLoading(_lang["Connecting"]);

            txtStatus.Text = _lang["Connecting"];
            txtStatus.Foreground = System.Windows.Media.Brushes.Gray;

            await System.Threading.Tasks.Task.Delay(100);

            var result = await _cameraService.StartAsync(_config);

            await System.Threading.Tasks.Task.Delay(200);

            HideLoading();

            switch (result)
            {
                case CameraService.ConnectResult.Success:
                    txtStatus.Text = string.Format(_lang["Connected"], _config.Width, _config.Height, _config.Fps);
                    txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    break;
                case CameraService.ConnectResult.NoCamera:
                    txtStatus.Text = _lang["NoCameraDetected"];
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                    ShowCenteredMessage(_lang["NoCameraDetected_Msg"], _lang["Tip"], MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                case CameraService.ConnectResult.CameraInUse:
                    txtStatus.Text = _lang["CameraInUse"];
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                    ShowCenteredMessage(_lang["CameraInUse_Msg"], _lang["Tip"], MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                default:
                    txtStatus.Text = _lang["ConnectFailed"];
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                    break;
            }
        }

        private void OnFrameCaptured(Mat frame)
        {
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var bitmap = frame.ToWriteableBitmap();
                    cameraImage.Source = bitmap;
                }
                catch
                {
                }
            });
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                OpenFramePreview();
                e.Handled = true;
            }
            else if (e.Key == Key.M && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ToggleMirror();
                e.Handled = true;
            }
        }

        private void OpenFramePreview()
        {
            if (!_cameraService.IsConnected)
            {
                ShowCenteredMessage(_lang["CameraNotConnected"], _lang["Tip"], MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var liveWindow = new LiveStreamWindow(_cameraService, _config.WindowShape);
            liveWindow.Closed += (s, e) =>
            {
                this.Show();
            };
            liveWindow.Owner = this;
            liveWindow.Topmost = true;
            liveWindow.Activated += (s, e) => liveWindow.Topmost = false;
            this.Hide();
            liveWindow.Show();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnecting) return;

            var settingsWindow = new SettingsWindow(_config);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                var oldIndex = _config.Index;
                _config = settingsWindow.Config;
                ConfigService.SaveConfig(_config);

                // 只有切换摄像头源才需要重新连接
                if (_config.Index != oldIndex)
                {
                    var unused = ConnectCameraAsync();
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnecting) return;
            var unused = ConnectCameraAsync();
        }

        private void ToggleMirror()
        {
            _cameraService.Mirror = !_cameraService.Mirror;
            if (_cameraService.Mirror)
            {
                btnMirror.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3D, 0x5A, 0x80));
                btnMirror.Foreground = System.Windows.Media.Brushes.White;
            }
            else
            {
                btnMirror.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3D, 0x3D, 0x3D));
                btnMirror.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE0, 0xE0, 0xE0));
            }
        }

        private void btnMirror_Click(object sender, RoutedEventArgs e)
        {
            ToggleMirror();
        }

        private void btnLive_Click(object sender, RoutedEventArgs e)
        {
            OpenFramePreview();
        }

        private static void ShowCenteredMessage(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            var handle = new System.Threading.AutoResetEvent(false);
            var thread = new System.Threading.Thread(() =>
            {
                handle.Set();
                var result = MessageBox.Show(messageBoxText, caption, button, icon);
            });
            thread.IsBackground = true;
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            handle.WaitOne();

            // 等待 MessageBox 窗口出现并居中
            for (int i = 0; i < 50; i++)
            {
                System.Threading.Thread.Sleep(10);
                var msgHwnd = FindWindow(null, caption);
                if (msgHwnd != IntPtr.Zero)
                {
                    CenterMessageBox(msgHwnd);
                    break;
                }
            }
            thread.Join();
        }

        private static void CenterMessageBox(IntPtr msgHwnd)
        {
            if (Application.Current?.MainWindow is not WpfWindow owner) return;

            owner.Dispatcher.Invoke(() =>
            {
                if (!GetWindowRect(msgHwnd, out var msgRect)) return;

                var ownerLeft = owner.Left;
                var ownerTop = owner.Top;
                var ownerWidth = owner.ActualWidth;
                var ownerHeight = owner.ActualHeight;

                var msgWidth = msgRect.Right - msgRect.Left;
                var msgHeight = msgRect.Bottom - msgRect.Top;
                var x = (int)(ownerLeft + (ownerWidth - msgWidth) / 2);
                var y = (int)(ownerTop + (ownerHeight - msgHeight) / 2);

                SetWindowPos(msgHwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
            });
        }
    }
}
