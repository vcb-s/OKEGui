using Newtonsoft.Json;
using OKEGui.Model;
using OKEGui.Utils;
using OKEGui.Task;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using YamlDotNet.Serialization;

namespace OKEGui
{
    public static class AddEpProfileService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("AddEpProfileService");

        // 读入每个源独立的小Json文件，并转成EpisodeConfig对象。
        public static EpisodeConfig LoadJsonAsProfile(string filePath)
        {
            string profileStr = File.ReadAllText(filePath);

            EpisodeConfig json;
            try
            {
                var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                json = deserializer.Deserialize<EpisodeConfig>(profileStr);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), filePath + "json文件写错了诶", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            return ProcessJsonProfile(json, filePath);
        }

        // 依次检查json里各项输入是否正确
        public static EpisodeConfig ProcessJsonProfile(EpisodeConfig json, string filePath)
        {

            // 检查参数
            if (json.EnableReEncode == true)
            {
                // 旧版压制成品
                if (json.ReEncodeOldFile == null)
                {
                    MessageBox.Show("ReEncode项目必须指定旧版压制成品", "未指定旧版压制成品", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                FileInfo oldFile = new FileInfo(PathUtils.GetFullPath(json.ReEncodeOldFile, Path.GetDirectoryName(filePath)));
                if (!oldFile.Exists)
                {
                    MessageBox.Show("指定的旧版压制成品不存在，是不是路径写错了？", "旧版压制成品文件不存在", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                json.ReEncodeOldFile = oldFile.FullName;

                // 检查切片序列
                if (json.ReEncodeSliceArray == null)
                {
                    MessageBox.Show("ReEncode项目必须指定需要重压的切片序列", "未指定切片序列", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                foreach (var s in json.ReEncodeSliceArray)
                {
                    if (s.CheckIllegal())
                    {
                        MessageBox.Show($"切片[{s.begin}, {s.end}]不合法", "切片不合法", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }
                }
                Logger.Debug("Raw ReEncodeSliceArray: " + json.ReEncodeSliceArray.ToString());
                json.ReEncodeSliceArray.Sorted();
                json.ReEncodeSliceArray = json.ReEncodeSliceArray.CheckAndMerge();
                if (json.ReEncodeSliceArray == null)
                {
                    MessageBox.Show($"切片区间存在覆盖", "切片区间不能有覆盖", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }

            return json;
        }
    }
}
