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
using System.Windows.Forms;
using System.ComponentModel; // CancelEventArgs


namespace PodPlayer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        MainWindow playWindow;
        private Window2 heardWindow;

        public Window1(MainWindow parent)
        {
            playWindow = parent;
            InitializeComponent();
            songListPathTextBox.Text = @"C:\Users\home\Music\Playlists\wakeupSongs.wpl";
            podPathTextBox.Text = @"C:\Users\home\Documents\My Received Podcasts";
            loadConfig();
        }

        private void windowClosing(Object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
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

        void findFile(Object obj, System.Windows.RoutedEventArgs e)
        {
            findFile();
        }
        void findFile(Object obj, System.Windows.Input.MouseEventArgs e)
        {
            findFile();
        }
        void findFile()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(songListPathTextBox.Text);
            DialogResult result = fileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) // Test result.
            {
                songListPathTextBox.Text = fileDialog.FileName;
            }
            Console.WriteLine(result);
        }

        void findPath(Object obj, System.Windows.RoutedEventArgs e)
        {
            findPath();
        }
        void findPath(Object obj, System.Windows.Input.MouseEventArgs e)
        {
            findPath();
        }

        void findPath()
        {
            FolderBrowserDialog pathDialog = new FolderBrowserDialog();
            pathDialog.SelectedPath = podPathTextBox.Text;
            DialogResult result = pathDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) // Test result.
            {
                podPathTextBox.Text = pathDialog.SelectedPath;
                Console.WriteLine(result);
            }
        }

        void deleteDelete(Object obj, RoutedEventArgs e)
        {
            playWindow.deleteDelete();
        }

        void resaveHeard(Object obj, RoutedEventArgs e)
        {
            playWindow.savePodsHeard();
        }

        void reviewPodsHeard(Object obj, RoutedEventArgs e)
        {
            heardWindow = new Window2(this, playWindow);
            heardWindow.Show();
        }

    }
}
