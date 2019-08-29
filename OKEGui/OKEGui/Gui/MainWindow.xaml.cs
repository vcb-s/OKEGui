using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using OKEGui.Worker;

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

        public int WorkerCount = 0;
        public TaskManager tm = new TaskManager();
        public WorkerManager wm;

        public MainWindow()
        {
            InitializeComponent();

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            Title += " v" + version;

            listView1.ItemsSource = tm.taskStatus;

            wm = new WorkerManager(tm);

            BtnMoveDown.IsEnabled = false;
            BtnMoveup.IsEnabled = false;
            BtnStop.IsEnabled = false;
            BtnEdit.IsEnabled = false;

            // 初始的worker数量等于Numa数量。
            int numaCount = NumaNode.NumaCount;
            for (int i = 0; i < numaCount; i++)
            {
                WorkerCount++;
                wm.AddWorker("工作单元-" + WorkerCount.ToString());
            }
            WorkerNumber.Text = "工作单元：" + WorkerCount.ToString();
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            // 新建任务。具体实现请见Gui/wizardWindow
            try
            {
                var wizard = new WizardWindow(ref tm);
                wizard.ShowDialog();
                BtnRun.IsEnabled = true;
                tm.IsCanStart = true;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.StackTrace);
                Environment.Exit(0);
            }

        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            // TODO
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tm.IsCanStart = true;

                if (!wm.Start())
                {
                    tm.IsCanStart = false;
                    MessageBox.Show("无法开始任务！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                BtnDeleteWorker.IsEnabled = false;
                BtnEmpty.IsEnabled = false;
                BtnRun.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.StackTrace);
                Environment.Exit(0);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            // TODO
        }

        private void BtnMoveup_Click(object sender, RoutedEventArgs e)
        {
            // TODO
        }

        private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            // TODO
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            object o = listView1.SelectedItem;
            if (o == null)
            {
                return;
            }

            TaskDetail item = o as TaskDetail;
            if (item.IsRunning)
            {
                MessageBox.Show("无法删除该任务！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!tm.DeleteTask(item))
            {
                MessageBox.Show("任务删除失败！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        private void BtnEmpty_Click(object sender, RoutedEventArgs e)
        {
            if (!tm.IsCanStart)
            {
                tm.taskStatus.Clear();
            }
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
                    wm.AfterFinish = () => Process.Start("cmd.exe", "/c shutdown -s -t 300");
                    break;

                default:
                    wm.AfterFinish = null;
                    break;
            }
        }
    }
}
