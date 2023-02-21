using OKEGui.Utils;
using System;
using System.Reflection;
using System.Windows;

namespace OKEGui
{
    /// <summary>
    /// 程序接入点。使用静态构造器来执行程序开始前的检查和其他任务。
    /// 主界面的设计和逻辑请见 Gui/MainWindow
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("App");

        private void AppStartup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender_, args) =>
            {
                AssemblyName assemblyName = new AssemblyName(args.Name);
                var path = assemblyName.Name + ".dll";

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
                {
                    if (stream == null) return null;

                    var assemblyRawBytes = new byte[stream.Length];
                    stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                    return Assembly.Load(assemblyRawBytes);
                }
            };
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
