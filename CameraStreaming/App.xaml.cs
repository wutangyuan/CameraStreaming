using System.IO;
using System.Windows;
using log4net;
using log4net.Config;
using log4net.Layout;
using log4net.Appender;

namespace CameraStreaming
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(App));

        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigureLogging();
            base.OnStartup(e);
            Log.Info("应用程序启动");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Info("应用程序退出");
            base.OnExit(e);
        }

        private static void ConfigureLogging()
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CameraStreaming", "logs");

            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var patternLayout = new PatternLayout
            {
                ConversionPattern = "%date [%thread] %-5level %logger - %message%newline%exception"
            };
            patternLayout.ActivateOptions();

            var fileAppender = new RollingFileAppender
            {
                File = Path.Combine(logDir, "CameraStreaming.log"),
                AppendToFile = true,
                RollingStyle = RollingFileAppender.RollingMode.Date,
                DatePattern = "yyyyMMdd",
                MaxSizeRollBackups = 30,
                StaticLogFileName = false,
                Layout = patternLayout
            };
            fileAppender.ActivateOptions();

            BasicConfigurator.Configure(fileAppender);
        }
    }
}
