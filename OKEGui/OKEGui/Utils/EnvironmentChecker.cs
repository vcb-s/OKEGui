﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

            return CheckVspipe() && CheckQAAC() && CheckFfmpeg() && CheckRPChecker() && CheckEac3toWrapper() && CheckEncoders();
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

            vspipeInfo = new FileInfo(Constants.vspipePath);
            if (vspipeInfo.Exists)
            {
                Config.vspipePath = vspipeInfo.FullName;
                return true;
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
            FileInfo qaacInfo = new FileInfo(Constants.QAACPath);
            if (!qaacInfo.Exists)
            {
                MessageBox.Show("请更新tools工具包。", "无法找到qaac");
                return false;
            }
            QAACEncoder e = new QAACEncoder("--check");
            try
            {
                e.start();
            }
            catch (OKETaskException ex)
            {
                ExceptionMsg msg = ExceptionParser.Parse(ex, null);
                MessageBox.Show(msg.errorMsg, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "qaac检查失败");
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

            rpCheckerInfo = new FileInfo(Constants.rpcPath);
            if (rpCheckerInfo.Exists)
            {
                Config.rpCheckerPath = rpCheckerInfo.FullName;
                return true;
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

        static bool CheckEac3toWrapper()
        {
            FileInfo eac3toWrapperInfo = new FileInfo(Constants.eac3toWrapperPath);

            if (eac3toWrapperInfo.Exists)
            {
                return true;
            }
            else
            {
                MessageBox.Show("请更新tools工具包。", "无法找到eac3to-wrapper");
                return false;
            }
        }

        static bool CheckEncoders()
        {
            var encoders = new List<FileInfo> {
                new FileInfo(Constants.x264Path),
                new FileInfo(Constants.x265Path),
            };
            foreach (FileInfo fi in encoders)
            {
                if (!fi.Exists)
                {
                    MessageBox.Show("请更新tools工具包。", "无法找到" + fi.Name);
                    return false;
                }
            }
            return true;
        }
    }
}
