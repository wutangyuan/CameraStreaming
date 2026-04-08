using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using CameraStreaming.Models;
using log4net;

namespace CameraStreaming.Services
{
    public class CameraService : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CameraService));

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
            Log.Info($"尝试连接摄像头: Index={config.Index}, 分辨率={config.Width}x{config.Height}, 帧率={config.Fps}");

            return await Task.Run(() =>
            {
                try
                {
                    Stop();

                    _capture = new VideoCapture(config.Index, VideoCaptureAPIs.ANY);
                    
                    if (!_capture.IsOpened())
                    {
                        Log.Warn($"无法打开摄像头: Index={config.Index}");
                        return ConnectResult.NoCamera;
                    }

                    // 尝试读取一帧来检测摄像头是否真正可用（可能被占用）
                    using var testFrame = new Mat();
                    bool canRead = _capture.Read(testFrame);
                    if (!canRead || testFrame.Empty())
                    {
                        Log.Warn($"摄像头被占用或无法读取: Index={config.Index}");
                        _capture.Dispose();
                        _capture = null;
                        return ConnectResult.CameraInUse;
                    }

                    _capture.Set(VideoCaptureProperties.FrameWidth, config.Width);
                    _capture.Set(VideoCaptureProperties.FrameHeight, config.Height);
                    _capture.Set(VideoCaptureProperties.Fps, config.Fps);

                    Log.Info($"摄像头设置完成: {config.Width}x{config.Height}@{config.Fps}fps");

                    _isRunning = true;
                    _cancellationTokenSource = new CancellationTokenSource();

                    Task.Run(() => CaptureLoop(_cancellationTokenSource.Token), 
                        _cancellationTokenSource.Token);

                    Log.Info("摄像头连接成功，开始采集");
                    return ConnectResult.Success;
                }
                catch (OpenCvSharp.OpenCVException ex) when (ex.Status < 0)
                {
                    Log.Error($"摄像头被占用: {ex.Message}", ex);
                    Stop();
                    return ConnectResult.CameraInUse;
                }
                catch (Exception ex)
                {
                    Log.Error($"启动摄像头失败: {ex.Message}", ex);
                    Stop();
                    return ConnectResult.Failed;
                }
            });
        }

        private void CaptureLoop(CancellationToken cancellationToken)
        {
            Log.Info("帧采集循环启动");
            int frameCount = 0;
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_capture == null || !_capture.IsOpened())
                    {
                        Log.Warn("摄像头已断开，退出采集循环");
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
                        frameCount++;
                    }

                    Thread.Sleep(33); // 约30fps
                }
                catch (Exception ex)
                {
                    Log.Error($"捕获帧失败(已采集{frameCount}帧): {ex.Message}", ex);
                    break;
                }
            }
            Log.Info($"帧采集循环结束，共采集{frameCount}帧");
        }

        public void Stop()
        {
            if (_isRunning)
            {
                Log.Info("停止摄像头采集");
            }
            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _capture?.Dispose();
            _capture = null;
        }

        public static async Task<List<CameraInfo>> GetAvailableCamerasAsync()
        {
            Log.Info("扫描可用摄像头...");
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

                            Log.Info($"发现摄像头 {i}: {width}x{height}@{fps}fps");
                            capture.Release();
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                Log.Info($"摄像头扫描完成，共发现 {cameras.Count} 个摄像头");
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
