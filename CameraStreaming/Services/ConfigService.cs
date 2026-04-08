using System.IO;
using System.Text.Json;
using CameraStreaming.Models;
using log4net;

namespace CameraStreaming.Services
{
    public class ConfigService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ConfigService));

        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CameraStreaming",
            "config.json");

        public static CameraConfig LoadConfig()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<CameraConfig>(json) ?? new CameraConfig();
                    Log.Info($"配置加载成功: {ConfigFilePath}");
                    return config;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"加载配置失败: {ex.Message}", ex);
            }

            Log.Info("使用默认配置");
            return new CameraConfig();
        }

        public static void SaveConfig(CameraConfig config)
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(ConfigFilePath, json);
                Log.Info($"配置保存成功: 摄像头={config.Index}, 分辨率={config.Width}x{config.Height}, 帧率={config.Fps}");
            }
            catch (Exception ex)
            {
                Log.Error($"保存配置失败: {ex.Message}", ex);
            }
        }
    }
}
