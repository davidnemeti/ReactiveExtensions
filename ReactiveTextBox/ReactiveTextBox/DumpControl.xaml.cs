using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ReactiveTextBox
{
    /// <summary>
    /// Interaction logic for DumpControl.xaml
    /// </summary>
    public partial class DumpControl : UserControl
    {
        public string DumpTitle
        {
            get => TitleBlock.Text;
            set => TitleBlock.Text = value;
        }

        public ObservableCollection<string> DumpItems
        {
            get => (ObservableCollection<string>) ItemList.ItemsSource;
            set => ItemList.ItemsSource = value;
        }

        public DumpControl()
        {
            InitializeComponent();
        }
    }
}
