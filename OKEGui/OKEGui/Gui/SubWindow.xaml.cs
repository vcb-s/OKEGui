using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace OKEGui
{
    public class GoblaData
    {
    }

    /// <summary>
    /// SubWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SubWindow : Window
    {
        private class TaskInfo : INotifyPropertyChanged
        {
            public TaskInfo()
            {
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                if (PropertyChanged != null) {
                    PropertyChanged(this, e);
                }
            }
        }

        private JobDetails info;

        private JobDetails oldInfo;

        private bool isAsked;

        public static T DeepCopy<T>(T obj)
        {
            //如果是字符串或值类型则直接返回
            if (obj is string || obj.GetType().IsValueType) return obj;

            object retval = Activator.CreateInstance(obj.GetType());
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (FieldInfo field in fields) {
                try { field.SetValue(retval, DeepCopy(field.GetValue(obj))); } catch { }
            }
            return (T)retval;
        }

        public SubWindow(JobDetails task)
        {
            InitializeComponent();
            oldInfo = task;

            info = DeepCopy<JobDetails>(task);
            this.DataContext = info;

            ContainerFormat.Text = info.ContainerFormat;
            VideoFormat.Text = info.VideoFormat;
            AudioFormat.Text = info.AudioFormat;

            InputFileList.Items.Add(info.InputFile);

            isAsked = false;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("是否放弃更改并返回？", "任务详细", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes) {
                isAsked = true;
                this.Close();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 保存更改
            oldInfo = info;

            oldInfo.ContainerFormat = ContainerFormat.Text == "不封装" ? "" : ContainerFormat.Text;
            oldInfo.VideoFormat = VideoFormat.Text;
            oldInfo.AudioFormat = AudioFormat.Text;

            // 更新输出文件拓展名
            if (!oldInfo.UpdateOutputFileName()) {
                System.Windows.MessageBox.Show("格式错误！", "任务详细", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            isAsked = true;
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!isAsked) {
                MessageBoxResult result = System.Windows.MessageBox.Show("是否放弃更改并返回？", "任务详细", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No) {
                    e.Cancel = true;
                }
            }

            // 直接退出
        }

        public JobDetails GetNewTaskDetail()
        {
            return oldInfo;
        }

        private void InputScriptBtn_Click(object sender, RoutedEventArgs e)
        {
            Process editor = new Process();
            editor.StartInfo.FileName = @"c:\Windows\System32\notepad.exe";
            editor.StartInfo.Arguments = info.InputScript;
            editor.Start();
        }
    }
}
