using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using OKEGui.Task;
using OKEGui.Utils;
using OKEGui.Worker;
using System.IO;
using System.Text.RegularExpressions;

namespace OKEGui
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// 相关概念解释:
    ///   Task: 从单个源（例如m2ts）到成品（例如mkv）的整个过程。task会在主程序界面的列表里显示。
    ///   Job: 每个Task会被分解成不同的Job，并依次执行。例如抽流，压制，封装等。Job是可以独立运行的最低单位。
    ///   JobProcessror: 负责执行每个Job的命令行Warpper。比如X265Encoder调用x265压制HEVC，FFMpegVolumeChecker调用ffmpeg检查音轨音量
    ///   Model: 储存媒体文件相关的信息。Info只带例如语言、封装选项等信息，Track则是File+Info的组合，MediaFile则是多条Track的合集
    ///   Worker: 每一个Task只会在一个Worker里进行，因此有几个Worker就允许几个Task同时进行。多开相关的选项。每个Task具体的实现流程由Worker负责执行。
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private SystemMenu _systemMenu;

        public int WorkerCount = 0;
        public TaskManager tm = new TaskManager();
        public WorkerManager wm;

        public MainWindow()
        {
            InitializeComponent();

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            Title += " v" + version;

            TaskList.ItemsSource = tm.taskStatus;

            wm = new WorkerManager(this, tm);

            BtnRun.IsEnabled = false;
            BtnMoveDown.IsEnabled = false;
            BtnMoveUp.IsEnabled = false;
            BtnMoveTop.IsEnabled = false;
            BtnPause.IsEnabled = false;
            BtnResume.IsEnabled = false;
            BtnChap.IsEnabled = false;
            BtnDelete.IsEnabled = false;
            BtnEmpty.IsEnabled = false;
            BtnCancelShutdown.IsEnabled = false;

            // 初始的worker数量等于Numa数量。
            int numaCount = NumaNode.NumaCount;
            for (int i = 0; i < numaCount; i++)
            {
                WorkerCount++;
                wm.AddWorker("工作单元-" + WorkerCount.ToString());
            }
            WorkerNumber.Text = "工作单元：" + WorkerCount.ToString();

            // 初始化更新菜单
            _systemMenu = new SystemMenu(this);
            _systemMenu.AddCommand("检查更新(&U)", () => { Updater.CheckUpdate(true); }, true);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            {
                hwndSource.AddHook(_systemMenu.HandleMessage);
            }
        }

        private void Checkbox_Changed(object sender, RoutedEventArgs e)
        {
            if (!wm.IsRunning)
            {
                BtnRun.IsEnabled = tm.HasNextTask();
                BtnChap.IsEnabled = BtnRun.IsEnabled;
            }
        }

        private void BtnRpc_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string rpcOutput = btn.Tag?.ToString();
            if (!string.IsNullOrEmpty(rpcOutput))
            {
                Process.Start(Initializer.Config.rpCheckerPath, $"-r \"{rpcOutput}\"");
            }
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtFreeMemory.Text))
            {
                // 新建任务。具体实现请见Gui/wizardWindow
                try
                {
                    var wizard = new WizardWindow(wm);
                    wizard.ShowDialog();
                    int activeTaskCount = tm.GetActiveTaskCount();
                    BtnRun.IsEnabled = activeTaskCount > 0;
                    BtnDelete.IsEnabled = activeTaskCount > 0;
                    BtnEmpty.IsEnabled = activeTaskCount > 0;
                    BtnMoveDown.IsEnabled = activeTaskCount > 1;
                    BtnMoveUp.IsEnabled = activeTaskCount > 1;
                    BtnMoveTop.IsEnabled = activeTaskCount > 2;
                    BtnChap.IsEnabled = activeTaskCount > 0;
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex.StackTrace);
                    Environment.Exit(0);
                }
            }
            else
            {
                MessageBox.Show("请输入系统可用空闲内存！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            BtnPause.IsEnabled = false;
            SubProcessService.PauseAll();
            BtnResume.IsEnabled = true;
        }
        private void BtnResume_Click(object sender, RoutedEventArgs e)
        {
            BtnResume.IsEnabled = false;
            SubProcessService.ResumeAll();
            BtnPause.IsEnabled = true;
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                wm.Start();
                BtnDeleteWorker.IsEnabled = false;
                BtnEmpty.IsEnabled = false;
                BtnRun.IsEnabled = false;
                BtnPause.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.StackTrace);
                MessageBox.Show("无法开始任务！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnChap_Click(object sender, RoutedEventArgs e)
        {
            BtnChap.IsEnabled = false;
            tm.UpdateChapterStatus();
            BtnChap.IsEnabled = true;
        }

        private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            TaskDetail item = TaskList.SelectedItem as TaskDetail;

            if (item == null)
            {
                MessageBox.Show("请点击一个任务开始操作", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!tm.MoveTaskUp(item))
            {
                MessageBox.Show("无法上移任务！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            TaskDetail item = TaskList.SelectedItem as TaskDetail;

            if (item == null)
            {
                MessageBox.Show("请点击一个任务开始操作", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!tm.MoveTaskDown(item))
            {
                MessageBox.Show("无法下移任务！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnMoveTop_Click(object sender, RoutedEventArgs e)
        {
            TaskDetail item = TaskList.SelectedItem as TaskDetail;
            if (item == null)
            {
                MessageBox.Show("请点击一个任务开始操作", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            switch (tm.MoveTaskTop(item))
            {
                case TaskManager.MoveTaskTopResult.OK:
                    break;
                case TaskManager.MoveTaskTopResult.Already:
                    MessageBox.Show("任务早已在队列顶部！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case TaskManager.MoveTaskTopResult.Failure:
                    MessageBox.Show("无法置顶任务！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                default:
                    return;
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            TaskDetail item = TaskList.SelectedItem as TaskDetail;

            if (item == null)
            {
                MessageBox.Show("请点击一个任务开始操作", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!tm.DeleteTask(item))
            {
                MessageBox.Show("无法删除任务！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            int activeTaskCount = tm.GetActiveTaskCount();
            BtnRun.IsEnabled = activeTaskCount > 0;
            BtnDelete.IsEnabled = activeTaskCount > 0;
            BtnEmpty.IsEnabled = activeTaskCount > 0;
            BtnMoveDown.IsEnabled = activeTaskCount > 1;
            BtnMoveUp.IsEnabled = activeTaskCount > 1;
            BtnMoveTop.IsEnabled = activeTaskCount > 2;
            BtnChap.IsEnabled = activeTaskCount > 0;
        }

        private void BtnEmpty_Click(object sender, RoutedEventArgs e)
        {
            tm.taskStatus.Clear();
            BtnRun.IsEnabled = false;
            BtnDelete.IsEnabled = false;
            BtnEmpty.IsEnabled = false;
            BtnMoveDown.IsEnabled = false;
            BtnMoveUp.IsEnabled = false;
            BtnMoveTop.IsEnabled = false;
            BtnChap.IsEnabled = false;
        }

        private void BtnNewWorker_Click(object sender, RoutedEventArgs e)
        {
            WorkerCount++;
            wm.AddWorker("工作单元-" + WorkerCount.ToString());
            WorkerNumber.Text = "工作单元：" + WorkerCount.ToString();
        }

        private void BtnDeleteWorker_Click(object sender, RoutedEventArgs e)
        {
            if (WorkerCount == 1)
            {
                MessageBox.Show("工作单元删除失败！", "只有一个工作单元了", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (wm.DeleteWorker("工作单元-" + WorkerCount.ToString()))
            {
                WorkerCount--;
                WorkerNumber.Text = "工作单元：" + WorkerCount.ToString();
            }
            else
            {
                MessageBox.Show("工作单元删除失败！", "全部被占用中", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AfterFinish_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (wm == null)
            {
                return;
            }

            string cmd = (AfterFinish.SelectedItem as ComboBoxItem).Content as string;

            switch (cmd)
            {
                case "关机":
                    wm.AfterFinish = (MainWindow mainWindow) =>
                    {
                        Process.Start("cmd.exe", "/c shutdown -s -t 300");
                        mainWindow.BtnCancelShutdown.IsEnabled = true;
                    };
                    break;

                default:
                    wm.AfterFinish = null;
                    break;
            }
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (wm.IsRunning)
            {
                string msg = "有任务正在运行，确定退出？点击No取消。";
                MessageBoxResult result =
                  MessageBox.Show(
                    msg,
                    "没手滑吧？",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    SubProcessService.KillAll();
                }
            }
        }

        private void BtnCancelShutdown_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("cmd.exe", "/c shutdown -a");
            BtnCancelShutdown.IsEnabled = false;
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            Window config = new ConfigPanel();
            config.ShowDialog();
        }

        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TaskDetail item = TaskList.SelectedItem as TaskDetail;

            if (item == null)
            {
                MessageBox.Show("你需要选择一个任务来打开文件。", "文件夹打开失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                string path = Path.GetDirectoryName(item.InputFile);
                string arg = path;

                if (item.CurrentStatus == "完成")
                {
                    arg = @"/select," + Path.Combine(path, item.OutputFile);
                }
                Process.Start("Explorer.exe", arg);
            }
        }

        private void TxtFreeMemory_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9]+");

            e.Handled = re.IsMatch(e.Text);
        }

    }
}
