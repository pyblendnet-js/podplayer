using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PodPlayer
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        Window1 callWindow;
        MainWindow playWindow;

        public Window2(Window1 w, MainWindow mw)
        {
            callWindow = w;
            playWindow = mw;
            InitializeComponent();
            foreach (string s in playWindow.podPlayList)
            {
                if (!playWindow.podPlayList.Contains(s))
                    continue;
                String st = playWindow.getPodStats(s);
                if (st == "Gone" || st.StartsWith("NeverHeard"))
                    continue;
                listBox1.Items.Add(st);
            }
        }

        private void deletePods(Object obj, RoutedEventArgs e)
        {
            foreach (var item in listBox1.SelectedItems)
            {

            }
        }
    }
}
