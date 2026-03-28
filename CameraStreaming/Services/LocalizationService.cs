using System.ComponentModel;
using System.Globalization;

namespace CameraStreaming.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        private string _language = "zh-CN";

        public string Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                    OnPropertyChanged(null);
                }
            }
        }

        public static string DetectSystemLanguage()
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return culture.StartsWith("zh") ? "zh-CN" : "en-US";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string this[string key] => GetString(key);

        private string GetString(string key)
        {
            return _language == "zh-CN" ? ZhCN.GetString(key) : EnUS.GetString(key);
        }

        private static class ZhCN
        {
            public static string GetString(string key) => key switch
            {
                // MainWindow
                "MainWindow_Title" => "摄像头画面显示",
                "Settings" => "⚙ 设置",
                "Refresh" => "↻ 刷新",
                "Mirror" => "↔ 镜像",
                "LiveStream" => "▶ 直播模式",
                "NoSignal" => "无信号",
                "Shortcut_Prefix" => "快捷键: ",
                "Shortcut_Preview" => " 打开直播窗口",
                "Shortcut_Mirror" => " 切换镜像",
                "Connecting" => "正在连接摄像头...",
                "Connected" => "摄像头已连接 ({0}x{1}, {2}fps)",
                "ConnectFailed" => "摄像头连接失败，请检查设置",
                "CameraNotConnected" => "摄像头未连接",
                "NoCameraDetected_Msg" => "未检测到摄像头，请确认摄像头已正确连接。",
                "CameraInUse" => "摄像头被占用",
                "CameraInUse_Msg" => "摄像头可能被其他程序占用，请关闭其他使用摄像头的程序后重试。",

                // SettingsWindow
                "Settings_Title" => "摄像头设置",
                "Settings_Header" => "摄像头配置",
                "CameraLabel" => "摄像头:",
                "ResolutionLabel" => "分辨率:",
                "FpsLabel" => "帧率:",
                "LanguageLabel" => "语言:",
                "DetectingCameras" => "正在检测摄像头...",
                "NoCameraDetected" => "未检测到摄像头",
                "DetectCameraFailed" => "检测摄像头失败",
                "CameraInfo" => "{0} ({1}x{2}, {3}fps)",
                "OK" => "确定",
                "Cancel" => "取消",
                "SaveSettingsFailed" => "保存设置失败: {0}",

                // FramePreviewWindow
                "Preview_Title" => "摄像头实时画面",
                "CopyToClipboard" => "复制到剪贴板 (Ctrl+C)",
                "SaveToFile" => "保存到文件 (Ctrl+S)",
                "Close" => "关闭 (Esc)",
                "NoFrameToCopy" => "当前没有可复制的画面",
                "CopiedToClipboard" => "画面已复制到剪贴板",
                "CopyFailed" => "复制失败: {0}",
                "NoFrameToSave" => "当前没有可保存的画面",
                "SaveDialogFilter" => "PNG 图片|*.png|JPEG 图片|*.jpg|BMP 图片|*.bmp",
                "SaveDialogFileName" => "摄像头截图_{0}.png",
                "SavedTo" => "图片已保存到:\n{0}",
                "SaveFailed" => "保存失败: {0}",

                // Common
                "Tip" => "提示",
                "Error" => "错误",
                "ToggleShape" => "⬜ 方形",
                "CircleMode" => "圆形窗口",
                "SquareMode" => "方形窗口",
                "WindowShapeLabel" => "窗口形状:",

                _ => key
            };
        }

        private static class EnUS
        {
            public static string GetString(string key) => key switch
            {
                // MainWindow
                "MainWindow_Title" => "Camera Streaming",
                "Settings" => "⚙ Settings",
                "Refresh" => "↻ Refresh",
                "Mirror" => "↔ Mirror",
                "LiveStream" => "▶ Live Mode",
                "NoSignal" => "No Signal",
                "Shortcut_Prefix" => "Shortcuts: ",
                "Shortcut_Preview" => " Open live stream",
                "Shortcut_Mirror" => " Toggle mirror",
                "Connecting" => "Connecting camera...",
                "Connected" => "Camera connected ({0}x{1}, {2}fps)",
                "ConnectFailed" => "Camera connection failed, check settings",
                "CameraNotConnected" => "Camera not connected",
                "NoCameraDetected_Msg" => "No camera detected. Please make sure the camera is properly connected.",
                "CameraInUse" => "Camera in use",
                "CameraInUse_Msg" => "The camera may be in use by another application. Please close other programs using the camera and try again.",

                // SettingsWindow
                "Settings_Title" => "Camera Settings",
                "Settings_Header" => "Camera Configuration",
                "CameraLabel" => "Camera:",
                "ResolutionLabel" => "Resolution:",
                "FpsLabel" => "Frame Rate:",
                "LanguageLabel" => "Language:",
                "DetectingCameras" => "Detecting cameras...",
                "NoCameraDetected" => "No camera detected",
                "DetectCameraFailed" => "Camera detection failed",
                "CameraInfo" => "{0} ({1}x{2}, {3}fps)",
                "OK" => "OK",
                "Cancel" => "Cancel",
                "SaveSettingsFailed" => "Failed to save settings: {0}",

                // FramePreviewWindow
                "Preview_Title" => "Camera Live Preview",
                "CopyToClipboard" => "Copy to Clipboard (Ctrl+C)",
                "SaveToFile" => "Save to File (Ctrl+S)",
                "Close" => "Close (Esc)",
                "NoFrameToCopy" => "No frame available to copy",
                "CopiedToClipboard" => "Frame copied to clipboard",
                "CopyFailed" => "Copy failed: {0}",
                "NoFrameToSave" => "No frame available to save",
                "SaveDialogFilter" => "PNG Image|*.png|JPEG Image|*.jpg|BMP Image|*.bmp",
                "SaveDialogFileName" => "camera_capture_{0}.png",
                "SavedTo" => "Image saved to:\n{0}",
                "SaveFailed" => "Save failed: {0}",

                // Common
                "Tip" => "Tip",
                "Error" => "Error",
                "ToggleShape" => "⬜ Square",
                "CircleMode" => "Circle Window",
                "SquareMode" => "Square Window",
                "WindowShapeLabel" => "Window Shape:",

                _ => key
            };
        }
    }
}
