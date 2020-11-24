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

namespace WykladDoPDF
{
    /// <summary>
    /// Logika interakcji dla klasy SavingProgressWindow.xaml
    /// </summary>
    public partial class SavingProgressWindow : Window
    {
        public SavingProgressWindow()
        {
            InitializeComponent();
        }

        public void SetValue(float value)
        {
            SetValue((int)value);
        }

        public void SetValue(int value)
        {
            if(!SavingProgressBar.Dispatcher.CheckAccess())
            {
                SavingProgressBar.Dispatcher.Invoke(delegate { _SetValue(value); });
                return;
            }

            _SetValue(value);
        }

        void _SetValue(int value)
        {
            SavingProgressBar.Value = value;
            SavingProgressText.Text = value.ToString() + "%";
        }
    }
}
