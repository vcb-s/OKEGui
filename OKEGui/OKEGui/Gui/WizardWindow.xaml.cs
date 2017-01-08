using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Newtonsoft.Json;

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

                set {
                    projectFile = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("ProjectFile"));
                }
            }

            private int configVersion;

            public int ConfigVersion
            {
                get { return configVersion; }
                set {
                    configVersion = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ConfigVersion"));
                }
            }

            private string projectPreview;

            public string ProjectPreview
            {
                get { return projectPreview; }

                set {
                    projectPreview = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("ProjectPreview"));
                }
            }

            private string taskNamePrefix;

            public string TaskNamePrefix
            {
                get { return taskNamePrefix; }

                set {
                    taskNamePrefix = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("TaskNamePrefix"));
                }
            }

            private string inputScript;

            public string InputScript
            {
                get { return inputScript; }

                set {
                    inputScript = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("InputScript"));
                }
            }

            private string vsscript;

            public string VSScript
            {
                get { return vsscript; }

                set {
                    vsscript = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("VSScript"));
                }
            }

            private ObservableCollection<string> inputFile;

            public ObservableCollection<string> InputFile
            {
                get {
                    if (inputFile == null) {
                        inputFile = new ObservableCollection<string>();
                    }

                    return inputFile;
                }
            }

            // 最终成品 c:\xxx\123.mkv
            private string outputFile;

            public string OutputFile
            {
                get { return outputFile; }
                set {
                    outputFile = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("OutputFile"));
                }
            }

            // mp4, mkv, null
            private string containerFormat;

            public string ContainerFormat
            {
                get { return containerFormat; }
                set {
                    containerFormat = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ContainerFormat"));
                }
            }

            // HEVC, AVC
            private string videoFormat;

            public string VideoFormat
            {
                get { return videoFormat; }
                set {
                    videoFormat = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("VideoFormat"));
                }
            }

            // FLAC, AAC(m4a)
            private string audioFormat;

            public string AudioFormat
            {
                get { return audioFormat; }
                set {
                    audioFormat = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("AudioFormat"));
                }
            }

            // 音频码率
            private int audioBitrate;

            public int AudioBitrate
            {
                get { return audioBitrate; }
                set {
                    audioBitrate = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("audioBitrate"));
                }
            }

            private ObservableCollection<JobDetails.AudioInfo> audioTracks;

            public ObservableCollection<JobDetails.AudioInfo> AudioTracks
            {
                get {
                    if (audioTracks == null) {
                        audioTracks = new ObservableCollection<JobDetails.AudioInfo>();
                    }

                    return audioTracks;
                }
            }

            private string encoderPath;

            public string EncoderPath
            {
                get { return encoderPath; }
                set {
                    encoderPath = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("EncoderPath"));
                }
            }

            private string encoderParam;

            public string EncoderParam
            {
                get { return encoderParam; }
                set {
                    encoderParam = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("EncoderParam"));
                }
            }

            private string encoderType;

            public string EncoderType
            {
                get { return encoderType; }
                set {
                    encoderType = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("EncoderType"));
                }
            }

            private string encoderInfo;

            public string EncoderInfo
            {
                get { return encoderInfo; }
                set {
                    encoderInfo = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("EncoderInfo"));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, e);
            }
        }

        public class IniFiles
        {
            public string FileName; //INI文件名

            [DllImport("kernel32")]
            private static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);

            [DllImport("kernel32")]
            private static extern int GetPrivateProfileString(string section, string key, string def, byte[] retVal, int size, string filePath);

            //类的构造函数，传递INI文件名
            public IniFiles(string AFileName)
            {
                // 判断文件是否存在
                FileInfo fileInfo = new FileInfo(AFileName);
                //Todo:搞清枚举的用法
                if ((!fileInfo.Exists)) { //|| (FileAttributes.Directory in fileInfo.Attributes))
                                          //文件不存在，建立文件
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(AFileName, false, System.Text.Encoding.Default);
                    try {
                        sw.Write("#表格配置档案");
                        sw.Close();
                    } catch {
                        throw (new ApplicationException("Ini文件不存在"));
                    }
                }
                //必须是完全路径，不能是相对路径
                FileName = fileInfo.FullName;
            }

            //写INI文件
            public void WriteString(string Section, string Key, string Value)
            {
                if (!WritePrivateProfileString(Section, Key, Value, FileName)) {
                    throw (new ApplicationException("写Ini文件出错"));
                }
            }

            //读取INI文件指定
            public string ReadString(string Section, string Key, string Default)
            {
                Byte[] Buffer = new Byte[65535];
                int bufLen = GetPrivateProfileString(Section, Key, Default, Buffer, Buffer.GetUpperBound(0), FileName);
                //必须设定0（系统默认的代码页）的编码方式，否则无法支持中文
                string s = Encoding.GetEncoding(0).GetString(Buffer);
                s = s.Substring(0, bufLen);
                return s.Trim();
            }

            //读整数
            public int ReadInteger(string Section, string Key, int Default)
            {
                string intStr = ReadString(Section, Key, Convert.ToString(Default));
                try {
                    return Convert.ToInt32(intStr);
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                    return Default;
                }
            }

            //写整数
            public void WriteInteger(string Section, string Key, int Value)
            {
                WriteString(Section, Key, Value.ToString());
            }

            //读布尔
            public bool ReadBool(string Section, string Key, bool Default)
            {
                try {
                    return Convert.ToBoolean(ReadString(Section, Key, Convert.ToString(Default)));
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                    return Default;
                }
            }

            //写Bool
            public void WriteBool(string Section, string Key, bool Value)
            {
                WriteString(Section, Key, Convert.ToString(Value));
            }

            //从Ini文件中，将指定的Section名称中的所有Key添加到列表中
            public void ReadSection(string Section, StringCollection Keys)
            {
                Byte[] Buffer = new Byte[16384];
                //Keys.Clear();

                int bufLen = GetPrivateProfileString(Section, null, null, Buffer, Buffer.GetUpperBound(0),
                  FileName);
                //对Section进行解析
                GetStringsFromBuffer(Buffer, bufLen, Keys);
            }

            private void GetStringsFromBuffer(Byte[] Buffer, int bufLen, StringCollection Strings)
            {
                Strings.Clear();
                if (bufLen != 0) {
                    int start = 0;
                    for (int i = 0; i < bufLen; i++) {
                        if ((Buffer[i] == 0) && ((i - start) > 0)) {
                            String s = Encoding.GetEncoding(0).GetString(Buffer, start, i - start);
                            Strings.Add(s);
                            start = i + 1;
                        }
                    }
                }
            }

            //从Ini文件中，读取所有的Sections的名称
            public void ReadSections(StringCollection SectionList)
            {
                //Note:必须得用Bytes来实现，StringBuilder只能取到第一个Section
                byte[] Buffer = new byte[65535];
                int bufLen = 0;
                bufLen = GetPrivateProfileString(null, null, null, Buffer,
                  Buffer.GetUpperBound(0), FileName);
                GetStringsFromBuffer(Buffer, bufLen, SectionList);
            }

            //读取指定的Section的所有Value到列表中
            public void ReadSectionValues(string Section, NameValueCollection Values)
            {
                StringCollection KeyList = new StringCollection();
                ReadSection(Section, KeyList);
                Values.Clear();
                foreach (string key in KeyList) {
                    Values.Add(key, ReadString(Section, key, ""));
                }
            }

            ////读取指定的Section的所有Value到列表中，
            //public void ReadSectionValues(string Section, NameValueCollection Values,char splitString)
            //{   string sectionValue;
            //    string[] sectionValueSplit;
            //    StringCollection KeyList = new StringCollection();
            //    ReadSection(Section, KeyList);
            //    Values.Clear();
            //    foreach (string key in KeyList)
            //    {
            //        sectionValue=ReadString(Section, key, "");
            //        sectionValueSplit=sectionValue.Split(splitString);
            //        Values.Add(key, sectionValueSplit[0].ToString(),sectionValueSplit[1].ToString());
            //    }
            //}

            //清除某个Section
            public void EraseSection(string Section)
            {
                //
                if (!WritePrivateProfileString(Section, null, null, FileName)) {
                    throw (new ApplicationException("无法清除Ini文件中的Section"));
                }
            }

            //删除某个Section下的键
            public void DeleteKey(string Section, string Key)
            {
                WritePrivateProfileString(Section, Key, null, FileName);
            }

            //Note:对于Win9X，来说需要实现UpdateFile方法将缓冲中的数据写入文件
            //在Win NT, 2000和XP上，都是直接写文件，没有缓冲，所以，无须实现UpdateFile
            //执行完对Ini文件的修改之后，应该调用本方法更新缓冲区。
            public void UpdateFile()
            {
                WritePrivateProfileString(null, null, null, FileName);
            }

            //检查某个Section下的某个键值是否存在
            public bool ValueExists(string Section, string Key)
            {
                //
                StringCollection Keys = new StringCollection();
                ReadSection(Section, Keys);
                return Keys.IndexOf(Key) > -1;
            }

            //确保资源的释放
            ~IniFiles()
            {
                UpdateFile();
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
            public int Version { get; set; }
            public string ProjectName { get; set; }
            public string EncoderType { get; set; }
            public string Encoder { get; set; }
            public string EnocderParam { get; set; }
            public string ContainerFormat { get; set; }
            public string VideoFormat { get; set; }
            public List<JobDetails.AudioInfo> AudioTracks { get; set; }
            public string InputScript { get; set; }
        }

        private bool LoadJsonProfile(string profile)
        {
            // TODO: 测试
            string profileStr = File.ReadAllText(profile);
            JsonProfile okeProj = JsonConvert.DeserializeObject<JsonProfile>(profileStr);
            DirectoryInfo projDir = new DirectoryInfo(wizardInfo.ProjectFile).Parent;

            // 检查参数
            if (okeProj.Version != 2) {
                return false;
            }

            if (okeProj.EncoderType.ToLower() != "x265") {
                return false;
            }
            wizardInfo.EncoderType = okeProj.EncoderType.ToLower();

            // 获取编码器全路径
            FileInfo encoder = new FileInfo(projDir.FullName + "\\" + okeProj.Encoder);
            if (encoder.Exists) {
                wizardInfo.EncoderPath = encoder.FullName;
                wizardInfo.EncoderInfo = this.GetEncoderInfo(wizardInfo.EncoderPath);
            }

            wizardInfo.EncoderParam = okeProj.EnocderParam;

            Dictionary<string, ComboBoxItem> comboItems = new Dictionary<string, ComboBoxItem>() {
                { "MKV",    MKVContainer},
                { "MP4",    MP4Container },
                { "HEVC",   HEVCVideo},
                { "AVC",    AVCVideo },
                { "FLAC",   FLACAudio },
                { "AAC",    AACAudio},
            };

            // 设置封装格式
            wizardInfo.ContainerFormat = okeProj.ContainerFormat.ToUpper();
            if (wizardInfo.ContainerFormat != "MKV" && wizardInfo.ContainerFormat != "MP4" &&
                wizardInfo.ContainerFormat != "NULL" && wizardInfo.ContainerFormat != "RAW") {
                return false;
            }
            comboItems[wizardInfo.ContainerFormat].IsSelected = true;

            // 设置视频编码
            if (okeProj.VideoFormat.ToUpper() != "HEVC") {
                return false;
            }
            wizardInfo.VideoFormat = okeProj.VideoFormat.ToUpper();
            comboItems[wizardInfo.VideoFormat].IsSelected = true;

            if (okeProj.AudioTracks.Count > 0) {
                // 包含音轨
                wizardInfo.AudioFormat = okeProj.AudioTracks[0].Format.ToUpper();
                wizardInfo.AudioBitrate = okeProj.AudioTracks[0].Bitrate;

                foreach (var track in okeProj.AudioTracks) {
                    wizardInfo.AudioTracks.Add(track);
                }
            }

            if (wizardInfo.AudioFormat != "FLAC" && wizardInfo.AudioFormat != "AAC" &&
                wizardInfo.AudioFormat != "ALAC") {
                return false;
            }
            comboItems[wizardInfo.AudioFormat].IsSelected = true;

            wizardInfo.InputScript = new FileInfo(projDir.FullName + "\\" + okeProj.InputScript).FullName;
            wizardInfo.VSScript = File.ReadAllText(wizardInfo.InputScript);

            // 预览
            wizardInfo.ProjectPreview += "项目名字: " + wizardInfo.TaskNamePrefix;
            wizardInfo.ProjectPreview += "\n\n编码器类型: " + wizardInfo.EncoderType;
            wizardInfo.ProjectPreview += "\n编码器路径: \n" + wizardInfo.EncoderPath;
            wizardInfo.ProjectPreview += "\n编码参数: \n" + wizardInfo.EncoderParam;
            wizardInfo.ProjectPreview += "\n\n封装格式: " + wizardInfo.ContainerFormat;
            wizardInfo.ProjectPreview += "\n视频编码: " + wizardInfo.VideoFormat;
            wizardInfo.ProjectPreview += "\n音频编码(主音轨): " + wizardInfo.AudioFormat;

            return true;
        }

        private void OpenProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "OKEGui 项目文件 (*.okeproj, *.json)|*.okeproj;*.json";
            var result = ofd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel) {
                return;
            }

            wizardInfo.ProjectFile = ofd.FileName;
            if (new FileInfo(wizardInfo.ProjectFile).Extension.ToLower() == ".json") {
                if (!LoadJsonProfile(wizardInfo.ProjectFile)) {
                    // 配置文件无效
                    System.Windows.MessageBox.Show("无效的配置文件。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            // 配置文件 INI格式
            // Demo1.okeproj

            //[OKEProject]
            //ProjectVersion = 1
            //ProjectName = Demo1
            //EncoderType = x265
            //Encoder = x265 - 10b.exe
            //EnocderParam = "--crf 19"
            //ContainerFormat = mkv
            //VideoFormat = hevc
            //AudioFormat = flac
            //AudioFormat = aac:128
            //InputScript = demo1.vpy
            //ExtractAudioTrack = true(暂时不使用)

            IniFiles okeproj = new IniFiles(wizardInfo.ProjectFile);
            DirectoryInfo projDir = new DirectoryInfo(wizardInfo.ProjectFile).Parent;

            wizardInfo.ConfigVersion = okeproj.ReadInteger("OKEProject", "ProjectVersion", 0);
            if (wizardInfo.ConfigVersion < 1) {
                System.Windows.MessageBox.Show("无效的配置文件。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            wizardInfo.TaskNamePrefix = okeproj.ReadString("OKEProject", "ProjectName", "");

            wizardInfo.EncoderType = okeproj.ReadString("OKEProject", "EncoderType", "").ToLower();
            if (wizardInfo.EncoderType != "x265") {
                System.Windows.MessageBox.Show("目前只支持x265编码器。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 获取编码器全路径
            FileInfo encoder = new FileInfo(projDir.FullName + "\\" + okeproj.ReadString("OKEProject", "Encoder", ""));
            if (encoder.Exists) {
                wizardInfo.EncoderPath = encoder.FullName;
                wizardInfo.EncoderInfo = this.GetEncoderInfo(wizardInfo.EncoderPath);
            }

            wizardInfo.EncoderParam = okeproj.ReadString("OKEProject", "EnocderParam", "");

            Dictionary<string, ComboBoxItem> comboItems = new Dictionary<string, ComboBoxItem>() {
                { "MKV",    MKVContainer},
                { "MP4",    MP4Container },
                { "HEVC",   HEVCVideo},
                { "AVC",    AVCVideo },
                { "FLAC",   FLACAudio },
                { "AAC",    AACAudio},
            };

            wizardInfo.ContainerFormat = okeproj.ReadString("OKEProject", "ContainerFormat", "").ToLower();
            if (wizardInfo.ContainerFormat != "mkv" && wizardInfo.ContainerFormat != "mp4" &&
                wizardInfo.ContainerFormat != "null") {
                System.Windows.MessageBox.Show("封装格式不正确。只支持mkv，mp4,或者无封装", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            comboItems[wizardInfo.ContainerFormat.ToUpper()].IsSelected = true;

            wizardInfo.VideoFormat = okeproj.ReadString("OKEProject", "VideoFormat", "").ToUpper();
            if (wizardInfo.VideoFormat != "HEVC") {
                System.Windows.MessageBox.Show("目前只支持HEVC编码。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            comboItems[wizardInfo.VideoFormat].IsSelected = true;

            wizardInfo.AudioFormat = okeproj.ReadString("OKEProject", "AudioFormat", "").ToUpper();
            wizardInfo.AudioBitrate = 128;
            var audioParam = wizardInfo.AudioFormat.Split(':');
            if (audioParam.Length == 2) {
                int bitrate = 0;
                if (int.TryParse(audioParam[1], out bitrate)) {
                    wizardInfo.AudioBitrate = bitrate == 0 ? 128 : bitrate;
                }
            }

            if (wizardInfo.AudioFormat != "FLAC" && wizardInfo.AudioFormat != "AAC" &&
                wizardInfo.AudioFormat != "ALAC") {
                System.Windows.MessageBox.Show("音频编码格式不支持。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            comboItems[wizardInfo.AudioFormat].IsSelected = true;

            wizardInfo.InputScript = new FileInfo(projDir.FullName + "\\" + okeproj.ReadString("OKEProject", "InputScript", "")).FullName;
            wizardInfo.VSScript = File.ReadAllText(wizardInfo.InputScript);

            // 预览
            wizardInfo.ProjectPreview += "项目名字: " + wizardInfo.TaskNamePrefix;
            wizardInfo.ProjectPreview += "\n\n编码器类型: " + wizardInfo.EncoderType;
            wizardInfo.ProjectPreview += "\n编码器路径: \n" + wizardInfo.EncoderPath;
            wizardInfo.ProjectPreview += "\n编码参数: \n" + wizardInfo.EncoderParam;
            wizardInfo.ProjectPreview += "\n\n封装格式: " + wizardInfo.ContainerFormat;
            wizardInfo.ProjectPreview += "\n视频编码: " + wizardInfo.VideoFormat;
            wizardInfo.ProjectPreview += "\n音频编码: " + wizardInfo.AudioFormat;
        }

        private void OpenScriptBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "VapourSynth脚本 (*.vpy)|*.vpy";
            var result = ofd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel) {
                return;
            }

            wizardInfo.InputScript = ofd.FileName;
            wizardInfo.VSScript = File.ReadAllText(wizardInfo.InputScript);

            // 可以下一步
            SelectVSScript.CanSelectNextPage = true;
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
            try {
                bool started = proc.Start();
            } catch (Exception e) {
                throw e;
            }

            proc.WaitForExit();

            StreamReader sr = null;
            string line = "";
            try {
                sr = proc.StandardError;
                line = sr.ReadToEnd();
            } catch (Exception) {
                throw;
            }

            return line;
        }

        private void SelectEncoder_Loaded(object sender, RoutedEventArgs e)
        {
            if (wizardInfo.EncoderPath != "") {
                SelectEncoder.CanSelectNextPage = true;
            }
        }

        private void OpenEncoderBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "视频编码器 (*.exe)|*.exe";
            var result = ofd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel) {
                return;
            }

            wizardInfo.EncoderPath = ofd.FileName;
            wizardInfo.EncoderInfo = this.GetEncoderInfo(wizardInfo.EncoderPath);

            SelectEncoder.CanSelectNextPage = true;
        }

        private void WalkDirectoryTree(System.IO.DirectoryInfo root, Action<FileInfo> func, bool IsSearchSubDir = true)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e) {
                // log.Add(e.Message);
            } catch (System.IO.DirectoryNotFoundException e) {
                // Console.WriteLine(e.Message);
            }

            if (files != null) {
                foreach (System.IO.FileInfo fi in files) {
                    func(fi);
                }

                if (IsSearchSubDir) {
                    // 搜索子目录
                    subDirs = root.GetDirectories();
                    foreach (System.IO.DirectoryInfo dirInfo in subDirs) {
                        WalkDirectoryTree(dirInfo, func, IsSearchSubDir);
                    }
                }
            }
        }

        private void OpenInputFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "视频文件 (*.*)|*.*";
            var result = ofd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel) {
                return;
            }

            // TODO: 重复文件处理
            if (!wizardInfo.InputFile.Contains(ofd.FileName)) {
                wizardInfo.InputFile.Add(ofd.FileName);
            } else {
                System.Windows.MessageBox.Show("文件已添加！", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (wizardInfo.InputFile.Count > 1 && !wizardInfo.VSScript.Contains("#OKE:INPUTFILE")) {
                System.Windows.MessageBox.Show("添加多个输入文件请确保VapourSynth脚本使用OKE提供的模板。点击上一步可以重新选择VapourSynth脚本。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            SelectInputFile.CanSelectNextPage = true;
        }

        private void OpenInputFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "请选择视频文件夹";
            // fbd.SelectedPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
            fbd.SelectedPath = "C:\\";
            DialogResult result = fbd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel) {
                return;
            }

            string dir = fbd.SelectedPath.Trim();

            // 历遍改目录，添加全部支持的文件类型
            // 默认为m2ts, mp4, mkv
            MessageBoxResult isSearchSub = System.Windows.MessageBox.Show("是否搜索子目录？", "新建任务向导", MessageBoxButton.YesNo);
            WalkDirectoryTree(new DirectoryInfo(dir), (FileInfo fi) => {
                if (fi.Extension.ToUpper() == ".M2TS" || fi.Extension.ToUpper() == ".MP4" ||
                    fi.Extension.ToUpper() == ".MKV") {
                    // TODO: 重复文件处理
                    if (!wizardInfo.InputFile.Contains(fi.FullName)) {
                        wizardInfo.InputFile.Add(fi.FullName);
                    }
                }
            }, isSearchSub == MessageBoxResult.Yes);

            if (wizardInfo.InputFile.Count > 1 && !wizardInfo.VSScript.Contains("#OKE:INPUTFILE")) {
                System.Windows.MessageBox.Show("添加多个输入文件请确保VapourSynth脚本使用OKE提供的模板。点击上一步可以重新选择VapourSynth脚本。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            SelectInputFile.CanSelectNextPage = wizardInfo.InputFile.Count != 0;
        }

        private void SelectFormat_Leave(object sender, RoutedEventArgs e)
        {
            string container = ContainerFormat.Text;
            string video = VideoFormat.Text;
            string audio = AudioFormat.Text;

            if (container == "不封装") {
                container = "";
            }

            // 简单检查
            if (container == "MP4" && audio == "FLAC") {
                System.Windows.MessageBox.Show("格式选择错误！\nMP4不能封装FLAC格式。音频格式将改为AAC。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                audio = "AAC";
            }

            wizardInfo.ContainerFormat = container;
            wizardInfo.VideoFormat = video;
            wizardInfo.AudioFormat = audio;
        }

        private void WizardFinish(object sender, RoutedEventArgs e)
        {
            // 检查输入脚本是否为oke模板
            var isTemplate = wizardInfo.VSScript.Contains("#OKE:INPUTFILE");

            // 使用正则解析模板, 多行忽略大小写
            Regex r = new Regex("#OKE:INPUTFILE([\\n\\r ]+\\w+[ ]*=[ ]*)([r]*[\"'].+[\"'])", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var inputTemplate = r.Split(wizardInfo.VSScript);
            if (inputTemplate.Length < 4 && wizardInfo.InputFile.Count() > 1) {
                System.Windows.MessageBox.Show("任务创建失败！添加多个输入文件请确保VapourSynth脚本使用OKE提供的模板。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 处理DEBUG标签
            // TODO: 是否进行调试输出
            if (wizardInfo.VSScript.Contains("#OKE:DEBUG") && true) {
                Regex dr = new Regex("#OKE:DEBUG([\\n\\r ]+\\w+[ ]*=[ ]*)(\\w+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var debugTag = dr.Split(inputTemplate[3]);
                if (debugTag.Length < 4) {
                    // error
                    System.Windows.MessageBox.Show("Debug标签语法错误！", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                inputTemplate[3] = debugTag[0] + debugTag[1] + "None" + debugTag[3];
            }

            // 新建任务
            // 1、新建脚本文件
            // 2、新建任务参数
            foreach (var inputFile in wizardInfo.InputFile) {
                // 新建文件（inputname.m2ts-mm-dd-HH-MM.vpy）
                string vpy = inputTemplate[0] + inputTemplate[1] + "r'" +
                    inputFile + "'" + inputTemplate[3];

                DateTime time = DateTime.Now;

                string fileName = inputFile + "-" + time.ToString("MMddHHmm") + ".vpy";
                System.IO.File.WriteAllText(fileName, vpy);

                var finfo = new System.IO.FileInfo(inputFile);
                JobDetails td = new JobDetails();
                td.TaskName = finfo.Name;
                if (wizardInfo.TaskNamePrefix != "") {
                    td.TaskName = wizardInfo.TaskNamePrefix + "-" + td.TaskName;
                }

                td.InputScript = fileName;

                td.EncoderPath = wizardInfo.EncoderPath;
                td.EncoderParam = wizardInfo.EncoderParam;
                td.InputFile = inputFile;

                td.ContainerFormat = wizardInfo.ContainerFormat;
                td.VideoFormat = wizardInfo.VideoFormat;
                td.AudioFormat = wizardInfo.AudioFormat;

                foreach (var audio in wizardInfo.AudioTracks) {
                    td.AudioTracks.Add(audio);
                }

                // 更新输出文件拓展名
                if (!td.UpdateOutputFileName()) {
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
            if (o == null) {
                return;
            }

            String input = o as string;
            int id = wizardInfo.InputFile.IndexOf(input);
            if (id == -1) {
                // 没有找到
                return;
            }

            wizardInfo.InputFile.RemoveAt(id);
        }

        private void DeleteInput_Click(object sender, RoutedEventArgs e)
        {
            var list = InputList.SelectedItems;

            if (list.Count == 0) {
                return;
            }

            if (list.Count > 1) {
                MessageBoxResult result = System.Windows.MessageBox.Show("是否删除" + (list.Count.ToString()) + "个文件？", "新建任务向导", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No) {
                    return;
                }
            }

            List<object> selectList = new List<object>();
            foreach (object item in list) {
                selectList.Add(item);
            }

            for (int i = 0; i < selectList.Count; i++) {
                foreach (object item in selectList) {
                    String selected = item as string;
                    int index = wizardInfo.InputFile.IndexOf(selected);
                    if (index != -1) {
                        wizardInfo.InputFile.RemoveAt(index);
                    }
                }
            }

            SelectInputFile.CanSelectNextPage = wizardInfo.InputFile.Count != 0;
        }

        private void SelectVSScript_Loaded(object sender, RoutedEventArgs e)
        {
            SelectVSScript.CanSelectNextPage = wizardInfo.VSScript != "";
        }
    }
}
