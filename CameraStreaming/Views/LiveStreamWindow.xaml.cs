using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using CameraStreaming.Services;
using log4net;
using WpfWindow = System.Windows.Window;
using WpfPoint = System.Windows.Point;

namespace CameraStreaming.Views
{
    public partial class LiveStreamWindow : WpfWindow
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(LiveStreamWindow));

        private readonly CameraService _cameraService;
        private readonly LocalizationService _lang = LocalizationService.Instance;

        private const int GRIP_SIZE = 20;
        private readonly bool _isCircle;

        private Mat? _latestFrame;
        private readonly object _frameLock = new object();
        private volatile bool _renderQueued;

        public LiveStreamWindow(CameraService cameraService, string windowShape = "circle")
        {
            InitializeComponent();
            _cameraService = cameraService;
            _isCircle = windowShape != "square";
            _cameraService.FrameCaptured += OnFrameCaptured;
            SourceInitialized += (_, _) => ApplyShape();
            Log.Info($"直播窗口创建: 窗口形状={windowShape}");
        }

        #region Frame Rendering

        private void OnFrameCaptured(Mat frame)
        {
            lock (_frameLock)
            {
                _latestFrame?.Dispose();
                _latestFrame = frame.Clone();
            }

            if (!_renderQueued)
            {
                _renderQueued = true;
                Dispatcher.BeginInvoke(RenderLatestFrame);
            }
        }

        private void RenderLatestFrame()
        {
            _renderQueued = false;

            if (!IsLoaded) return;

            Mat? frame = null;
            lock (_frameLock)
            {
                if (_latestFrame != null)
                {
                    frame = _latestFrame;
                    _latestFrame = null;
                }
            }

            if (frame != null)
            {
                try
                {
                    var bitmap = frame.ToWriteableBitmap();
                    previewImage.Source = bitmap;
                }
                catch { }
                frame.Dispose();
            }
        }

        #endregion

        #region Window Lifecycle

        private void Window_Closed(object sender, EventArgs e)
        {
            Log.Info("直播窗口关闭");
            _cameraService.FrameCaptured -= OnFrameCaptured;
            lock (_frameLock)
            {
                _latestFrame?.Dispose();
                _latestFrame = null;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                DuplicateWindow();
                e.Handled = true;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateClip();
        }

        #endregion

        #region Shape

        private void ApplyShape()
        {
            if (_isCircle)
            {
                windowBackground.Clip = circleClip;
                Background = new SolidColorBrush(Color.FromArgb(1, 255, 255, 255));
            }
            else
            {
                windowBackground.Clip = null;
                Background = new SolidColorBrush(Colors.Transparent);
            }
            UpdateClip();
        }

        private void UpdateClip()
        {
            double halfW = ActualWidth / 2;
            double halfH = ActualHeight / 2;
            circleClip.RadiusX = halfW;
            circleClip.RadiusY = halfH;
            circleClip.Center = new WpfPoint(halfW, halfH);
        }

        #endregion

        #region Controls Visibility

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            closeOverlay.Opacity = 1;
            resizeOverlay.Opacity = 1;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            closeOverlay.Opacity = 0;
            resizeOverlay.Opacity = 0;
        }

        #endregion

        #region Button Clicks

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void resizeOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);
            ResizeWindow(ResizeDirection.BottomRight, pos);
            e.Handled = true;
        }

        private void DuplicateWindow()
        {
            Log.Info("复制直播窗口");
            var clone = new LiveStreamWindow(_cameraService, _isCircle ? "circle" : "square");
            clone.Width = ActualWidth;
            clone.Height = ActualHeight;
            clone.Left = Left + 30;
            clone.Top = Top + 30;
            clone.WindowStartupLocation = WindowStartupLocation.Manual;
            clone.Owner = Owner;
            clone.Show();
        }

        #endregion

        #region Drag & Resize

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            var pos = e.GetPosition(this);
            var resizeDir = GetResizeDirection(pos);

            if (resizeDir != ResizeDirection.None)
            {
                ResizeWindow(resizeDir, pos);
                e.Handled = true;
            }
            else
            {
                DragMove();
            }
        }

        private enum ResizeDirection
        {
            None, Left, Right, Top, Bottom,
            TopLeft, TopRight, BottomLeft, BottomRight
        }

        private ResizeDirection GetResizeDirection(WpfPoint pos)
        {
            bool nearLeft = pos.X < GRIP_SIZE;
            bool nearRight = pos.X > ActualWidth - GRIP_SIZE;
            bool nearTop = pos.Y < GRIP_SIZE;
            bool nearBottom = pos.Y > ActualHeight - GRIP_SIZE;

            if (nearRight && nearBottom) return ResizeDirection.BottomRight;
            if (nearLeft && nearBottom) return ResizeDirection.BottomLeft;
            if (nearRight && nearTop) return ResizeDirection.TopRight;
            if (nearLeft && nearTop) return ResizeDirection.TopLeft;
            if (nearRight) return ResizeDirection.Right;
            if (nearLeft) return ResizeDirection.Left;
            if (nearBottom) return ResizeDirection.Bottom;
            if (nearTop) return ResizeDirection.Top;
            return ResizeDirection.None;
        }

        private void ResizeWindow(ResizeDirection direction, WpfPoint startPos)
        {
            var screenStart = PointToScreen(startPos);
            double startLeft = Left;
            double startTop = Top;
            double startWidth = ActualWidth;
            double startHeight = ActualHeight;

            Mouse.Capture(this);

            void OnMouseMove(object s, MouseEventArgs args)
            {
                var current = PointToScreen(args.GetPosition(this));
                var delta = current - screenStart;

                switch (direction)
                {
                    case ResizeDirection.Right:
                        Width = Math.Max(40, startWidth + delta.X);
                        break;
                    case ResizeDirection.Bottom:
                        Height = Math.Max(40, startHeight + delta.Y);
                        break;
                    case ResizeDirection.BottomRight:
                        Width = Math.Max(40, startWidth + delta.X);
                        Height = Math.Max(40, startHeight + delta.Y);
                        break;
                    case ResizeDirection.Left:
                    {
                        var newWidth = Math.Max(40, startWidth - delta.X);
                        Left = startLeft + startWidth - newWidth;
                        Width = newWidth;
                        break;
                    }
                    case ResizeDirection.Top:
                    {
                        var newHeight = Math.Max(40, startHeight - delta.Y);
                        Top = startTop + startHeight - newHeight;
                        Height = newHeight;
                        break;
                    }
                    case ResizeDirection.TopLeft:
                    {
                        var newWidth = Math.Max(40, startWidth - delta.X);
                        var newHeight = Math.Max(40, startHeight - delta.Y);
                        Left = startLeft + startWidth - newWidth;
                        Top = startTop + startHeight - newHeight;
                        Width = newWidth;
                        Height = newHeight;
                        break;
                    }
                    case ResizeDirection.TopRight:
                    {
                        var newWidth = Math.Max(40, startWidth + delta.X);
                        var newHeight = Math.Max(40, startHeight - delta.Y);
                        Top = startTop + startHeight - newHeight;
                        Width = newWidth;
                        Height = newHeight;
                        break;
                    }
                    case ResizeDirection.BottomLeft:
                    {
                        var newWidth = Math.Max(40, startWidth - delta.X);
                        var newHeight = Math.Max(40, startHeight + delta.Y);
                        Left = startLeft + startWidth - newWidth;
                        Width = newWidth;
                        Height = newHeight;
                        break;
                    }
                }
            }

            void OnMouseUp(object s, MouseButtonEventArgs args)
            {
                Mouse.Capture(null);
                MouseMove -= OnMouseMove;
                MouseLeftButtonUp -= OnMouseUp;
            }

            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseUp;
        }

        #endregion
    }
}
