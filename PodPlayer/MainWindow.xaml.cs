using System;
using System.ComponentModel; // CancelEventArgs
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
using System.Diagnostics;

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
        public List<string> wakeupSongList;
        public string wakeupSong;
        //public List<string> tweenSongs;
        public List<string> podsHeard;
        private char[] podsHeardSeperator = ",".ToArray<char>();
        public List<string> podPlayList;
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

        private bool playingSample = false;  //can be interupted

        public Dictionary<String,String> keyActions;
        public String[] keyList;
        public String[] actionList = { "Exit", "Pause"};
        public String keyConfigFid = "keyconfig.txt";

        public void loadKeyList()
        { 
            List<String> kl = new List<String>();
            for (int ki = 0; ki < 512; ki++)
            {
                try
                {
                    Key k = (Key)ki;
                    kl.Add(k.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception in loadKeyList:" + e.ToString());
                }
            }
            keyList = kl.ToArray();
        }

        public void saveActionKeys(String fid)
        {
            try
            {
                // Write each directory name to a file. 
                using (StreamWriter sw = new StreamWriter(fid, false))  //do not append
                {
                    foreach (String k in keyActions.Keys)
                    {
                        sw.WriteLine(keyActions[k] + " = " + k);
                    }
                }
            }
            catch (Exception ec)
            {
                System.Windows.MessageBox.Show("ERROR Saving pods heard" + ec.ToString());
            }
        }

        public void loadActionKeys(String fid)
        {
            keyActions = new Dictionary<String, String>();
            try
            {
                // Read and show each line from the file. 
                string line = "";
                using (StreamReader sr = new StreamReader(fid))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        String[] ka = line.Split("=".ToArray<char>());
                        keyActions[ka[1].Trim()] = ka[0].Trim();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in loadActionKeys:" + e.ToString());
                //System.Windows.MessageBox.Show("ERROR loading keyAction config:" + e.ToString());
            }
        }

        public void setKeyAction(String act, String key)
        {
            if (key != "None")
                keyActions[key] = act;
            else
            {
                foreach (String k in keyActions.Keys)
                {
                    if (keyActions[k] == act)
                    {
                        keyActions.Remove(k);
                        break;
                    }
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            configWindow = new Window1(this);

            rand = new Random(DateTime.Now.Millisecond);
        }

        private void windowLoaded(object sender, RoutedEventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("PodPlayer");
            if (processes.Count() > 1)
            {
                this.Close();
                return;
            }
            fullScreen(true);
            loadKeyList();
            loadActionKeys(keyConfigFid);
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
            if (wakeupSong != "")
            {
                wplayer.URL = wakeupSong;
            }
            else
                wplayer.URL = "sample.mp3";
            queueMedia();

            //this.KeyDown += new KeyEventHandler(keyPressed);
            fixLayout();
            //    IntPtr wh = new WindowInteropHelper(this).Handle;
            //    SendMessage(wh, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
            this.Focus();

            //  updateTimer setup
            updateTimer = new System.Windows.Threading.DispatcherTimer();
            updateTimer.Tick += new EventHandler(updateTick);
            updateTimer.Interval = new TimeSpan(0, 0, 1);
            updateTimer.Start();
        }

        private void windowClosing(Object sender, CancelEventArgs e)
        {
            //e.Cancel = true;  //if you wanted to stop it 
            if (updateTimer != null)
                updateTimer.Stop();
            if (wplayer != null)
            {
                wplayer.controls.stop();
                wplayer.close();
            }
            if (configWindow != null)
                configWindow.closeReally = true;
            configWindow.Close();
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
                    int j = i + 1;
                    while (wplayer.currentPlaylist.count > j)
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
                    if ((songPlayList.Count > 0) && (bool)configWindow.altMusicCheckBox.IsChecked)
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
            configWindow.ShowDialog();
            configWindow.Focus();
        }

        private void mouseBackground(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
                 toggleMusic();
            // else  togglePlay();  - left button was just annoying
         }

        private void mouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!ignorMove)
                this.Focus();
        }

        private void selectSong(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                //mouseBackground(sender, e);
                return;
            }
            ignorMove = true;
            int i = getIndexOfCurrent();
            fileDialog = new OpenFileDialog();
            if (!podPlayList.Contains(wplayer.URL))
                fileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(wplayer.URL);
            else
                fileDialog.InitialDirectory = configWindow.podPathTextBox.Text;
            DialogResult result = fileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) // Test result.
            {
                if (i < 0)
                    wplayer.URL = fileDialog.FileName;
                else
                    wplayer.currentPlaylist.insertItem(i + 1, wplayer.newMedia(fileDialog.FileName));
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
            {
                wplayer.controls.play();
                volLbl.Content = "";
            }
            else if (wplayer.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                wplayer.controls.pause();
                volLbl.Content = "PAUSED";
                volLbl.Opacity = 1.0;
            }
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
                    if (podMode)  // currently listening to a podcast
                    {
                        recordPodsHeard(wplayer.currentMedia.sourceURL, "DELETE");
                        wplayer.controls.next();
                    }
                    else  //listening to music
                    {
                        // really need to add DELETE to previous line
                    }
                    break;
                case Key.Enter:
                    markAsKeep = true;
                    break;
                case Key.C:
                    showConfig(null, null);
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
            if (wplayer.currentMedia == null)
                return;
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
                if (!podPlayList.Contains(lastMediaURL))
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
            if (wplayer.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
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
            }
            Double rem_sec = wplayer.currentMedia.duration - wplayer.controls.currentPosition;
            lastMediaHeard = (rem_sec < 2);  //prevents record unless end is heard
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

        public string getPodName(String url)
        {
            WMPLib.WindowsMediaPlayer tplayer = new WMPLib.WindowsMediaPlayer();
            tplayer.URL = url;
            String name_str = tplayer.currentMedia.name;
            tplayer.close();
            return name_str;
        }

        public string getPodStats(String url)
        {
            if (!File.Exists(url))
                return "Gone";
            String stat_str = getPodName(url);
            // see how many times it has been heard etc
            String lastHeard = "NeverHeard";
            int heardCount = 0;
            int skipCount = 0;
            foreach (string ph in podsHeard)
            {
                String[] tn = ph.Split(podsHeardSeperator);
                if (tn[0].Equals(url))
                {
                    if (tn.Length > 1)
                    {
                        if (tn[tn.Length - 1].StartsWith("SKIPPED"))
                            continue;
                        if (tn[1].Length == 15)
                        {
                            DateTime lh = DateTime.ParseExact(tn[1], "yyyyMMdd_HHmmss", null);
                            TimeSpan ld = (DateTime.Now - lh);
                            lastHeard = ld.TotalDays.ToString("F0") + " days";
                         }
                        else
                        {
                            lastHeard = tn[1] + " heard";
                        }
                    }
                    else
                    {
                        lastHeard = "??? days";
                    }
                    if (tn[tn.Length - 1].StartsWith("SKIPPED"))
                        skipCount++;
                    else
                        heardCount++;
                }
            }
            lastHeard = String.Format("{0,25}", lastHeard);
            stat_str = "Count=" + heardCount.ToString() + " Skipped=" + skipCount.ToString() + " " + lastHeard + " Name=" + stat_str;
            return stat_str;
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
                if (!podPlayList.Contains(urli1))
                    nextMediaLbl.Foreground = Brushes.Green;
                else
                    nextMediaLbl.Foreground = Brushes.Blue;
            }
            playlistStatsLbl.Content = podsFoundCount.ToString() + " pod files, " + podsHeardCount + " heard";
            if (!stopAtNext)
            {
                nextMediaLbl.Content = "Next: " + status_str;
                nextMediaLbl.FontSize = nextMediaLbl.ActualWidth / status_str.Length * 2;
                if (nextMediaLbl.FontSize > nextMediaLbl.ActualHeight * 0.6)
                    nextMediaLbl.FontSize = nextMediaLbl.ActualHeight * 0.6;
            }
        }

        private List<string> loadMediaPaths(string fid)
        {
            List<string> media_list = null;
            try
            {
                // Read and show each line from the file. 
                string line = "";
                using (StreamReader sr = new StreamReader(fid))
                {
                    media_list = new List<string>();
                    while ((line = sr.ReadLine()) != null)
                    {
                        int ms = line.IndexOf("<media src=");
                        if (ms < 0)
                            continue;
                        string mfid = line.Split('"')[1];  //Substring(line.IndexOf("=")+2);
                        if (mfid.Contains(".."))
                            mfid = mfid.Replace("..", @"C:\Users\home\Music");
                        if (File.Exists(mfid))
                            media_list.Add(mfid);
                    }

                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("ERROR loading wakeup songs" + e.ToString());
            }
            return media_list;
        }

        private void loadSongs()
        {
            List<string> rwl = loadMediaPaths(configWindow.songListPathTextBox.Text);
            if (rwl != null)
            {
                List<string> wl = orderSongList(rwl);
                wakeupSong = wl[0];
                songPlayList = wl;  //only used if tween song list fails to load
            }
            List<string> rtl = loadMediaPaths(configWindow.songListPathTextBox.Text);
            if (rtl != null)
                songPlayList = orderSongList(rtl);
        }

        private List<string> orderSongList(List<string> src)
        {
            if (src == null)
                return null;
            List<string> l = new List<string>();
            List<String> pl = new List<string>(src);
            // suffle the song list
            pl = new List<String>(pl.OrderBy(item => rand.Next()));
            int fc = 0;
            int ri = 0;
            while (fc < pl.Count)
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
                    if (rep != ri)
                        continue;
                    fc++;
                    if (rep < 0)
                        continue;
                    l.Add(pc);
                }
                ri++;
            }
            return l;
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

        public void savePodsHeard()
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
                                        if (ri == 0 && rep == 0)
                                            podsFoundCount--;
                                        break;
                                    }
                                    if (ri == 0 && rep == 0 && podPlayList.Contains(ph))
                                        podsHeardCount++;
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

        public void sampleNamed(string name)
        {
            string tn = findPod(name);
            if (tn == null)
            return;
            if(!playingSample && wplayer.playState == WMPLib.WMPPlayState.wmppsPlaying)
                return;
            wplayer.URL = tn;
            wplayer.controls.play();
            playingSample = true;
        }

        public void stopSample()
        {
            wplayer.controls.stop();
            playingSample = false;
        }

        public void deleteDelete()
        {   // delete pods heard that are marked delete
            int dc = 0;
            foreach (string ph in podsHeard)
            {
                String[] tn = ph.Split(podsHeardSeperator);
                if (!podPlayList.Contains(tn[0]))  // don't delete songs
                    continue;
                if (tn.Contains("DELETE"))
                {
                    dc++;
                    Console.WriteLine("Deleting file:" + tn[0]);
                }
            }
            if (System.Windows.MessageBox.Show("Delete " + dc.ToString() + " files", "WARNING", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.No)
                return;
            foreach (string ph in podsHeard)
            {
                String[] tn = ph.Split(podsHeardSeperator);
                if (!podPlayList.Contains(tn[0]))  // don't delete songs
                    continue;
                if (tn.Contains("DELETE"))
                {
                    Console.WriteLine("Deleting file:" + tn[0]);
                    File.Delete(tn[0]);
                }
            }
        }

        public string findPod(string name)
        {
            foreach (string ph in podsHeard)
            {
                String[] tn = ph.Split(podsHeardSeperator);
                if (!podPlayList.Contains(tn[0]))  // don't delete songs
                    continue;
                String pod_name = getPodName(tn[0]);
                if (pod_name.Equals(name))
                    return tn[0];
            }
            return null;
        }

        public void deletePodCast(string name)
        {   // delete pods heard that are marked delete
            string tn = findPod(name);
            if (tn == null)
            {
                Console.WriteLine("Cannot find file {0} to delete it." + tn);
            }
            if (System.Windows.MessageBox.Show("Delete file name:" + tn, "WARNING", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.No)
                return;
            Console.WriteLine("Deleting file:" + tn);
            try
            {
                File.Delete(tn);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Exception:" + e.ToString());
            }
        }

        public void clearPlayList()
        {
            wplayer.controls.stop();
            wplayer.currentPlaylist.clear();
        }

        public void addNamed(string name)
        {
            string url = findPod(name);
            if (url == null)
                return;
            wplayer.currentPlaylist.appendItem(wplayer.newMedia(url));
        }

        public void playList()
        {
            wplayer.controls.play();
        }
    }
}
