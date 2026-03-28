using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using CameraStreaming.Services;
using WpfWindow = System.Windows.Window;

namespace CameraStreaming.Views
{
    public partial class FramePreviewWindow : WpfWindow
    {
        private readonly CameraService _cameraService;
        private Mat? _latestFrame;
        private readonly object _frameLock = new object();
        private readonly LocalizationService _lang = LocalizationService.Instance;

        public FramePreviewWindow(CameraService cameraService)
        {
            InitializeComponent();
            _cameraService = cameraService;
            _cameraService.FrameCaptured += OnFrameCaptured;
        }

        private void OnFrameCaptured(Mat frame)
        {
            lock (_frameLock)
            {
                _latestFrame?.Dispose();
                _latestFrame = frame.Clone();
            }

            Dispatcher.Invoke(() =>
            {
                try
                {
                    var bitmap = frame.ToWriteableBitmap();
                    previewImage.Source = bitmap;
                }
                catch
                {
                }
            });
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            _cameraService.FrameCaptured -= OnFrameCaptured;
            lock (_frameLock)
            {
                _latestFrame?.Dispose();
                _latestFrame = null;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CopyToClipboard();
                e.Handled = true;
            }
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveToFile();
                e.Handled = true;
            }
            else if (e.Key == Key.M && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ToggleMirror();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void CopyToClipboard()
        {
            try
            {
                Mat? frameToCopy = null;
                lock (_frameLock)
                {
                    if (_latestFrame != null && !_latestFrame.Empty())
                    {
                        frameToCopy = _latestFrame.Clone();
                    }
                }

                if (frameToCopy == null)
                {
                    MessageBox.Show(this, _lang["NoFrameToCopy"], _lang["Tip"], MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (frameToCopy)
                {
                    var bitmap = frameToCopy.ToWriteableBitmap();
                    bitmap.Freeze();
                    Clipboard.SetImage(bitmap);
                    MessageBox.Show(this, _lang["CopiedToClipboard"], _lang["Tip"], MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(_lang["CopyFailed"], ex.Message), _lang["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveToFile()
        {
            try
            {
                Mat? frameToSave = null;
                lock (_frameLock)
                {
                    if (_latestFrame != null && !_latestFrame.Empty())
                    {
                        frameToSave = _latestFrame.Clone();
                    }
                }

                if (frameToSave == null)
                {
                    MessageBox.Show(this, _lang["NoFrameToSave"], _lang["Tip"], MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = _lang["SaveDialogFilter"],
                    DefaultExt = ".png",
                    FileName = string.Format(_lang["SaveDialogFileName"], DateTime.Now.ToString("yyyyMMdd_HHmmss"))
                };

                if (dialog.ShowDialog() == true)
                {
                    using (frameToSave)
                    {
                        var encoder = dialog.FilterIndex switch
                        {
                            1 => (BitmapEncoder)new PngBitmapEncoder(),
                            2 => new JpegBitmapEncoder { QualityLevel = 95 },
                            _ => new BmpBitmapEncoder()
                        };

                        var bitmap = frameToSave.ToWriteableBitmap();
                        bitmap.Freeze();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));

                        using var stream = new FileStream(dialog.FileName, FileMode.Create);
                        encoder.Save(stream);
                    }

                    MessageBox.Show(this, string.Format(_lang["SavedTo"], dialog.FileName), _lang["Tip"], MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    frameToSave.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(_lang["SaveFailed"], ex.Message), _lang["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void btnCopy_Click(object sender, RoutedEventArgs e) => CopyToClipboard();
        private void btnMirror_Click(object sender, RoutedEventArgs e) => ToggleMirror();
        private void btnSave_Click(object sender, RoutedEventArgs e) => SaveToFile();
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
