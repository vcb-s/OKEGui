using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace OKEGui.Utils
{
    public class OKEGuiConfig : INotifyPropertyChanged, ICloneable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        private string _vspipePath;
        public string vspipePath
        {
            get => _vspipePath;
            set
            {
                _vspipePath = value;
                NotifyPropertyChanged();
            }
        }

        private string _logLevel = "DEBUG";
        public string logLevel
        {
            get => _logLevel;
            set
            {
                _logLevel = value;
                NotifyPropertyChanged();
            }
        }

        private bool _noNuma = false;
        public bool noNuma
        {
            get => _noNuma;
            set
            {
                _noNuma = value;
                NotifyPropertyChanged();
            }
        }

        private string _rpCheckerPath;
        public string rpCheckerPath
        {
            get => _rpCheckerPath;
            set
            {
                _rpCheckerPath = value;
                NotifyPropertyChanged();
            }
        }

        private bool _avx512;

        public bool avx512
        {
            get => _avx512;
            set
            {
                _avx512 = value;
                NotifyPropertyChanged();
            }
        }
    }

    static class Initializer
    {
        public static OKEGuiConfig Config = new OKEGuiConfig();

        public static OKEGuiConfig LoadConfig()
        {
            FileInfo configInfo = new FileInfo(Constants.configFile);
            if (configInfo.Exists)
            {
                string strConfig = File.ReadAllText(Constants.configFile);
                try
                {
                    Config = JsonConvert.DeserializeObject<OKEGuiConfig>(strConfig);
                }
                catch (Exception)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "程序目录下的OKEGuiConfig.json已损坏。点击Yes重置所有之前设置，点击No退出程序以人工修复。",
                        "无法读取配置文件",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error);

                    if (result == MessageBoxResult.No)
                    {
                        Environment.Exit(0);
                    }
                }
            }
            return Config;
        }

        public static bool WriteConfig()
        {
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented
            };
            using (StreamWriter fileWriter = new StreamWriter(Constants.configFile))
            using (JsonTextWriter writer = new JsonTextWriter(fileWriter))
            {
                writer.Indentation = 4;
                serializer.Serialize(writer, Config);
            }
            return true;
        }

        public static bool ConfigLogger()
        {
            string time = DateTime.Now.ToString("yyyyMMdd-HHmm");
            string pid = Process.GetCurrentProcess().Id.ToString("X");
            LogLevel level = LogLevel.FromString(Config.logLevel); // default is DEBUG

            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget logfile = new FileTarget("logfile") { FileName = $"log\\{time}_{pid}.log" };
            DebuggerTarget logconsole = new DebuggerTarget("logconsole");

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logconsole);
            config.AddRule(level, LogLevel.Fatal, logfile);

            // Apply config           
            LogManager.Configuration = config;
            return true;
        }

        public static void ClearOldLogs()
        {
            if (Directory.Exists("log"))
            {
                Directory.GetFiles("log")
                    .Select(f => new FileInfo(f))
                    .Where(f => f.LastWriteTime < DateTime.Now.AddMonths(-3) || (f.LastWriteTime < DateTime.Now.AddDays(-7) && f.Length < 1024))
                    .ToList()
                    .ForEach(f => f.Delete());
            }
        }
    }
}
