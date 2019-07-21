using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OKEGui
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public int WorkerCount = 0;
        public TaskManager tm = new TaskManager();
        public WorkerManager wm;

        public MainWindow()
        {
            InitializeComponent();

            listView1.ItemsSource = tm.taskStatus;

            wm = new WorkerManager(tm);

            BtnMoveDown.IsEnabled = false;
            BtnMoveup.IsEnabled = false;

            WorkerCount++;
            wm.AddWorker("工作单元-" + WorkerCount.ToString());
            this.WorkerNumber.Text = "工作单元：" + WorkerCount.ToString();
        }

        private void _btnTest_Click(object sender, RoutedEventArgs e)
        {
            // ClickEvent();
            tm.AddTask();
        }

        private void start()
        {
            //for (int i = 1; i <= 1000; i++) {
            //    tm.taskStatus[0].ProgressValue = (double)i / 10;
            //    Thread.Sleep(10);
            //}
        }

        // 测试用函数
        private void _btnRun_Click(object sender, RoutedEventArgs e)
        {
            //Thread thread = new Thread(start);
            //thread.Start();
        }

        private void listView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            object o = listView1.SelectedItem;
            if (o == null) {
                return;
            }

            TaskDetail item = o as TaskDetail;
            SubWindow subWin = new SubWindow(item);
            subWin.ShowDialog();

            if (!tm.UpdateTask(subWin.GetNewTaskDetail())) {
                System.Windows.MessageBox.Show("任务更新失败！", "任务详细", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        private void listView1_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (IsSingleFile(e) != null) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Handled = true;
            }
        }

        private void listView1_PreviewDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            string fileName = IsSingleFile(e);
            if (fileName == null) {
                return;
            }
            // TODO
        }

        private string IsSingleFile(DragEventArgs args)
        {
            // Check for files in the hovering data object.
            if (args.Data.GetDataPresent(DataFormats.FileDrop, true)) {
                string[] fileNames = args.Data.GetData(DataFormats.FileDrop, true) as string[];
                // Check fo a single file or folder.
                if (fileNames.Length == 1) {
                    // Check for a file (a directory will return false).
                    if (File.Exists(fileNames[0])) {
                        // At this point we know there is a single file.
                        return fileNames[0];
                    }
                }
            }
            return null;
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            var wizard = new WizardWindow(ref tm);
            wizard.ShowDialog();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            // TODO
            tm.isCanStart = false;
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (tm.isCanStart) {
                return;
            }

            if (wm.GetWorkerCount() == 0) {
                WorkerCount++;
                wm.AddWorker("工作单元-" + WorkerCount.ToString());
                this.WorkerNumber.Text = "工作单元：" + WorkerCount.ToString();
            }

            tm.isCanStart = true;

            if (!wm.Start()) {
                tm.isCanStart = false;
                MessageBox.Show("无法开始任务！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            BtnDeleteWorker.IsEnabled = false;
            BtnEmpty.IsEnabled = false;
            BtnRun.IsEnabled = false;

            //VideoJob vj = new VideoJob();
            //tm.AddTask();
            //vj.config = tm.taskStatus[0];
            //JobWorker worker = new JobWorker(vj);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            object o = listView1.SelectedItem;
            if (o == null) {
                return;
            }

            TaskDetail item = o as TaskDetail;

            if (item.IsRunning) {
                System.Windows.MessageBox.Show("无法编辑该任务！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SubWindow subWin = new SubWindow(item);
            subWin.ShowDialog();

            if (!tm.UpdateTask(subWin.GetNewTaskDetail())) {
                System.Windows.MessageBox.Show("任务更新失败！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
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
            if (o == null) {
                return;
            }

            TaskDetail item = o as TaskDetail;
            if (item.IsRunning) {
                System.Windows.MessageBox.Show("无法删除该任务！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!tm.DeleteTask(item)) {
                System.Windows.MessageBox.Show("任务删除失败！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        private void BtnEmpty_Click(object sender, RoutedEventArgs e)
        {
            if (!tm.isCanStart) {
                tm.taskStatus.Clear();
            }
        }

        private void BtnNewWorker_Click(object sender, RoutedEventArgs e)
        {
            // wm.AddTempWorker();
            WorkerCount++;
            wm.AddWorker("工作单元-" + WorkerCount.ToString());
            this.WorkerNumber.Text = "工作单元：" + WorkerCount.ToString();
        }

        private void BtnDeleteWorker_Click(object sender, RoutedEventArgs e)
        {
            if (wm.DeleteWorker("工作单元-" + WorkerCount.ToString())) {
                WorkerCount--;
                this.WorkerNumber.Text = "工作单元：" + WorkerCount.ToString();
            } else {
                System.Windows.MessageBox.Show("工作单元删除失败！", "OKEGui", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (wm == null) {
                return;
            }

            string cmd = (comboBox.SelectedItem as ComboBoxItem).Content as string;

            switch (cmd) {
                case "关机":
                    wm.AfterFinish = () => System.Diagnostics.Process.Start("cmd.exe", "/c shutdown -s -t 300");
                    break;

                default:
                    wm.AfterFinish = null;
                    break;
            }
        }
    }
}
