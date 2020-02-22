using Microsoft.Win32;
using OKEGui.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OKEGui
{
    /// <summary>
    /// Config.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigPanel : Window
    {
        public OKEGuiConfig Config { get; }

        private void Vspipe_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "vspipe.exe (vspipe.exe)|vspipe.exe",
                InitialDirectory = Config.vspipePath
            };
            bool result = ofd.ShowDialog().GetValueOrDefault(false);
            if (result)
            {
                Config.vspipePath = ofd.FileName;
            }
        }

        private void RPChecker_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "RPChecker.exe (RPChecker*.exe)|RPChecker*.exe",
                InitialDirectory = Config.rpCheckerPath
            };
            bool result = ofd.ShowDialog().GetValueOrDefault(false);
            if (result)
            {
                Config.rpCheckerPath = ofd.FileName;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Initializer.Config = Config;
            Initializer.WriteConfig();
            Close();
        }

        public ConfigPanel()
        {
            Config = Initializer.Config.Clone() as OKEGuiConfig;
            InitializeComponent();
        }
    }
}
