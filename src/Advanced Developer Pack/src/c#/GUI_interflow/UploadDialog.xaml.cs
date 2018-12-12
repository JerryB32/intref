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

namespace GUI_interflow
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class UploadWindow : Window
    {

        public bool Submit = false;

        public string OneIndicatorJSON
        {
            get
            {
                if (OneIndicatorContent.Text == null) return string.Empty;
                return OneIndicatorContent.Text;
            }
        }

        public UploadWindow()
        {
            InitializeComponent();
        }

        private void Button_Upload_Submit_Click(object sender, RoutedEventArgs e)
        {
            Close();
            this.Submit = true;
        }

        private void Button_Import_file_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.Multiselect = false;

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // open document
                string filename = dlg.FileName;
                OneIndicatorContent.Text = System.IO.File.ReadAllText(filename);
            }
        }
    }
}
