using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using OKEGui.Utils;
using OKEGui.Model;
using OKEGui.Worker;
using OKEGui.Task;
using Newtonsoft.Json;

namespace OKEGui
{
    /// <summary>
    /// WizardWindow.xaml 的交互逻辑
    /// 通过Json文件读入一个新的task
    /// 详细实现在Task/NewTaskService
    /// </summary>
    public partial class WizardWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("WizardWindow");
        // Wizard里需要显示的内容。
        private class NewTask : INotifyPropertyChanged
        {
            // 需要载入的json文件
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

            // 生成的任务预览，会显示在wizard页面上
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

            // 输入文件
            public ObservableCollection<string> InputFile { get; } = new ObservableCollection<string>();

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                PropertyChanged?.Invoke(this, e);
            }
        }

        private readonly NewTask wizardInfo = new NewTask();
        private readonly WorkerManager workerManager;
        private TaskProfile json;
        private string vsScript;
        private int eachFreeMemory;
        public WizardWindow(WorkerManager w)
        {
            InitializeComponent();
            taskWizard.BackButtonContent = "上一步";
            taskWizard.CancelButtonContent = "取消";
            taskWizard.FinishButtonContent = "完成";
            taskWizard.HelpButtonContent = "帮助";
            taskWizard.HelpButtonVisibility = Visibility.Hidden;
            taskWizard.NextButtonContent = "下一步";
            DataContext = wizardInfo;

            workerManager = w;

            if (Initializer.Config.memoryLimit > 2000)
            {
                eachFreeMemory = (Initializer.Config.memoryLimit - workerManager.GetWorkerCount() * 2000) / workerManager.GetWorkerCount();
            }
            else
            {
                eachFreeMemory = 0;
            }
        }

        // 读入json文件，检查项目设置，并生成预览信息
        private bool LoadJsonProfile(string filePath)
        {
            DirectoryInfo jsonDir = new DirectoryInfo(filePath).Parent;

            // 读入json文件
            json = AddTaskService.LoadJsonAsProfile(filePath, jsonDir);
            if (json == null)
            {
                return false;
            }

            // 读入json里指定的vs脚本，并检查#OKE:INPUTFILE 标签
            vsScript = AddTaskService.LoadVsScript(json, jsonDir);
            if (string.IsNullOrEmpty(vsScript))
            {
                return false;
            }

            // 读入json里指定的输入文件，加入到inputFile里
            int fileCount = AddTaskService.LoadInputFiles(json, jsonDir, wizardInfo.InputFile);
            SelectInputFile.CanFinish = fileCount > 0;


            // 预览
            wizardInfo.ProjectPreview = json.ToString();
            return true;
        }

        //拖拽输入
        private void SelectProjectFile_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string fileName = (e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[])[0];

            wizardInfo.ProjectFile = fileName;
            SelectProjectFile.CanSelectNextPage = LoadJsonProfile(wizardInfo.ProjectFile);
        }


        private void SelectProjectFile_PreviewDrop(object sender, System.Windows.DragEventArgs e)
        {
            string fileDrop = System.Windows.DataFormats.FileDrop;
            if (e.Data.GetDataPresent(fileDrop))
            {
                string ext = Path.GetExtension((e.Data.GetData(fileDrop) as string[])[0]).ToLower();
                if (ext != ".json" && ext != ".yaml" && ext != ".yml")
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                    e.Handled = true;
                }
            }
        }


        private void OpenProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "OKEGui 项目文件 (*.json;*.yaml;*.yml)|*.json;*.yaml;*.yml";
            var result = ofd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            wizardInfo.ProjectFile = ofd.FileName;
            SelectProjectFile.CanSelectNextPage = LoadJsonProfile(wizardInfo.ProjectFile);
        }

        private void OpenInputFile_Click(object sender, RoutedEventArgs e)
        {
            using (var ofd = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "视频文件 (*.m2ts, *.mkv, *.ts, *.m2v, *.vob)|*.m2ts;*.mkv;*.ts;*.m2v;*.vob"
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

                SelectInputFile.CanFinish = wizardInfo.InputFile.Count > 0;
            }
        }

        // 为所有输入文件生成vs脚本，并添加任务至TaskManager。
        private void WizardFinish(object sender, RoutedEventArgs e)
        {
          try {
            // 处理PROJECTDIR标签
            if (Constants.projectDirRegex.IsMatch(vsScript)) {
                string[] dirTag = Constants.projectDirRegex.Split(vsScript);
                string projectDir = new DirectoryInfo(wizardInfo.ProjectFile).Parent.FullName;
                vsScript = dirTag[0] + dirTag[1] + "R\"" + projectDir + "\"" + dirTag[3];
            }

            string updatedVsScript = vsScript;

            // 处理MEMORY标签
            if (eachFreeMemory > 0 && Constants.memoryRegex.IsMatch(updatedVsScript))
            {
                string[] memoryTag = Constants.memoryRegex.Split(updatedVsScript);
                updatedVsScript = memoryTag[0] + memoryTag[1] + eachFreeMemory.ToString() + memoryTag[3];
            }

            // 处理DEBUG标签
            if (Constants.debugRegex.IsMatch(updatedVsScript))
            {
                string[] debugTag = Constants.debugRegex.Split(updatedVsScript);
                if (debugTag.Length < 4)
                {
                    // error
                    System.Windows.MessageBox.Show("Debug标签语法错误！", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                updatedVsScript = debugTag[0] + debugTag[1] + "None" + debugTag[3];
            }

            string[] inputTemplate = Constants.inputRegex.Split(updatedVsScript);

            // 新建任务
            // 1、清理残留文件
            // 2、新建脚本文件
            // 3、新建任务参数
            Cleaner cleaner = new Cleaner();
            foreach (string inputFile in wizardInfo.InputFile)
            {
                List<TaskDetail> existing = workerManager.tm.GetTasksByInputFile(inputFile);
                bool skip = existing.Any(i => i.Progress == TaskStatus.TaskProgress.RUNNING || i.Progress == TaskStatus.TaskProgress.WAITING);

                if (skip)
                {
                    System.Windows.MessageBox.Show($"{inputFile}已经在任务列表里，将跳过处理。", $"{inputFile}已经在任务列表里", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                // 清理文件
                cleaner.Clean(inputFile, new List<string> { json.InputScript, inputFile + ".lwi" });

                EpisodeConfig epConfig = null;
                string cfgPath = null;
                string[] cfgSuffixList = {".json", ".yaml", ".yml"};

                foreach (string suffix in cfgSuffixList)
                {
                    FileInfo cfgFile = new FileInfo(inputFile + suffix);
                    if (cfgFile.Exists)
                    {
                        cfgPath = inputFile + suffix;
                        break;
                    }
                }
                if (cfgPath != null)
                {
                    epConfig = AddEpProfileService.LoadJsonAsProfile(cfgPath);
                    if (epConfig == null)
                    {
                        continue;
                    }
                }

                // 新建vpy文件（inputname.m2ts-mmddHHMM.vpy）
                string vpy = inputTemplate[0] + inputTemplate[1] + "R\"" +
                    inputFile + "\"" + inputTemplate[3];

                string inputSuffixPath = inputFile.Replace(':', '_');
                const string stripCommonPathComponents = "BDBOX/BDROM/BD/BDMV/STREAM/BD_VIDEO"; // FIXME: do not hardcode this.
                string[] strippedComponents = stripCommonPathComponents.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var comp in strippedComponents)
                {
                    inputSuffixPath = Regex.Replace(inputSuffixPath, @"[/\\]" + Regex.Escape(comp) + @"[/\\]", "\\");
                }
                string[] inputSuffixComponents = inputSuffixPath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (inputSuffixComponents.Length > 3 && Initializer.Config.reducePath)
                {
                    // inputSuffixComponents[0] is drive number, inputSuffixComponents[-1] is filename, inputSuffixComponents[1:-2] is the effective path to be reduced.
                    string effectivePathPrefix = string.Join("\\", inputSuffixComponents, 1, inputSuffixComponents.Length - 3);
                    string effectivePathLast = inputSuffixComponents[inputSuffixComponents.Length - 2];
                    Regex rVolNumber = new Regex(@".*Vol[.\- ]?(?<vol>\d+).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    Match mLast = rVolNumber.Match(effectivePathLast);

                    string newEffectivePath;
                    if (mLast.Success)
                    {
                        // If the last level path contains a volume number, keep the last level path unchanged.
                        newEffectivePath = effectivePathLast;
                    }
                    else
                    {
                        // By default, the last level path is preserved and the crc of the prefix path is added to prevent naming conflicts.
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(effectivePathPrefix);
                        var crc = CRC32.Compute(buffer);
                        newEffectivePath = crc.ToString("X8") + "-" + effectivePathLast;
                    }
                    string[] newInputSuffixComponents = new string[] {inputSuffixComponents[0], newEffectivePath, inputSuffixComponents[inputSuffixComponents.Length - 1]};
                    inputSuffixPath = string.Join("\\", newInputSuffixComponents);
                }
                Logger.Debug("inputSuffixPathComponents: " + string.Join(", ", inputSuffixComponents));
                Logger.Debug("Transformed input path: " + inputSuffixPath);

                string newPath = new DirectoryInfo(wizardInfo.ProjectFile).Parent.FullName + "/" + inputSuffixPath;
                Directory.CreateDirectory(new DirectoryInfo(newPath).Parent.FullName);
                string outPath = Regex.Replace(newPath, @"[/\\]._[/\\]", "\\output\\");
                Directory.CreateDirectory(new DirectoryInfo(outPath).Parent.FullName);

                DateTime time = DateTime.Now;
                string fileName = newPath + "-" + time.ToString("MMddHHmm") + ".vpy";
                File.WriteAllText(fileName, vpy);

                FileInfo finfo = new FileInfo(inputFile);
                TaskDetail td = new TaskDetail
                {
                    TaskName = string.IsNullOrEmpty(json.ProjectName) ? finfo.Name : json.ProjectName + "-" + finfo.Name,
                    Taskfile = json.Clone() as TaskProfile,
                    InputFile = inputFile,
                };
                td.Taskfile.WorkingPathPrefix = newPath;
                td.Taskfile.OutputPathPrefix = outPath;

                // 更新输入脚本和输出文件拓展名
                td.Taskfile.InputScript = fileName;
                td.UpdateOutputFileName();

                if (epConfig != null)
                {
                    td.Taskfile.Config = epConfig.Clone() as EpisodeConfig;
                    td.Taskfile.IsReEncode = epConfig.EnableReEncode;
                    if (td.Taskfile.IsReEncode)
                    {
                        Logger.Debug("Processed epConfig: " + td.Taskfile.Config.ToString());
                        if (td.Taskfile.ContainerFormat != "MKV")
                        {
                            System.Windows.MessageBox.Show($"ReEncode项目暂时只支持mkv格式输出，{td.Taskfile.ContainerFormat}格式暂不支持", "封装格式不支持", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue;
                        }
                    }
                }
                else
                {
                    td.Taskfile.IsReEncode = false;
                }

                // 寻找章节
                td.ChapterStatus = ChapterService.UpdateChapterStatus(td);

                // 任务类型
                if (td.Taskfile.IsReEncode)
                    td.TaskType = TaskStatus.TaskTypeEnum.ReEncode;
                else
                    td.TaskType = TaskStatus.TaskTypeEnum.Normal;

                workerManager.AddTask(td);
            }
          }
          catch (Exception ex)
          {
              Logger.Error(ex.StackTrace);
              System.Windows.MessageBox.Show("任务初始化失败：" + ex.Message);
          }
        }

        private void DeleteInput_Click(object sender, RoutedEventArgs e)
        {
            DeleteInputVideoFiles();
        }

        private void InputList_PreviewDrop(object sender, System.Windows.DragEventArgs e)
        {
            // nothing
        }

        private void InputList_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[] inputPaths = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            string[] allowedExts = { ".mkv", ".m2ts", ".ts", ".m2v", ".vob" };

            string[] inputFileList = inputPaths.Where(i => allowedExts.Contains(Path.GetExtension(i).ToLower())).ToArray();

            foreach (string inputFile in inputFileList)
            {
                if (!wizardInfo.InputFile.Contains(inputFile))
                {
                    wizardInfo.InputFile.Add(inputFile);
                }
            }

            SelectInputFile.CanFinish = wizardInfo.InputFile.Count > 0;
        }

        private void InputList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                DeleteInputVideoFiles();
            }
        }

        private void DeleteInputVideoFiles()
        {
            var list = InputList.SelectedItems;

            if (list.Count == 0)
            {
                return;
            }

            if (list.Count > 1)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("是否删除" + list.Count.ToString() + "个文件？", "新建任务向导", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            List<string> selectList = new List<string>();
            foreach (object item in list)
            {
                selectList.Add(item as string);
            }

            for (int i = 0; i < selectList.Count; i++)
            {
                foreach (string selected in selectList)
                {
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
