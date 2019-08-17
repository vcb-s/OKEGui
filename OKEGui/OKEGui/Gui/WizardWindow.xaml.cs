using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Newtonsoft.Json;
using OKEGui.Utils;
using OKEGui.Model;

namespace OKEGui
{
    /// <summary>
    /// WizardWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WizardWindow : Window
    {
        private class NewTask : INotifyPropertyChanged
        {
            private string projectFile;
            public string ProjectFile
            {
                get { return projectFile; }

                set
                {
                    projectFile = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("ProjectFile"));
                }
            }

            private string projectPreview;
            public string ProjectPreview
            {
                get { return projectPreview; }

                set
                {
                    projectPreview = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("ProjectPreview"));
                }
            }

            private ObservableCollection<string> inputFile;
            public ObservableCollection<string> InputFile
            {
                get
                {
                    if (inputFile == null)
                    {
                        inputFile = new ObservableCollection<string>();
                    }

                    return inputFile;
                }
            }

            public string VSScript;

            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, e);
            }
        }

        private NewTask wizardInfo = new NewTask();
        private TaskManager tm;
        private JobProfile json;

        public WizardWindow(ref TaskManager t)
        {
            InitializeComponent();
            taskWizard.BackButtonContent = "上一步";
            taskWizard.CancelButtonContent = "取消";
            taskWizard.FinishButtonContent = "完成";
            taskWizard.HelpButtonContent = "帮助";
            taskWizard.HelpButtonVisibility = Visibility.Hidden;
            taskWizard.NextButtonContent = "下一步";
            this.DataContext = wizardInfo;

            tm = t;
        }

        private bool LoadJsonProfile(string profile)
        {
            string profileStr = File.ReadAllText(profile);
            try
            {
                json = JsonConvert.DeserializeObject<JobProfile>(profileStr);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString(), "json文件写错了诶", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            DirectoryInfo projDir = new DirectoryInfo(wizardInfo.ProjectFile).Parent;

            // 检查参数
            if (json.Version != 2)
            {
                System.Windows.MessageBox.Show("这配置文件版本号不匹配当前的OKE", "版本不对", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

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
                    System.Windows.MessageBox.Show("EncoderType请填写x264或者x265", "编码器版本错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
            }

            // 获取编码器全路径
            FileInfo encoder = new FileInfo(projDir.FullName + "\\" + json.Encoder);
            if (encoder.Exists)
            {
                json.Encoder = encoder.FullName;
            }
            else
            {
                System.Windows.MessageBox.Show("编码器好像不在json指定的地方（文件名错误？还有记得放在json文件同目录下）", "找不到编码器啊", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // 设置封装格式
            json.ContainerFormat = json.ContainerFormat.ToUpper();
            if (json.ContainerFormat != "MKV" && json.ContainerFormat != "MP4")
            {
                System.Windows.MessageBox.Show("MKV/MP4，只能这两种", "封装格式指定的有问题", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // 设置视频编码

            // 设置视频帧率
            if (json.Fps <= 0 && (json.FpsNum <= 0 || json.FpsDen <= 0))
            {
                System.Windows.MessageBox.Show("现在json文件中需要指定帧率，哪怕 Fps : 23.976", "帧率没有指定诶", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (json.FpsNum > 0 && json.FpsDen > 0)
            {
                json.Fps = ((double)json.FpsNum) / json.FpsDen;
            }
            else
            {
                switch (json.Fps)
                {
                    case 23.976:
                        json.FpsNum = 24000;
                        json.FpsDen = 1001;
                        break;
                    case 24.000:
                        json.FpsNum = 24;
                        json.FpsDen = 1;
                        break;
                    case 29.97:
                        json.FpsNum = 30000;
                        json.FpsDen = 1001;
                        break;
                    case 59.94:
                        json.FpsNum = 60000;
                        json.FpsDen = 1001;
                        break;
                    default:
                        System.Windows.MessageBox.Show("请通过FpsNum和FpsDen来指定", "不知道的帧率诶", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                }
            }

            json.SubtitleLanguage = string.IsNullOrEmpty(json.SubtitleLanguage) ? Constants.language : json.SubtitleLanguage;

            if (json.AudioTracks.Count > 0)
            {
                // 主音轨
                json.AudioFormat = json.AudioTracks[0].OutputCodec.ToUpper();

                // 添加音频参数到任务里面
                foreach (var track in json.AudioTracks)
                {
                    if (track.Bitrate == 0)
                    {
                        track.Bitrate = Constants.QAACBitrate;
                    }
                    if (string.IsNullOrEmpty(track.Language))
                    {
                        track.Language = Constants.language;
                    }
                    if (track.MuxOption == MuxOption.Default && track.SkipMuxing)
                    {
                        System.Windows.MessageBox.Show("现在用MuxOption指定封装法则，指定了SkipMuxing = true的音轨将不被抽取。", "SkipMuxing不再使用", MessageBoxButton.OK, MessageBoxImage.Warning);
                        track.MuxOption = MuxOption.Skip;
                    }
                }
            }

            if (json.AudioFormat != "FLAC" && json.AudioFormat != "AAC" &&
                json.AudioFormat != "AC3" && json.AudioFormat != "DTS")
            {
                System.Windows.MessageBox.Show("音轨只能是FLAC/AAC/AC3/DTS", "音轨格式不支持", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (json.AudioFormat == "FLAC" && json.ContainerFormat == "MP4")
            {
                System.Windows.MessageBox.Show("MP4格式没法封FLAC", "音轨格式不支持", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var scriptFile = new FileInfo(projDir.FullName + "\\" + json.InputScript);

            if (scriptFile.Exists)
            {
                json.InputScript = scriptFile.FullName;
                wizardInfo.VSScript = File.ReadAllText(json.InputScript);
                if (!Constants.inputRegex.IsMatch(wizardInfo.VSScript))
                {
                    System.Windows.MessageBox.Show("vpy里没有#OKE:INPUTFILE的标签。", "vpy没有为OKEGui设计", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            else
            {
                System.Windows.MessageBox.Show("指定的vpy文件没有找到，检查下json文件和vpy文件是不是放一起了？", "vpy文件找不到", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // 预览
            wizardInfo.ProjectPreview = "项目名字: " + json.ProjectName;
            wizardInfo.ProjectPreview += "\n\n编码器类型: " + json.EncoderType;
            wizardInfo.ProjectPreview += "\n编码器路径: " + json.Encoder;
            wizardInfo.ProjectPreview += "\n编码参数: " + json.EncoderParam.Substring(0, Math.Min(30, json.EncoderParam.Length - 1)) + "......";
            wizardInfo.ProjectPreview += "\n\n封装格式: " + json.ContainerFormat;
            wizardInfo.ProjectPreview += "\n视频编码: " + json.VideoFormat;
            wizardInfo.ProjectPreview += "\n视频帧率: " + String.Format("{0:0.000} fps", json.Fps);
            wizardInfo.ProjectPreview += "\n音频编码(主音轨): " + json.AudioFormat;

            return true;
        }

        private void OpenProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "OKEGui 项目文件 (*.json)|*.json";
            var result = ofd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            wizardInfo.ProjectFile = ofd.FileName;
            if (LoadJsonProfile(wizardInfo.ProjectFile))
            {
                SelectProjectFile.CanSelectNextPage = true;
            }
            else
            {
                // 配置文件无效
                SelectProjectFile.CanSelectNextPage = false;
            }
        }

        private void WalkDirectoryTree(System.IO.DirectoryInfo root, Action<FileInfo> func, bool IsSearchSubDir = true)
        {
            FileInfo[] files = null;
            DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try
            {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException)
            {
                // log.Add(e.Message);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                // Console.WriteLine(e.Message);
            }

            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    func(fi);
                }

                if (IsSearchSubDir)
                {
                    // 搜索子目录
                    subDirs = root.GetDirectories();
                    foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                    {
                        WalkDirectoryTree(dirInfo, func, IsSearchSubDir);
                    }
                }
            }
        }

        private void OpenInputFile_Click(object sender, RoutedEventArgs e)
        {
            using (var ofd = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "视频文件 (*.m2ts, *.mkv, *.mp4)|*.m2ts;*.mkv;*.mp4"
            })
            {
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                {
                    return;
                }

                foreach (var filename in ofd.FileNames)
                {
                    if (!wizardInfo.InputFile.Contains(filename))
                    {
                        wizardInfo.InputFile.Add(filename);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(filename + "被重复选择，已取消添加。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                SelectInputFile.CanFinish = wizardInfo.InputFile.Count != 0;
            }
        }

        private void OpenInputFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "请选择视频文件夹";
            // fbd.SelectedPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
            fbd.SelectedPath = "C:\\";
            DialogResult result = fbd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            string dir = fbd.SelectedPath.Trim();

            // 历遍改目录，添加全部支持的文件类型
            // 默认为m2ts, mp4, mkv
            MessageBoxResult isSearchSub = System.Windows.MessageBox.Show("是否搜索子目录？", "新建任务向导", MessageBoxButton.YesNo);
            WalkDirectoryTree(new DirectoryInfo(dir), (FileInfo fi) =>
            {
                if (fi.Extension.ToUpper() == ".M2TS" || fi.Extension.ToUpper() == ".MP4" ||
                    fi.Extension.ToUpper() == ".MKV")
                {
                    // TODO: 重复文件处理
                    if (!wizardInfo.InputFile.Contains(fi.FullName))
                    {
                        wizardInfo.InputFile.Add(fi.FullName);
                    }
                }
            }, isSearchSub == MessageBoxResult.Yes);

            SelectInputFile.CanFinish = wizardInfo.InputFile.Count != 0;
        }

        private void WizardFinish(object sender, RoutedEventArgs e)
        {
            // 使用正则解析模板, 多行忽略大小写
            string[] inputTemplate = Constants.inputRegex.Split(wizardInfo.VSScript);
            if (inputTemplate.Length < 4 && wizardInfo.InputFile.Count() > 1)
            {
                System.Windows.MessageBox.Show("任务创建失败！添加多个输入文件请确保VapourSynth脚本使用OKE提供的模板。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 处理DEBUG标签
            // TODO: 是否进行调试输出
            if (Constants.debugRegex.IsMatch(wizardInfo.VSScript))
            {
                string[] debugTag = Constants.debugRegex.Split(inputTemplate[3]);
                if (debugTag.Length < 4)
                {
                    // error
                    System.Windows.MessageBox.Show("Debug标签语法错误！", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                inputTemplate[3] = debugTag[0] + debugTag[1] + "None" + debugTag[3];
            }

            // 新建任务
            // 1、清理残留文件
            // 2、新建脚本文件
            // 3、新建任务参数
            Cleaner cleaner = new Cleaner();
            foreach (var inputFile in wizardInfo.InputFile)
            {
                // 清理文件
                cleaner.Clean(inputFile, new List<string> { json.InputScript });

                // 新建文件（inputname.m2ts-mm-dd-HH-MM.vpy）
                string vpy = inputTemplate[0] + inputTemplate[1] + "r'" +
                    inputFile + "'" + inputTemplate[3];

                DateTime time = DateTime.Now;

                string fileName = inputFile + "-" + time.ToString("MMddHHmm") + ".vpy";
                File.WriteAllText(fileName, vpy);

                var finfo = new FileInfo(inputFile);
                TaskDetail td = new TaskDetail
                {
                    TaskName = finfo.Name
                };
                if (!string.IsNullOrEmpty(json.ProjectName))
                {
                    td.TaskName = json.ProjectName + "-" + td.TaskName;
                }

                td.Profile = json.Clone() as JobProfile;
                td.InputFile = inputFile;
                td.Profile.InputScript = fileName;

                // 更新输出文件拓展名
                if (!td.UpdateOutputFileName())
                {
                    System.Windows.MessageBox.Show("格式错误！", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                tm.AddTask(td);
            }
        }

        private void InputFile_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            object o = InputList.SelectedItem;
            if (o == null)
            {
                return;
            }

            String input = o as string;
            int id = wizardInfo.InputFile.IndexOf(input);
            if (id == -1)
            {
                // 没有找到
                return;
            }

            wizardInfo.InputFile.RemoveAt(id);
        }

        private void DeleteInput_Click(object sender, RoutedEventArgs e)
        {
            var list = InputList.SelectedItems;

            if (list.Count == 0)
            {
                return;
            }

            if (list.Count > 1)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("是否删除" + (list.Count.ToString()) + "个文件？", "新建任务向导", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            List<object> selectList = new List<object>();
            foreach (object item in list)
            {
                selectList.Add(item);
            }

            for (int i = 0; i < selectList.Count; i++)
            {
                foreach (object item in selectList)
                {
                    String selected = item as string;
                    int index = wizardInfo.InputFile.IndexOf(selected);
                    if (index != -1)
                    {
                        wizardInfo.InputFile.RemoveAt(index);
                    }
                }
            }

            SelectInputFile.CanFinish = wizardInfo.InputFile.Count != 0;
        }
    }
}
