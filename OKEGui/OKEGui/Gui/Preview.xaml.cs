using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OKEGui
{
    /// <summary>
    /// Preview.xaml 的交互逻辑
    /// </summary>
    public partial class Preview : Window
    {
        public Preview()
        {
            InitializeComponent();
        }

        public void ShowBitmapFromMemory(int width, int height, byte[] buf)
        {
            // 设置窗口大小
            this.Width = width;
            this.Height = height;

            // 设置控件大小
            image.Width = width;
            image.Height = height;

            //开始加载图像
            BitmapImage bim = new BitmapImage();
            bim.BeginInit();
            bim.StreamSource = new MemoryStream(buf);
            bim.EndInit();
            image.Source = bim;
            GC.Collect(); //强制回收资源
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            string path = @"c:\tmp\demo.bmp";
            using (BinaryReader loader = new BinaryReader(File.Open(path, FileMode.Open))) {
                FileInfo fd = new FileInfo(path);
                int length = (int)fd.Length;
                byte[] buf = new byte[length];
                buf = loader.ReadBytes((int)fd.Length);
                loader.Dispose();
                loader.Close();

                ShowBitmapFromMemory(2560, 1440, buf);
            }
        }
    }
}
