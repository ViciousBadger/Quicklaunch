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

namespace Quicklaunch
{
    /// <summary>
    /// Interaction logic for RenameTag.xaml
    /// </summary>
    public partial class RenameTag : Window
    {
        public bool Cancelled { get; set; }
        public string NewTagName { get; set; }

        public RenameTag()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            NewTagName = tagNameBox.Text;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Cancelled = true;
            Close();
        }
    }
}