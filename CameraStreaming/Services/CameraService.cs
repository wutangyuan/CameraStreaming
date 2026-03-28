using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using CameraStreaming.Models;

namespace CameraStreaming.Services
{
    public class CameraService : IDisposable
    {
        private VideoCapture? _capture;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isRunning;
        private bool _mirror = false;

        public event Action<Mat>? FrameCaptured;

        public enum ConnectResult
        {
            Success,
            NoCamera,
            CameraInUse,
            Failed
        }

        public bool IsConnected => _capture != null && _capture.IsOpened();

        public bool Mirror
        {
            get => _mirror;
            set => _mirror = value;
        }

        public async Task<ConnectResult> StartAsync(CameraConfig config)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Stop();

                    _capture = new VideoCapture(config.Index, VideoCaptureAPIs.ANY);
                    
                    if (!_capture.IsOpened())
                    {
                        Debug.WriteLine($"无法打开摄像头: Index={config.Index}");
                        return ConnectResult.NoCamera;
                    }

                    // 尝试读取一帧来检测摄像头是否真正可用（可能被占用）
                    using var testFrame = new Mat();
                    bool canRead = _capture.Read(testFrame);
                    if (!canRead || testFrame.Empty())
                    {
                        Debug.WriteLine($"摄像头被占用或无法读取: Index={config.Index}");
                        _capture.Dispose();
                        _capture = null;
                        return ConnectResult.CameraInUse;
                    }

                    _capture.Set(VideoCaptureProperties.FrameWidth, config.Width);
                    _capture.Set(VideoCaptureProperties.FrameHeight, config.Height);
                    _capture.Set(VideoCaptureProperties.Fps, config.Fps);

                    _isRunning = true;
                    _cancellationTokenSource = new CancellationTokenSource();

                    Task.Run(() => CaptureLoop(_cancellationTokenSource.Token), 
                        _cancellationTokenSource.Token);

                    return ConnectResult.Success;
                }
                catch (OpenCvSharp.OpenCVException ex) when (ex.Status < 0)
                {
                    Debug.WriteLine($"摄像头被占用: {ex.Message}");
                    Stop();
                    return ConnectResult.CameraInUse;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"启动摄像头失败: {ex.Message}");
                    Stop();
                    return ConnectResult.Failed;
                }
            });
        }

        private void CaptureLoop(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_capture == null || !_capture.IsOpened())
                    {
                        break;
                    }

                    using var frame = new Mat();
                    if (_capture.Read(frame) && !frame.Empty())
                    {
                        if (_mirror)
                        {
                            Cv2.Flip(frame, frame, FlipMode.Y);
                        }
                        // Clone so the subscriber can safely use the frame after this block
                        FrameCaptured?.Invoke(frame.Clone());
                    }

                    Thread.Sleep(33); // 约30fps
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"捕获帧失败: {ex.Message}");
                    break;
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _capture?.Dispose();
            _capture = null;
        }

        public static async Task<List<CameraInfo>> GetAvailableCamerasAsync()
        {
            return await Task.Run(() =>
            {
                var cameras = new List<CameraInfo>();
                
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        using var capture = new VideoCapture(i, VideoCaptureAPIs.ANY);
                        if (capture.IsOpened())
                        {
                            int width = (int)capture.Get(VideoCaptureProperties.FrameWidth);
                            int height = (int)capture.Get(VideoCaptureProperties.FrameHeight);
                            double fps = capture.Get(VideoCaptureProperties.Fps);

                            cameras.Add(new CameraInfo
                            {
                                Index = i,
                                Name = $"摄像头 {i}",
                                Width = width > 0 ? width : 640,
                                Height = height > 0 ? height : 480,
                                Fps = fps > 0 ? (int)fps : 30
                            });

                            capture.Release();
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                return cameras;
            });
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public class CameraInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public int Fps { get; set; }
    }
}
