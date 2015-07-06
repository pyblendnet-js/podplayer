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

/// <summary>
/// Interaction logic for Window3.xaml
/// </summary>
public partial class keySelectWindow : Window
{
    public KeyActionClass keyAction;
    Boolean fixSource = false;

    public keySelectWindow(KeyActionClass ka, Boolean editable = true)
    {
        keyAction = ka;
        InitializeComponent();
        foreach (String act in keyAction.actionList)
        {
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            Label lbl = new Label();
            lbl.Content = act;
            lbl.Width = actionLbl.Width;
            sp.Children.Add(lbl);
            if (editable)
            {
                ComboBox cb = new ComboBox();
                cb.Width = keyLbl.Width;
                cb.SelectionChanged += actionKeyChanged;
                loadKeyItems(cb);
                setSelected(cb, act);
                sp.Children.Add(cb);
            }
            else
            {
                lbl = new Label();
                String k = keyAction.getKey(act);
                if (k == null)
                    lbl.Content = "Not set";
                else
                    lbl.Content = act;
                lbl.Width = actionLbl.Width;
                sp.Children.Add(lbl);
            }
            keyStackPanel.Children.Add(sp);
        }
        fixSource = true;
        if (!editable)
        //    buttonStack.Visibility = Visibility.Visible;
        //else
        {
            buttonStack.Visibility = Visibility.Hidden;
            buttonStack.Height = 0;
        }
    }

    void setSelected(ComboBox cb, String act)
    {
        fixSource = false;
        cb.SelectedItem = keyAction.getKey(act);
        fixSource = true;
    }

    void loadKeyItems(ComboBox cb)
    {
        foreach (String k in keyAction.keyList)
        {
            cb.Items.Add(k);
        }
    }

    void actionKeyChanged(Object ob, SelectionChangedEventArgs e)
    {
        if (!fixSource)  //dont want changes triggering this at startup
            return;
        //simpler just to set em all
        foreach (StackPanel sp in keyStackPanel.Children)
        {
            if (((ComboBox)sp.Children[1]).SelectedItem != null)
            {
                String act = ((Label)sp.Children[0]).Content.ToString();
                String key = ((ComboBox)sp.Children[1]).SelectedItem.ToString();
                keyAction.setKeyAction(act, key);
            }
        }
    }

    void saveActionKeys(Object ob, RoutedEventArgs e)
    {
        keyAction.saveActionKeys(keyAction.keyConfigFid);
    }

    void reloadActionKeys(Object ob, RoutedEventArgs e)
    {
        keyAction.loadActionKeys(keyAction.keyConfigFid);
        foreach (StackPanel sp in keyStackPanel.Children)
        {
            setSelected((ComboBox)(sp.Children[1]), ((Label)sp.Children[0]).Content.ToString());
        }
    }

}

