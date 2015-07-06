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

namespace PodPlayer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            songListPathTextBox.Text = @"C:\Users\home\Music\Playlists\wakeupSongs.wpl";
            podPathTextBox.Text = @"C:\Users\home\Documents\My Received Podcasts";
            loadConfig();
        }


        private void loadConfig()
        {
            if (!File.Exists("podPlayer.cfg"))
                return;  //not an error
            try
            {
                // Read and show each line from the file. 
                string cfl = "";
                using (StreamReader sr = new StreamReader("podPlayer.cfg"))
                {
                    while ((cfl = sr.ReadLine()) != null)
                    {
                        String[] vs = cfl.Split();
                        if (vs.Length != 2)
                            System.Windows.MessageBox.Show("ERROR Config line:" + cfl);
                        try
                        {
                            switch (vs[0])
                            {
                                case "alt_song":
                                    altMusicCheckBox.IsChecked = vs[1].ToLower().Equals("true");
                                    break;
                                case "fade_in":
                                    fadeInSpeedTextBox.Text = vs[1];
                                    break;
                                case "pod_path":
                                    podPathTextBox.Text = vs[1];
                                    break;
                                case "songlist_path":
                                    songListPathTextBox.Text = vs[1];
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            System.Windows.MessageBox.Show("ERROR Config line:" + cfl + " has exception " + e.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("ERROR loading pods heard" + e.ToString());
            }
        }

        public void saveConfig()
        {
            try
            {
                // Write each directory name to a file. 
                using (StreamWriter sw = new StreamWriter("podPlayer.cfg", false))  //do not append
                {
                    sw.WriteLine("alt_song " + altMusicCheckBox.IsChecked.ToString());
                    sw.WriteLine("fade_in " + fadeInSpeedTextBox.Text);
                    sw.WriteLine("pod_path " + podPathTextBox.Text);
                    sw.WriteLine("songlist_path" + songListPathTextBox.Text);
                }
            }
            catch (Exception ec)
            {
                System.Windows.MessageBox.Show("ERROR Saving pods heard" + ec.ToString());
            }
        }

       

        private void saveConfig(Object obj, RoutedEventArgs e)
        {
            saveConfig();
        }
    }
}
