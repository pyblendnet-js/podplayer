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
    /// Interaction logic for Window3.xaml
    /// </summary>
    public partial class Window3 : Window
    {
        Window1 callWindow;
        MainWindow playWindow;
        Boolean fixSource = false;

        public Window3(Window1 w, MainWindow mw)
        {
            callWindow = w;
            playWindow = mw;
            InitializeComponent();
            foreach(String act in playWindow.actionList)
            { 
                StackPanel sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;
                Label lbl = new Label();
                lbl.Content = act;
                lbl.Width = actionLbl.Width;
                sp.Children.Add(lbl);
                ComboBox cb = new ComboBox();
                cb.Width = keyLbl.Width;
                cb.SelectionChanged += actionKeyChanged;
                loadKeyItems(cb);
                setSelected(cb,act);
                sp.Children.Add(cb);
                keyStackPanel.Children.Add(sp);
            }
            fixSource = true;
        }

        void setSelected(ComboBox cb, String act)
        {
            fixSource = false;
            if (playWindow.keyActions.Values.Contains(act))
            {
                int ki = playWindow.keyActions.Values.ToList().IndexOf(act);
                String ak = playWindow.keyActions.Keys.ToList()[ki];
                cb.SelectedItem = ak;
            }
            else
            {
                cb.SelectedItem = null;
            }
            fixSource = true;
        }

        void loadKeyItems(ComboBox cb)
        {
            foreach (String k in playWindow.keyList)
            {
                cb.Items.Add(k);
            }
        }

        void actionKeyChanged(Object ob, SelectionChangedEventArgs e)
        {
            if (!fixSource)  //dont want changes triggering this at startup
                return;
            //simpler just to set em all
            foreach(StackPanel sp in keyStackPanel.Children)
            {
                if (((ComboBox)sp.Children[1]).SelectedItem != null)
                {
                    String act = ((Label)sp.Children[0]).Content.ToString();
                    String key = ((ComboBox)sp.Children[1]).SelectedItem.ToString();
                    playWindow.setKeyAction(act, key);
                }
            }
        }

        void saveActionKeys(Object ob, RoutedEventArgs e)
        {
            playWindow.saveActionKeys(playWindow.keyConfigFid);
        }

        void reloadActionKeys(Object ob, RoutedEventArgs e)
        {
            playWindow.loadActionKeys(playWindow.keyConfigFid);
            foreach (StackPanel sp in keyStackPanel.Children)
            {
                setSelected((ComboBox)(sp.Children[1]), ((Label)sp.Children[0]).Content.ToString());
            }
         }

    }
}
