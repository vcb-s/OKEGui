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

            public string TaskNamePrefix;
            public string InputScript;
            public string VSScript;

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

            // mp4, mkv, null
            public string ContainerFormat;
            // HEVC, AVC
            public string VideoFormat;
            public uint FpsNum;
            public uint FpsDen;
            // 23.976, 29.970,...
            public double Fps;

            // FLAC, AAC(m4a), "AC3", "ALAC"
            public string AudioFormat;
            // 音频码率
            public ObservableCollection<AudioInfo> AudioTracks = new ObservableCollection<AudioInfo>();
            public string EncoderPath;
            public string EncoderParam;
            public string EncoderType;
            public string EncoderInfo;
            public bool IncludeSub;
            public string SubtitleLanguage;
            public int AudioBitrate;

            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, e);
            }
        }

        private NewTask wizardInfo = new NewTask();
        private TaskManager tm;

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

        public class JsonProfile
        {
            public int Version;
            public string ProjectName;
            public string EncoderType;
            public string Encoder;
            public string EncoderParam;
            public string ContainerFormat;
            public string VideoFormat;
            public double Fps;
            public uint FpsNum;
            public uint FpsDen;
            public List<AudioInfo> AudioTracks;
            public string InputScript;
            public bool IncludeSub;
            public string SubtitleLanguage;
        }

        private bool LoadJsonProfile(string profile)
        {
            // TODO: 测试
            // TODO: FLAC -> lossless(auto)
            string profileStr = File.ReadAllText(profile);
            JsonProfile okeProj;
            try
            {
                okeProj = JsonConvert.DeserializeObject<JsonProfile>(profileStr);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString(), "json文件写错了诶", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            DirectoryInfo projDir = new DirectoryInfo(wizardInfo.ProjectFile).Parent;

            // 检查参数
            if (okeProj.Version != 2)
            {
                System.Windows.MessageBox.Show("这配置文件版本号不匹配当前的OKE", "版本不对", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            wizardInfo.TaskNamePrefix = okeProj.ProjectName;

            if (okeProj.EncoderType.ToLower() != "x265")
            {
                System.Windows.MessageBox.Show("啊，目前只能支持x265编码", "版本不对", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            wizardInfo.EncoderType = okeProj.EncoderType.ToLower();

            // 获取编码器全路径
            FileInfo encoder = new FileInfo(projDir.FullName + "\\" + okeProj.Encoder);
            if (encoder.Exists)
            {
                wizardInfo.EncoderPath = encoder.FullName;
                wizardInfo.EncoderInfo = this.GetEncoderInfo(wizardInfo.EncoderPath);
            }
            else
            {
                System.Windows.MessageBox.Show("编码器好像不在json指定的地方（文件名错误？还有记得放在json文件同目录下）", "找不到编码器啊", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            wizardInfo.EncoderParam = okeProj.EncoderParam;
            wizardInfo.IncludeSub = okeProj.IncludeSub;
            wizardInfo.SubtitleLanguage = okeProj.SubtitleLanguage;

            // 设置封装格式
            wizardInfo.ContainerFormat = okeProj.ContainerFormat.ToUpper();
            if (wizardInfo.ContainerFormat != "MKV" && wizardInfo.ContainerFormat != "MP4" &&
                wizardInfo.ContainerFormat != "NULL" && wizardInfo.ContainerFormat != "RAW")
            {
                System.Windows.MessageBox.Show("MKV/MP4，只能这两种", "封装格式指定的有问题", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // 设置视频编码
            if (okeProj.VideoFormat.ToUpper() != "HEVC")
            {
                System.Windows.MessageBox.Show("现在只能支持HEVC编码", "编码格式不对", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            wizardInfo.VideoFormat = okeProj.VideoFormat.ToUpper();

            // 设置视频帧率
            if (okeProj.Fps <= 0 && (okeProj.FpsNum <= 0 || okeProj.FpsDen <= 0))
            {
                System.Windows.MessageBox.Show("现在json文件中需要指定帧率，哪怕 Fps : 23.976", "帧率没有指定诶", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (okeProj.FpsNum > 0 && okeProj.FpsDen > 0)
            {
                okeProj.Fps = ((double)okeProj.FpsNum) / okeProj.FpsDen;
            }
            else
            {
                switch (okeProj.Fps)
                {
                    case 23.976:
                        okeProj.FpsNum = 24000;
                        okeProj.FpsDen = 1001;
                        break;
                    case 29.97:
                        okeProj.FpsNum = 30000;
                        okeProj.FpsDen = 1001;
                        break;
                    case 59.94:
                        okeProj.FpsNum = 60000;
                        okeProj.FpsDen = 1001;
                        break;
                    default:
                        System.Windows.MessageBox.Show("请通过FpsNum和FpsDen来指定", "不知道的帧率诶", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                }
            }

            wizardInfo.Fps = okeProj.Fps;
            wizardInfo.FpsNum = okeProj.FpsNum;
            wizardInfo.FpsDen = okeProj.FpsDen;
            wizardInfo.SubtitleLanguage = string.IsNullOrEmpty(okeProj.SubtitleLanguage) ? Constants.language : okeProj.SubtitleLanguage;

            if (okeProj.AudioTracks.Count > 0)
            {
                // 主音轨
                wizardInfo.AudioFormat = okeProj.AudioTracks[0].OutputCodec.ToUpper();
                wizardInfo.AudioBitrate = okeProj.AudioTracks[0].Bitrate;

                // 添加音频参数到任务里面
                foreach (var track in okeProj.AudioTracks)
                {
                    if (track.Bitrate == 0)
                    {
                        track.Bitrate = Constants.QAACBitrate;
                    }
                    if (string.IsNullOrEmpty(track.Language))
                    {
                        track.Language = Constants.language;
                    }
                    wizardInfo.AudioTracks.Add(track);
                }
            }

            if (wizardInfo.AudioFormat != "FLAC" && wizardInfo.AudioFormat != "AAC" &&
                wizardInfo.AudioFormat != "AC3" && wizardInfo.AudioFormat != "DTS")
            {
                System.Windows.MessageBox.Show("音轨只能是FLAC/AAC/AC3/DTS", "音轨格式不支持", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (wizardInfo.AudioFormat == "FLAC" && wizardInfo.ContainerFormat == "MP4")
            {
                System.Windows.MessageBox.Show("MP4格式没法封FLAC", "音轨格式不支持", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var scriptFile = new FileInfo(projDir.FullName + "\\" + okeProj.InputScript);

            if (scriptFile.Exists)
            {
                wizardInfo.InputScript = scriptFile.FullName;
                wizardInfo.VSScript = File.ReadAllText(wizardInfo.InputScript);
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
            wizardInfo.ProjectPreview = "项目名字: " + wizardInfo.TaskNamePrefix;
            wizardInfo.ProjectPreview += "\n\n编码器类型: " + wizardInfo.EncoderType;
            wizardInfo.ProjectPreview += "\n编码器路径: \n" + wizardInfo.EncoderPath;
            wizardInfo.ProjectPreview += "\n编码参数: \n" + wizardInfo.EncoderParam.Substring(0, Math.Min(30, wizardInfo.EncoderParam.Length - 1)) + "......";
            wizardInfo.ProjectPreview += "\n\n封装格式: " + wizardInfo.ContainerFormat;
            wizardInfo.ProjectPreview += "\n视频编码: " + wizardInfo.VideoFormat;
            wizardInfo.ProjectPreview += "\n视频帧率: " + String.Format("{0:0.000} fps", wizardInfo.Fps);
            wizardInfo.ProjectPreview += "\n音频编码(主音轨): " + wizardInfo.AudioFormat;

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

        private string GetEncoderInfo(string EncoderPath)
        {
            Process proc = new Process();
            ProcessStartInfo pstart = new ProcessStartInfo();
            pstart.FileName = EncoderPath;
            pstart.Arguments = "-V";
            pstart.RedirectStandardOutput = true;
            pstart.RedirectStandardError = true;
            pstart.WindowStyle = ProcessWindowStyle.Hidden;
            pstart.CreateNoWindow = true;
            pstart.UseShellExecute = false;
            proc.StartInfo = pstart;
            proc.EnableRaisingEvents = true;
            try
            {
                bool started = proc.Start();
            }
            catch (Exception e)
            {
                throw e;
            }

            proc.WaitForExit();

            StreamReader sr = null;
            string line = "";
            try
            {
                sr = proc.StandardError;
                line = sr.ReadToEnd();
            }
            catch (Exception)
            {
                throw;
            }

            return line;
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
            Debugger.Log(0, "", inputTemplate.Length.ToString() + "\n");
            foreach (string s in inputTemplate)
            {
                Debugger.Log(0, "", s + "\n");
            }
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
                cleaner.Clean(inputFile, new List<string> { wizardInfo.InputScript });

                // 新建文件（inputname.m2ts-mm-dd-HH-MM.vpy）
                string vpy = inputTemplate[0] + inputTemplate[1] + "r'" +
                    inputFile + "'" + inputTemplate[3];

                DateTime time = DateTime.Now;

                string fileName = inputFile + "-" + time.ToString("MMddHHmm") + ".vpy";
                System.IO.File.WriteAllText(fileName, vpy);

                var finfo = new System.IO.FileInfo(inputFile);
                TaskDetail td = new TaskDetail();
                td.TaskName = finfo.Name;
                if (wizardInfo.TaskNamePrefix != "")
                {
                    td.TaskName = wizardInfo.TaskNamePrefix + "-" + td.TaskName;
                }

                td.InputScript = fileName;

                td.EncoderPath = wizardInfo.EncoderPath;
                td.EncoderParam = wizardInfo.EncoderParam;
                td.InputFile = inputFile;

                td.ContainerFormat = wizardInfo.ContainerFormat;
                td.Fps = wizardInfo.Fps;
                td.FpsNum = wizardInfo.FpsNum;
                td.FpsDen = wizardInfo.FpsDen;
                td.VideoFormat = wizardInfo.VideoFormat;
                td.AudioFormat = wizardInfo.AudioFormat;

                td.IncludeSub = wizardInfo.IncludeSub;
                td.SubtitleLanguage = wizardInfo.SubtitleLanguage;

                foreach (var audio in wizardInfo.AudioTracks)
                {
                    td.AudioTracks.Add(audio);
                }

                // 更新输出文件拓展名
                if (!td.UpdateOutputFileName())
                {
                    System.Windows.MessageBox.Show("格式错误！", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                td.IsExtAudioOnly = false;

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
