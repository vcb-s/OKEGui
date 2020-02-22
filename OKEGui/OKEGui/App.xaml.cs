using OKEGui.Utils;
using System;
using System.Windows;

namespace OKEGui
{
    /// <summary>
    /// 程序接入点。使用静态构造器来执行程序开始前的检查和其他任务。
    /// 主界面的设计和逻辑请见 Gui/MainWindow
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private void AppStartup(object sender, StartupEventArgs e)
        {
            if (EnvironmentChecker.CheckEnviornment())
            {
                Initializer.ConfigLogger();
                Initializer.WriteConfig();
                Initializer.ClearOldLogs();
                Logger.Info("程序正常启动");
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
