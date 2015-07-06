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
using System.IO;
using System.Windows.Threading;

namespace PodPlayer
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        Window1 callWindow;
        MainWindow playWindow;
        DispatcherTimer dispatcherTimer;
        string sampleToPlay = null;

        public Window2(Window1 w, MainWindow mw)
        {
            callWindow = w;
            playWindow = mw;
            InitializeComponent();
            refresh();
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(timerTick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);   // 1second
            dispatcherTimer.Start();
        }

        private void refresh()
        {
            listBox1.Items.Clear();
            foreach (string s in playWindow.podPlayList)
            {
                if (!File.Exists(s))
                    continue;
                String st = playWindow.getPodStats(s);
                if (st == "Gone" || st.StartsWith("NeverHeard"))
                    continue;
                ListBoxItem it = new ListBoxItem();
                it.Content = st;
                it.MouseEnter += new MouseEventHandler(it_MouseEnter);
                it.MouseLeave += new MouseEventHandler(it_MouseLeave);
                int count = int.Parse(st.Split()[0].Split("="[0])[1]);
                int i = 0;
                foreach (ListBoxItem hit in listBox1.Items)
                {
                    string hs = (string)hit.Content;
                    int icount = int.Parse(hs.Split()[0].Split("="[0])[1]);
                    if (count > icount)
                    {

                        listBox1.Items.Insert(i, it);
                        break;
                    }
                    i++;
                }
                if (i >= listBox1.Items.Count)
                    listBox1.Items.Add(it);
            }
        }

        private void keyPressed(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.S:
                    checkBox1.IsChecked = !checkBox1.IsChecked;
                    break;
                case Key.P:
                    playPods(null, null);
                    break;
                case Key.D:
                    deletePods(null, null);
                    break;
            }
        }

        private void it_MouseEnter(Object obj, MouseEventArgs e)
        {
            if (!checkBox1.IsChecked.Value)
                return;
            string st = (string)((ListBoxItem)obj).Content;
            int i = st.IndexOf("Name=");
            string name = st.Substring(i + 5);
            sampleToPlay = name;
            dispatcherTimer.Start();
        }

        private void timerTick(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            playWindow.sampleNamed(sampleToPlay);
        }

        private void it_MouseLeave(Object obj, MouseEventArgs e)
        {
            dispatcherTimer.Stop();
            playWindow.stopSample();
        }

        private void playPods(Object obj, RoutedEventArgs e)
        {
            playWindow.clearPlayList();
            foreach (ListBoxItem item in listBox1.SelectedItems)
            {
                string st = (string)item.Content;
                int i = st.IndexOf("Name=");
                string name = st.Substring(i + 5);
                playWindow.addNamed(name);
            }
            playWindow.playList();
            listBox1.SelectedItems.Clear();
        }

        private void deletePods(Object obj, RoutedEventArgs e)
        {
            foreach (ListBoxItem item in listBox1.SelectedItems)
            {
                string st = (string)item.Content;
                int i = st.IndexOf("Name=");
                string name = st.Substring(i + 5);
                playWindow.deletePodCast(name);
            }
            refresh();
        }
    }
}
