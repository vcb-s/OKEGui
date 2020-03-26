using Newtonsoft.Json;
using OKEGui.Model;
using OKEGui.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace OKEGui
{
    public static class AddTaskService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        // 读入Json文件，并转成TaskProfile对象。
        public static TaskProfile LoadJsonAsProfile(string filePath, DirectoryInfo jsonDir)
        {
            string profileStr = File.ReadAllText(filePath);

            foreach (string option in Constants.deprecatedOptions)
            {
                if (profileStr.IndexOf(option, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    MessageBox.Show(option + "已不再支持", "json文件版本太老了", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }

            TaskProfile json;
            try
            {
                json = JsonConvert.DeserializeObject<TaskProfile>(profileStr);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "json文件写错了诶", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }


            return ProcessJsonProfile(json, jsonDir);
        }

        // 依次检查json里各项输入是否正确
        public static TaskProfile ProcessJsonProfile(TaskProfile json, DirectoryInfo projDir)
        {

            // 检查参数
            if (json.Version != 2)
            {
                MessageBox.Show("你是不是把单个文件追加用的json当成一套任务用的json了？", "版本不对", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            // 编码器设置，目前只允许x264/x265
            json.EncoderType = json.EncoderType.ToLower();
            switch (json.EncoderType)
            {
                case "x264":
                    json.VideoFormat = "AVC";
                    break;
                case "x265":
                    json.VideoFormat = "HEVC";
                    break;
                default:
                    MessageBox.Show("EncoderType请填写x264或者x265", "编码器版本错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
            }

            // 设置封装格式
            json.ContainerFormat = json.ContainerFormat.ToUpper();
            if (json.ContainerFormat != "MKV" && json.ContainerFormat != "MP4")
            {
                MessageBox.Show("MKV/MP4，只能这两种", "封装格式指定的有问题", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            // 如果是MP4则暂不支持指定时间码
            if (json.ContainerFormat == "MP4" && json.TimeCode)
            {
                MessageBox.Show("MP4暂不支持VFR封装，请联系技术总监。", "MP4暂不支持VFR封装", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            if (json.Fps <= 0 && (json.FpsNum <= 0 || json.FpsDen <= 0))
            {
                if (json.TimeCode)
                {
                    json.Fps = 1;
                }
                else
                {
                    MessageBox.Show("现在json文件中需要指定帧率，哪怕 Fps : 23.976", "帧率没有指定诶", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }

            if (json.FpsNum > 0 && json.FpsDen > 0)
            {
                json.Fps = ((double)json.FpsNum) / json.FpsDen;
            }
            else
            {
                switch (json.Fps)
                {
                    case 1.0:
                        json.FpsNum = 1;
                        json.FpsDen = 1;
                        break;
                    case 23.976:
                        json.FpsNum = 24000;
                        json.FpsDen = 1001;
                        break;
                    case 24.000:
                        json.FpsNum = 24;
                        json.FpsDen = 1;
                        break;
                    case 25.000:
                        json.FpsNum = 25;
                        json.FpsDen = 1;
                        break;
                    case 29.970:
                        json.FpsNum = 30000;
                        json.FpsDen = 1001;
                        break;
                    case 50.000:
                        json.FpsNum = 50;
                        json.FpsDen = 1;
                        break;
                    case 59.940:
                        json.FpsNum = 60000;
                        json.FpsDen = 1001;
                        break;
                    default:
                        MessageBox.Show("请通过FpsNum和FpsDen来指定", "不知道的帧率诶", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                }
            }

            if (json.AudioTracks != null && json.AudioTracks.Count > 0)
            {
                // 主音轨
                json.AudioFormat = json.AudioTracks[0].OutputCodec;
                if (string.IsNullOrEmpty(json.AudioFormat))
                {
                    json.AudioFormat = "AAC";
                }
                else
                {
                    json.AudioFormat = json.AudioFormat.ToUpper();
                }
            }
            else
            {
                json.AudioFormat = "AAC";
            }

            if (json.AudioFormat != "FLAC" && json.AudioFormat != "AAC" &&
                json.AudioFormat != "AC3" && json.AudioFormat != "DTS")
            {
                MessageBox.Show("音轨只能是FLAC/AAC/AC3/DTS", "音轨格式不支持", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            if (json.AudioFormat == "FLAC" && json.ContainerFormat == "MP4")
            {
                MessageBox.Show("MP4格式没法封FLAC", "音轨格式不支持", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            // 获取编码器全路径
            FileInfo encoder = new FileInfo(projDir.FullName + "\\" + json.Encoder);
            if (encoder.Exists)
            {
                json.Encoder = encoder.FullName;
            }
            else
            {
                MessageBox.Show("编码器好像不在json指定的地方（文件名错误？还有记得放在json文件同目录下）", "找不到编码器啊", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            return json;
        }

        // 读入vs脚本，并检查OKE:INPUTFILE标签
        public static string LoadVsScript(TaskProfile json, DirectoryInfo jsonDir)
        {
            FileInfo scriptFile = new FileInfo(jsonDir.FullName + "\\" + json.InputScript);

            if (scriptFile.Exists)
            {
                json.InputScript = scriptFile.FullName;
                string vsScript = File.ReadAllText(json.InputScript);
                if (json.Rpc && !vsScript.Contains(".set_output(1)"))
                {
                    MessageBox.Show("请告诉技术总监给vpy里加上rpc的输出。", "vpy里没有准备rpc的输出", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                if (Constants.inputRegex.IsMatch(vsScript))
                {
                    return vsScript;
                }
                else
                {
                    MessageBox.Show("vpy里没有#OKE:INPUTFILE的标签。", "vpy没有为OKEGui设计", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            else
            {
                MessageBox.Show("指定的vpy文件没有找到，检查下json文件和vpy文件是不是放一起了？", "vpy文件找不到", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // 读入json里指定的输入文件，加入到inputFile里
        public static int LoadInputFiles(TaskProfile json, DirectoryInfo jsonDir, ObservableCollection<string> inputFile)
        {
            int fileCount = 0;
            if (json.InputFiles != null)
            {
                inputFile.Clear();
                foreach (string file in json.InputFiles)
                {
                    FileInfo input = new FileInfo(jsonDir.FullName + "\\" + file);
                    if (inputFile.Contains(input.FullName))
                    {
                        MessageBox.Show("指定的文件(" + input.FullName + ")重复了，请总监复查下输入文件列表？", "输入文件有重复", MessageBoxButton.OK, MessageBoxImage.Error);
                        return 0;
                    }
                    if (input.Exists)
                    {
                        inputFile.Add(input.FullName);
                    }
                    else
                    {
                        MessageBox.Show("指定的文件(" + input.FullName + ")不存在啊，跟总监确认下json应该放哪？", "找不到输入文件啊", MessageBoxButton.OK, MessageBoxImage.Error);
                        return 0;
                    }
                }
                fileCount = json.InputFiles.Count;
            }
            return fileCount;
        }
    }
}
