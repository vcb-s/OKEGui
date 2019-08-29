using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace OKEGui.Utils
{
    class OKEGuiConfig
    {
        public string vspipePath;
        public string logLevel = "DEBUG";
    }

    static class ConfigManager
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
    }
}
