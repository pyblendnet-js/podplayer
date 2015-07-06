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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using System.Windows.Interop;

namespace PodPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer updateTimer;
        private WMPLib.WindowsMediaPlayer wplayer;
        private DateTime startTime;
        private List<string> wakeupSongs;
        private List<string> podsHeard;
        private char[] podsHeardSeperator = ",".ToArray<char>();
        private List<string> podPlayList;
        private bool fixLayoutPending;
        private bool podMode;
        private string lastMediaURL;
        private Random rand;
        const int MAX_REPEATS = 3;
        private bool lastMediaHeard;
        private int volume;

        private Window1 configWindow;

        public MainWindow()
        {
            InitializeComponent();

            configWindow = new Window1();
            loadConfig();

            rand = new Random(DateTime.Now.Millisecond);
            fullScreen(true);
            loadSongs();
            loadPods();
            podMode = false;
            lastMediaHeard = false;
            volume = 100;

            wplayer = new WMPLib.WindowsMediaPlayer();
            wplayer.settings.volume = 20;
            if (wakeupSongs.Count > 0)
            {
                int ni = rand.Next(wakeupSongs.Count);
                wplayer.URL = wakeupSongs[ni];
            } 
            else
                wplayer.URL = "sample.mp3";

            //WMPLib.IWMPPlaylist pl = wplayer.playlistCollection.newPlaylist("todays");
            //pl.appendItem(WMPLib.
            //String nm = wplayer.currentPlaylist.name;
            queueMedia();

            //  updateTimer setup
            updateTimer = new System.Windows.Threading.DispatcherTimer();
            updateTimer.Tick += new EventHandler(updateTick);
            updateTimer.Interval = new TimeSpan(0, 0, 1);
            updateTimer.Start();
        }

        private void queueMedia()
        {
            wplayer.controls.stop();
            wplayer.currentPlaylist.clear();
            if (wakeupSongs.Count > 0)
            {
                int ni = rand.Next(wakeupSongs.Count);
                wplayer.URL = wakeupSongs[ni];
            }
            else
                wplayer.URL = "sample.mp3";
            foreach (String p in podPlayList)
            {
                wplayer.currentPlaylist.appendItem(wplayer.newMedia(p));
                if ((wakeupSongs.Count > 0) && (bool)configWindow.altMusicCheckBox.IsChecked)
                {
                    int ni = rand.Next(wakeupSongs.Count);
                    String url = wakeupSongs[ni];
                    wplayer.currentPlaylist.appendItem(wplayer.newMedia(url));
                }
            }
            wplayer.controls.play();
            startTime = DateTime.Now;
        }


        private void fullScreen(bool state)
        {
            if (state)
            {
                this.WindowState = WindowState.Maximized;
                this.WindowStyle = WindowStyle.None;
            }
            else
            {
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
            }
            fixLayoutPending = true;
        }

        private void showConfig(Object obj, RoutedEventArgs e)
        {
            configWindow.Show();
        }

        private void windowLoaded(object sender, RoutedEventArgs e)
        {
            //this.KeyDown += new KeyEventHandler(keyPressed);
            fixLayout();
        //    IntPtr wh = new WindowInteropHelper(this).Handle;
        //    SendMessage(wh, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
            this.Focus();
        }

        private void mouseDown(object sender, MouseButtonEventArgs e)
        {
            togglePlay();
        }

        private void togglePlay()
        {
            if (wplayer.playState == WMPLib.WMPPlayState.wmppsPaused)
                wplayer.controls.play();
            else if (wplayer.playState == WMPLib.WMPPlayState.wmppsPlaying)
                wplayer.controls.pause();
        }

        private void keyPressed(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    wplayer.controls.stop();
                    configWindow.Close();
                    this.Close();
                    break;
                case Key.Space:
                    togglePlay();
                    break;
                case Key.W:
                    fullScreen(this.WindowState == WindowState.Normal);
                    break;
                case Key.Tab:
                    wplayer.controls.currentPosition += wplayer.currentMedia.duration / 10;
                    wplayer.controls.play();
                    break;
                case Key.Left:
                    wplayer.controls.currentPosition = 0;
                    wplayer.controls.play();
                    break;
                case Key.Right:
                    recordPodsHeard(wplayer.currentMedia.sourceURL,"SKIPPED_AT=" + wplayer.controls.currentPosition.ToString("F0") + "Sec");
                    wplayer.controls.next();
                    podMode = false;
                    break;
                case Key.Delete:
                    if (podMode)
                    {
                        recordPodsHeard(wplayer.currentMedia.sourceURL,"DELETE");
                        wplayer.controls.next();
                    }
                    break;
                case Key.H:  // just to test the heard function
                    if (podMode)
                    {
                        lastMediaHeard = true;
                        wplayer.controls.next();
                    }
                    break;
                case Key.Up:
                    if (volume < 50)
                        volume += 1;
                    else if (volume < 100)
                        volume += 5;
                    wplayer.settings.volume = volume;
                    volLbl.Content = "Volume = " + volume;
                    volLbl.Opacity = 1.0;
                    break;
                case Key.Down:
                    if (volume > 50)
                        volume -= 5;
                    else if (volume > 10)
                        volume -= 1;
                    if (wplayer.settings.volume > volume)
                        wplayer.settings.volume = volume;
                    volLbl.Content = "Volume = " + wplayer.settings.volume;
                    volLbl.Opacity = 1.0;
                    break;
            }
        }

        private void fixLayout()
        {
            statusLbl.Height = this.ActualHeight * 0.1;
            statusLbl.FontSize = statusLbl.Height * 0.6;
            progressRect.Margin = new Thickness(20, statusLbl.Height + 20, 20, 0);
            progressBkgRect.Margin = progressRect.Margin;
            remainderLbl.Margin = progressRect.Margin;
            progressRect.Height = this.ActualHeight * 0.1;
            progressBkgRect.Height = progressRect.Height;
            remainderLbl.Height = progressRect.Height;
            remainderLbl.FontSize = progressRect.Height * 0.8;
            lastHeardLabel.Margin = new Thickness(20, progressRect.Margin.Top + progressRect.Height, 0, 0);
            timesHeardLabel.Margin = new Thickness(20, progressRect.Margin.Top + progressRect.Height, 0, 0);
            volLbl.Height = this.ActualHeight * 0.1;
            volLbl.Margin = new Thickness(20, progressRect.Margin.Top + progressRect.Height + 20, 20, 0);
            volLbl.FontSize = volLbl.Height * 0.6;
            double y = statusLbl.Height + progressRect.Height + 20;
            clockDial.Height = this.ActualHeight - y;
            clockDial.Width = this.ActualWidth - 40;
            clockDial.FontSize = clockDial.Height * 0.6;
            nextMediaLbl.Height = this.ActualHeight * 0.06;
            nextMediaLbl.FontSize = nextMediaLbl.Height * 0.6;
            fixLayoutPending = false;
        }

        private void updateTick(object sender, EventArgs e)
        {
            if (fixLayoutPending)
                fixLayout();
            podMode = podPlayList.Contains(wplayer.currentMedia.sourceURL);
            //if (podMode && lastMediaHeard)  //have fini music it seems
            //{
            //    if (!wplayer.currentMedia.sourceURL.Equals(lastMediaURL) || wplayer.playState == WMPLib.WMPPlayState.wmppsMediaEnded)
            //    {
            //        recordPodsHeard(lastMediaURL);
            //    }
            //}
            if (!wplayer.currentMedia.sourceURL.Equals(lastMediaURL))  //lastMediaURL != wplayer.currentMedia.sourceURL)
            {
                if (lastMediaHeard)
                  recordPodsHeard(lastMediaURL);
                lastMediaURL = wplayer.currentMedia.sourceURL;
                if (wakeupSongs.Contains(lastMediaURL))
                    statusLbl.Foreground = Brushes.Green;
                else
                    statusLbl.Foreground = Brushes.Blue;
                string status_str = wplayer.playState.ToString().Substring(5) + ":" + wplayer.currentMedia.name + " = " + wplayer.currentMedia.duration.ToString("F0") + " Sec";
                statusLbl.Content = status_str;
                statusLbl.FontSize = statusLbl.ActualWidth / status_str.Length * 2;
                if (statusLbl.FontSize > statusLbl.ActualHeight * 0.6)
                    statusLbl.FontSize = statusLbl.ActualHeight * 0.6;
                // see how many times it has been heard etc
                String lastHeard = "Never heard";
                int heardCount = 0;
                foreach (string ph in podsHeard)
                {
                    String[] tn = ph.Split(podsHeardSeperator);
                    if (tn[0].Equals(lastMediaURL))
                    {
                        if (tn.Length > 1)
                        {
                            lastHeard = tn[1];
                        }
                        if(!tn[tn.Length-1].StartsWith("SKIPPED"))
                          heardCount++;
                    }
                }
                lastHeardLabel.Content = lastHeard;
                timesHeardLabel.Content = "Heard " + heardCount.ToString();
                status_str = "End of play list";  // assume end of list
                nextMediaLbl.Foreground = Brushes.Red;
                for (int i = 0; i < wplayer.currentPlaylist.count - 1; i++)
                {
                    if (wplayer.controls.currentItem.sourceURL == wplayer.currentPlaylist.get_Item(i).sourceURL)
                    {
                        status_str = wplayer.currentPlaylist.get_Item(i + 1).name;
                        if (wakeupSongs.Contains(wplayer.currentPlaylist.get_Item(i + 1).sourceURL))
                          nextMediaLbl.Foreground = Brushes.Green;
                        else
                          nextMediaLbl.Foreground = Brushes.Blue;
                    }
                }
                nextMediaLbl.Content = status_str;
                nextMediaLbl.FontSize = nextMediaLbl.ActualWidth / status_str.Length * 2;
                if (nextMediaLbl.FontSize > nextMediaLbl.ActualHeight * 0.6)
                    nextMediaLbl.FontSize = nextMediaLbl.ActualHeight * 0.6;
            }
            if (wplayer.settings.volume < volume)
            {
                int dv = 10;
                try
                {
                    dv = int.Parse(configWindow.fadeInSpeedTextBox.Text);
                }
                catch
                {
                }
                int v = wplayer.settings.volume + dv;
                if (v > 100 || v < 0)
                    wplayer.settings.volume = 50;
                else
                    wplayer.settings.volume = v;
                volLbl.Content = "Volume = " + wplayer.settings.volume.ToString();
                volLbl.Opacity = 1.0;
            }
            else if (volLbl.Opacity > 0.0)
            {
                volLbl.Opacity -= 0.1;
            }
            Double rem_sec = wplayer.currentMedia.duration - wplayer.controls.currentPosition;
            lastMediaHeard = (rem_sec < 2);
            remainderLbl.Content = rem_sec.ToString("F0") + " Sec";
            //double tm = (DateTime.Now - startTime).TotalSeconds;
            double tm = wplayer.controls.currentPosition;
            double progress = tm / wplayer.currentMedia.duration;
            progressRect.Width = progressBkgRect.ActualWidth * progress;
            String ts = DateTime.Now.ToString("HH:mm");
            clockDial.Content = ts;
        }

        private void loadSongs()
        {
            try
            {
                // Read and show each line from the file. 
                string line = "";
                using (StreamReader sr = new StreamReader(@"C:\Users\home\Music\Playlists\wakeupSongs.wpl"))
                {
                    wakeupSongs = new List<string>();
                    while ((line = sr.ReadLine()) != null)
                    {
                        int ms = line.IndexOf("<media src=");
                        if (ms < 0)
                            continue;
                        string fid = line.Split('"')[1];  //Substring(line.IndexOf("=")+2);
                        if (fid.Contains(".."))
                            fid = fid.Replace("..", @"C:\Users\home\Music");
                        if (File.Exists(fid))
                            wakeupSongs.Add(fid);
                    }

                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("ERROR loading wakeup songs" + e.ToString());
            }
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
                                    configWindow.altMusicCheckBox.IsChecked = vs[1].ToLower().Equals("true");
                                    break;
                                case "fade_in":
                                    configWindow.fadeInSpeedTextBox.Text = vs[1];
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            System.Windows.MessageBox.Show("ERROR Config line:" + cfl + " has exception "+ e.ToString());
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
                    sw.WriteLine("alt_song " + configWindow.altMusicCheckBox.IsChecked.ToString());
                    sw.WriteLine("fade_in " + configWindow.fadeInSpeedTextBox.Text);
                   }
            }
            catch (Exception ec)
            {
                System.Windows.MessageBox.Show("ERROR Saving pods heard" + ec.ToString());
            }
        }


        private void loadPodsHeard()
        {
            if (!File.Exists("podsHeard.txt"))
                return;  //not an error
            try
            {
                // Read and show each line from the file. 
                string ph = "";
                using (StreamReader sr = new StreamReader("podsHeard.txt"))
                {
                    podsHeard = new List<string>();
                    while ((ph = sr.ReadLine()) != null)
                    {
                        String[] tn = ph.Split(podsHeardSeperator);
                        if (File.Exists(tn[0]))
                            podsHeard.Add(ph);  // leave suffix
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("ERROR loading pods heard" + e.ToString());
            }
        }

        private void recordPodsHeard(string fid, string suffix = "")
        {
            try
            {
                // Write each directory name to a file. 
                using (StreamWriter sw = new StreamWriter("podsHeard.txt", true))  //append
                {
                    sw.WriteLine(fid + "," + DateTime.Now.ToString("yyyyMMdd_hhmmdd") + "," + suffix);
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("ERROR Saving pods heard" + e.ToString());
            }

        }

        private void savePodsHeard()
        {
            // this save will remove all the files that nolonger exist 
            try
            {
                // Write each directory name to a file. 
                using (StreamWriter sw = new StreamWriter("podsHeard.txt", false))  //do not append
                {
                    foreach (String ph in podsHeard)
                    {
                        String[] tn = ph.Split(podsHeardSeperator);
                        if (File.Exists(tn[0]))
                            sw.WriteLine(ph);  //leave suffix
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("ERROR Saving pods heard" + e.ToString());
            }
        }

        private void loadPods()
        {
            loadPodsHeard();
            String podPath = @"C:\Users\home\Documents\My Received Podcasts";
            podPlayList = new List<string>();
            if (Directory.Exists(podPath))
            {
                List<String> pl = new List<string>(Directory.EnumerateFiles(podPath, "*.mp3", SearchOption.AllDirectories));
                // suffle the podcasts
                pl = new List<String>(pl.OrderBy(item => rand.Next()));
                for (int ri = 0; ri < MAX_REPEATS; ri++)
                {
                    foreach (String pc in pl)
                    {
                        int rep = 0;
                        if (podsHeard != null)
                        {
                            foreach (string ph in podsHeard)
                            {
                                String[] tn = ph.Split(podsHeardSeperator);
                                if (tn[0].Equals(pc))
                                {
                                    if (tn.Contains("DELETE"))
                                    {
                                        rep = -1;  // this one due for deletion
                                        break;
                                    }
                                    rep++;
                                }
                            }
                        }
                        if (rep < 0 || rep != ri)
                            continue;
                        podPlayList.Add(pc);
                    }
                }
            }
        }
    }
}
