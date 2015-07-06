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
using System.Windows.Forms;  //for file dialog


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
        private List<string> songPlayList;
        private bool fixLayoutPending;
        private bool podMode;
        private bool musicMode;
        private string lastMediaURL;
        private Random rand;
        const int MAX_REPEATS = 3;
        private bool lastMediaHeard;
        private int volume;
        private int podsFoundCount = 0;
        private int podsHeardCount = 0;
        private bool markAsKeep = false;
        private bool stopAtNext = false;

        private Window1 configWindow;
        private OpenFileDialog fileDialog = null;
        private bool ignorMove = false;
        public MainWindow()
        {
            InitializeComponent();

            configWindow = new Window1();

            rand = new Random(DateTime.Now.Millisecond);
            fullScreen(true);
            loadPodsHeard();
            loadSongs();
            loadPods();
            podMode = false;
            musicMode = false;
            lastMediaHeard = false;
            volume = 100;
            markAsKeep = false;
            stopAtNext = false;

            wplayer = new WMPLib.WindowsMediaPlayer();
            wplayer.settings.volume = 20;
            //if (wakeupSongs.Count > 0)
            //{
            //    int ni = rand.Next(wakeupSongs.Count);
            //    wplayer.URL = wakeupSongs[ni];
            //} 
            //else
            //    wplayer.URL = "sample.mp3";

            //WMPLib.IWMPPlaylist pl = wplayer.playlistCollection.newPlaylist("todays");
            //pl.appendItem(WMPLib.
            //String nm = wplayer.currentPlaylist.name;
            if (wakeupSongs.Count > 0)
            {
                wplayer.URL = songPlayList[0];
            }
            else
                wplayer.URL = "sample.mp3";
            queueMedia();

            //  updateTimer setup
            updateTimer = new System.Windows.Threading.DispatcherTimer();
            updateTimer.Tick += new EventHandler(updateTick);
            updateTimer.Interval = new TimeSpan(0, 0, 1);
            updateTimer.Start();
        }

        private void queueMedia(bool music_only = false)
        {
            int ni = 1;
            //wplayer.controls.stop();
            //wplayer.currentPlaylist.clear();
            // remove everything from current position onwards
            for (int i = 0; i < wplayer.currentPlaylist.count - 1; i++)
            {
                if (wplayer.controls.currentItem.sourceURL == wplayer.currentPlaylist.get_Item(i).sourceURL)
                {
                    for (int j = i + 1; j < wplayer.currentPlaylist.count - 1; j++)
                    {
                        wplayer.currentPlaylist.removeItem(wplayer.currentPlaylist.get_Item(j));
                    }
                    break;
                }
            }
            if (music_only)
            {
                for (ni = 1; ni < songPlayList.Count; ni++)
                {
                    String url = songPlayList[ni];
                    wplayer.currentPlaylist.appendItem(wplayer.newMedia(url));
                }
            }
            else
            {
                foreach (String p in podPlayList)
                {
                    wplayer.currentPlaylist.appendItem(wplayer.newMedia(p));
                    if ((wakeupSongs.Count > 0) && (bool)configWindow.altMusicCheckBox.IsChecked)
                    {
                        String url = songPlayList[ni++];
                        if (ni >= songPlayList.Count)
                            ni = 0;  //start again
                        wplayer.currentPlaylist.appendItem(wplayer.newMedia(url));
                    }
                }
                wplayer.controls.play();
                startTime = DateTime.Now;
            }
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
            ignorMove = true;
            configWindow.Show();
            configWindow.Focus();
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
            if (e.LeftButton == MouseButtonState.Pressed)
                togglePlay();
            else
                toggleMusic();
        }

        private void mouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!ignorMove)
                this.Focus();
        }

        private void selectSong(object sender, MouseButtonEventArgs e)
        {
            ignorMove = true;
            int i = getIndexOfCurrent();
            fileDialog = new OpenFileDialog();
            if (wakeupSongs.Contains(wplayer.URL))
                fileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(wplayer.URL);
            else
                fileDialog.InitialDirectory = configWindow.podPathTextBox.Text;
            DialogResult result = fileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) // Test result.
            {
                if(i < 0)
                    wplayer.URL = fileDialog.FileName;
                else
                    wplayer.currentPlaylist.insertItem(i+1, wplayer.newMedia(fileDialog.FileName));
                if (sender == statusLbl)
                    recordPodsHeard(wplayer.currentMedia.sourceURL, "SKIPPED_AT=" + wplayer.controls.currentPosition.ToString("F0") + "Sec");
                wplayer.controls.next();
                    }
            Console.WriteLine(result);
            ignorMove = false;
        }


        private void togglePlay()
        {
            if (wplayer.playState == WMPLib.WMPPlayState.wmppsPaused)
                wplayer.controls.play();
            else if (wplayer.playState == WMPLib.WMPPlayState.wmppsPlaying)
                wplayer.controls.pause();
        }

        private void toggleMusic()
        {
            if (musicMode)
            {
                musicMode = false;
                loadSongs();
                loadPods();
                queueMedia(false);
                updateNext();
            }
            else
            {
                musicMode = true;
                loadSongs();
                queueMedia(true);
                updateNext();
            }
        }

        private void keyPressed(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (stopAtNext && e.Key != Key.Escape)  // exit from stopAtNext mode
            {
                stopAtNext = false;
                updateNext();
            }
            switch (e.Key)
            {
                case Key.Escape:
                    if (!stopAtNext)
                    {
                        stopAtNext = true;
                        nextMediaLbl.Content = "Program will exit at end of this track (or press ESC again to exit now)";
                        nextMediaLbl.Foreground = Brushes.Orange;
                    }
                    else
                    {
                        wplayer.controls.stop();
                        configWindow.Close();
                        this.Close();
                    }
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
                    recordPodsHeard(wplayer.currentMedia.sourceURL, "SKIPPED_AT=" + wplayer.controls.currentPosition.ToString("F0") + "Sec");
                    wplayer.controls.next();
                    podMode = false;
                    break;
                case Key.Delete:
                    if (podMode)
                    {
                        recordPodsHeard(wplayer.currentMedia.sourceURL, "DELETE");
                        wplayer.controls.next();
                    }
                    break;
                case Key.Enter:
                    markAsKeep = true;
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
            lastHeardLabel.Height = statusLbl.Height * 0.4;
            lastHeardLabel.FontSize = lastHeardLabel.Height * 0.6;
            timesHeardLabel.Height = statusLbl.Height * 0.4;
            timesHeardLabel.FontSize = timesHeardLabel.Height * 0.6;
            lastHeardLabel.Margin = new Thickness(20, progressRect.Margin.Top + progressRect.Height, 0, 0);
            timesHeardLabel.Margin = new Thickness(20, progressRect.Margin.Top + progressRect.Height, 0, 0);
            volLbl.Height = this.ActualHeight * 0.1;
            volLbl.Margin = new Thickness(20, progressRect.Margin.Top + progressRect.Height + 20, 20, 0);
            volLbl.FontSize = volLbl.Height * 0.6;
            double y = statusLbl.Height + progressRect.Height + 20;
            clockDial.Height = this.ActualHeight - y;
            clockDial.Width = this.ActualWidth - 40;
            clockDial.FontSize = clockDial.Height * 0.6;
            playlistStatsLbl.Height = this.ActualHeight * 0.06;
            playlistStatsLbl.FontSize = nextMediaLbl.Height * 0.6;
            nextMediaLbl.Height = this.ActualHeight * 0.06;
            nextMediaLbl.FontSize = nextMediaLbl.Height * 0.6;
            playlistStatsLbl.Margin = new Thickness(20, 0, 0, nextMediaLbl.Height + 40);
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
                if (stopAtNext)  //user pressed the esc key once
                {
                    wplayer.controls.stop();
                    configWindow.Close();
                    this.Close();
                    return;
                }
                lastMediaURL = wplayer.currentMedia.sourceURL;
                if (wakeupSongs.Contains(lastMediaURL))
                    statusLbl.Foreground = Brushes.LawnGreen;
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
                            if (tn[tn.Length - 1].StartsWith("SKIPPED"))
                                continue;
                            if (tn[1].Length == 15)
                            {
                                DateTime lh = DateTime.ParseExact(tn[1], "yyyyMMdd_HHmmss", null);
                                TimeSpan ld = (DateTime.Now - lh);
                                if (ld.TotalDays >= 2.0)
                                    lastHeard = ld.TotalDays.ToString("F0") + "days since heard";
                                else if (ld.TotalHours >= 1.0)
                                    lastHeard = ld.TotalHours.ToString("F0") + "hours since heard";
                                else
                                    lastHeard = ld.TotalMinutes.ToString("F0") + "minutes since heard";
                            }
                            else
                            {
                                lastHeard = tn[1];
                            }
                        }
                        else
                        {
                            lastHeard = "Heard";
                        }
                        if (!tn[tn.Length - 1].StartsWith("SKIPPED"))
                            heardCount++;
                    }
                }
                lastHeardLabel.Content = lastHeard;
                timesHeardLabel.Content = "Heard " + heardCount.ToString();
                updateNext();
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
            if (musicMode)
                clockDial.Foreground = Brushes.Aquamarine;
            else
                clockDial.Foreground = Brushes.Blue;
        }

        private int getIndexOfCurrent()
        {
            for (int i = 0; i < wplayer.currentPlaylist.count - 1; i++)
            {
                String urli = wplayer.currentPlaylist.get_Item(i).sourceURL;
                if (wplayer.controls.currentItem.sourceURL == urli)
                    return i;
            }
            return -1;
        }

        private void updateNext()
        {
            string status_str = "End of play list";  // assume end of list
            nextMediaLbl.Foreground = Brushes.Red;
            int i = getIndexOfCurrent();
            if (i >= 0)
            {
                String urli1 = wplayer.currentPlaylist.get_Item(i + 1).sourceURL;
                status_str = wplayer.currentPlaylist.get_Item(i + 1).name;
                if (wakeupSongs.Contains(urli1))
                    nextMediaLbl.Foreground = Brushes.Green;
                else
                    nextMediaLbl.Foreground = Brushes.Blue;
            }
            playlistStatsLbl.Content = podsFoundCount.ToString() + " pod files, " + podsHeardCount + " heard";
            if (!stopAtNext)
            {
                nextMediaLbl.Content = "Next:" + status_str;
                nextMediaLbl.FontSize = nextMediaLbl.ActualWidth / status_str.Length * 2;
                if (nextMediaLbl.FontSize > nextMediaLbl.ActualHeight * 0.6)
                    nextMediaLbl.FontSize = nextMediaLbl.ActualHeight * 0.6;
            }
        }

        private void loadWakeupSongs()
        {
            try
            {
                // Read and show each line from the file. 
                string line = "";
                using (StreamReader sr = new StreamReader(configWindow.songListPathTextBox.Text))
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

        private void loadSongs()
        {
            songPlayList = new List<string>();
            loadWakeupSongs();
            List<String> pl = new List<string>(wakeupSongs);
            // suffle the song list
            pl = new List<String>(pl.OrderBy(item => rand.Next()));
            int fc = 0;
            int ri = 0;
            while(fc < pl.Count)
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
                                if (tn[tn.Length - 1].StartsWith("SKIPPED"))
                                    continue;  
                                rep++;
                            }
                        }
                    }
                    if(rep != ri)
                      continue;
                    fc++;
                    if(rep < 0)
                        continue;
                    songPlayList.Add(pc);
                }
                ri++;
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
            String rec_str = fid + "," + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "," + suffix;
            podsHeard.Add(rec_str);
            try
            {
                // Write each directory name to a file. 
                using (StreamWriter sw = new StreamWriter("podsHeard.txt", true))  //append
                {
                    if (suffix.Length == 0 && markAsKeep)
                        suffix = "KEEP";
                    markAsKeep = false;
                    sw.WriteLine(rec_str);
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
            podPlayList = new List<string>();
            if (Directory.Exists(configWindow.podPathTextBox.Text))
            {
                List<String> pl = new List<string>(Directory.EnumerateFiles(configWindow.podPathTextBox.Text, "*.mp3", SearchOption.AllDirectories));
                podsFoundCount = pl.Count;
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
                                        if (rep == 0) podsFoundCount--;
                                        break;
                                    }
                                    if (rep == 0 && !wakeupSongs.Contains(pc)) podsHeardCount++;
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
