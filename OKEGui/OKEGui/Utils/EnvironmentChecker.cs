using Microsoft.Win32;
using System;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using OKEGui.JobProcessor;

namespace OKEGui.Utils
{
    static class EnvironmentChecker
    {
        static OKEGuiConfig Config;
        public static bool CheckEnviornment()
        {
            if (!CheckRootFolderWriteAccess())
            {
                MessageBox.Show("没有权限控制OKEGui所在的文件夹，请保证当前用户获取了目录权限，或者以管理员模式运行。", "没有权限控制OKEGui所在的文件夹");
                return false;
            }
            Config = Initializer.LoadConfig();

            return CheckVspipe() && CheckQAAC() && CheckFfmpeg() && CheckRPChecker();
        }

        static bool CheckRootFolderWriteAccess()
        {
            FileIOPermission f = new FileIOPermission(FileIOPermissionAccess.AllAccess, AppDomain.CurrentDomain.BaseDirectory);
            try
            {
                f.Demand();
            }
            catch (SecurityException)
            {
                return false;
            }
            return true;
        }

        static bool CheckVspipe()
        {
            string vspipePath = Config.vspipePath;
            FileInfo vspipeInfo;

            if (!string.IsNullOrWhiteSpace(vspipePath))
            {
                vspipeInfo = new FileInfo(vspipePath);
                if (vspipeInfo.Exists)
                {
                    return true;
                }
            }

            RegistryKey key = Registry.LocalMachine.OpenSubKey("software\\vapoursynth");
            if (key == null)
            {
                key = Registry.CurrentUser.OpenSubKey("software\\vapoursynth");
            }
            if (key != null)
            {
                vspipePath = key.GetValue("Path") as string;
            }

            if (!string.IsNullOrWhiteSpace(vspipePath))
            {
                foreach (var subPath in new[] {"core", "core64"})
                {
                    vspipeInfo = new FileInfo(vspipePath + $"\\{subPath}\\vspipe.exe");

                    if (vspipeInfo.Exists)
                    {
                        vspipePath += $"\\{subPath}\\vspipe.exe";
                        Config.vspipePath = vspipePath;
                        return true;
                    }
                }
            }

            MessageBox.Show(
                        "无法找到vspipe.exe。请手动指定其位置，否则程序将退出。",
                        "无法找到vspipe.exe",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

            OpenFileDialog ofd = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "vspipe.exe (vspipe.exe)|vspipe.exe"
            };
            bool result = ofd.ShowDialog().GetValueOrDefault(false);
            if (!result)
            {
                return false;
            }
            vspipePath = ofd.FileName;
            vspipeInfo = new FileInfo(vspipePath);

            if (vspipeInfo.Exists)
            {
                Config.vspipePath = vspipePath;
                return true;
            }
            else
            {
                MessageBox.Show("请尝试重新安装VapourSynth，程序将退出。", "此文件无法读取");
                return false;
            }
        }

        static bool CheckQAAC()
        {
            QAACEncoder e = new QAACEncoder("--check");
            try
            {
                e.start();
            } catch (OKETaskException ex)
            {
                ExceptionMsg msg = ExceptionParser.Parse(ex, null);
                MessageBox.Show(msg.errorMsg, ex.Message);
                return false;
            }
            return true;
        }

        static bool CheckFfmpeg()
        {
            FileInfo ffmpegInfo = new FileInfo(Constants.ffmpegPath);

            if (ffmpegInfo.Exists)
            {
                return true;
            }
            else
            {
                MessageBox.Show("请更新tools工具包。", "无法找到ffmpeg");
                return false;
            }
        }

        static bool CheckRPChecker()
        {
            string rpCheckerPath = Config.rpCheckerPath;
            FileInfo rpCheckerInfo;

            if (!string.IsNullOrWhiteSpace(rpCheckerPath))
            {
                rpCheckerInfo = new FileInfo(rpCheckerPath);
                if (rpCheckerInfo.Exists)
                {
                    return true;
                }
            }

            MessageBox.Show(
                        "无法找到RPChecker.exe。请手动指定其位置，否则程序将退出。",
                        "无法找到RPChecker.exe",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

            OpenFileDialog ofd = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "RPChecker.exe (RPChecker*.exe)|RPChecker*.exe"
            };
            bool result = ofd.ShowDialog().GetValueOrDefault(false);
            if (!result)
            {
                return false;
            }
            rpCheckerPath = ofd.FileName;
            rpCheckerInfo = new FileInfo(rpCheckerPath);

            if (rpCheckerInfo.Exists)
            {
                Config.rpCheckerPath = rpCheckerPath;
                return true;
            }
            else
            {
                MessageBox.Show("请准备RPChecker最新版，程序将退出。", "此文件无法读取");
                return false;
            }
        }
    }
}
