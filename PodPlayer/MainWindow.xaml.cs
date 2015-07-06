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
        private List<string> podPlayList;
        private bool fixLayoutPending;
        private bool podMode;
        private string lastMediaURL;
        private Random rand;
        const int MAX_REPEATS = 3;
        private bool lastMediaHeard;
        private int volume;

        public MainWindow()
        {
            InitializeComponent();
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
                wplayer.URL = wakeupSongs[rand.Next(wakeupSongs.Count)];
            else
                wplayer.URL = "sample.mp3";

            //WMPLib.IWMPPlaylist pl = wplayer.playlistCollection.newPlaylist("todays");
            //pl.appendItem(WMPLib.
            //String nm = wplayer.currentPlaylist.name;
            foreach (String p in podPlayList)
                wplayer.currentPlaylist.appendItem(wplayer.newMedia(p));
            wplayer.controls.play();
            startTime = DateTime.Now;
            //  updateTimer setup
            updateTimer = new System.Windows.Threading.DispatcherTimer();
            updateTimer.Tick += new EventHandler(updateTick);
            updateTimer.Interval = new TimeSpan(0, 0, 1);
            updateTimer.Start();
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
        private void windowLoaded(object sender, RoutedEventArgs e)
        {
            //this.KeyDown += new KeyEventHandler(keyPressed);
            fixLayout();
        //    IntPtr wh = new WindowInteropHelper(this).Handle;
        //    SendMessage(wh, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
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
                    this.Close();
                    break;
                case Key.Space:
                    togglePlay();
                    break;
                case Key.W:
                    fullScreen(this.WindowState == WindowState.Normal);
                    break;
                case Key.Left:
                    wplayer.controls.currentPosition = 0;
                    wplayer.controls.play();
                    break;
                case Key.Right:
                    wplayer.controls.next();
                    podMode = false;
                    break;
                case Key.Delete:
                    if (podMode)
                    {
                        wplayer.controls.next();
                        recordPodsHeard(lastMediaURL + "***DELETE");
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
            volLbl.Height = this.ActualHeight * 0.1;
            volLbl.Margin = new Thickness(20, progressRect.Margin.Top + progressRect.Height + 20, 20, 0);
            volLbl.FontSize = volLbl.Height * 0.6;
            double y = statusLbl.Height + progressRect.Height + 20;
            clockDial.Height = this.ActualHeight - y - saveHeardBtn.Height;
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
            if (podMode && lastMediaHeard)  //have fini music it seems
            {
                if (!wplayer.currentMedia.sourceURL.Equals(lastMediaURL) || wplayer.playState == WMPLib.WMPPlayState.wmppsMediaEnded)
                {
                    recordPodsHeard(lastMediaURL);
                }
            }
            if (lastMediaURL != wplayer.currentMedia.sourceURL)
            {
                lastMediaURL = wplayer.currentMedia.sourceURL;
                string status_str = wplayer.playState.ToString().Substring(5) + ":" + wplayer.currentMedia.name + " = " + wplayer.currentMedia.duration.ToString("F0") + " Sec";
                statusLbl.Content = status_str;
                statusLbl.FontSize = statusLbl.ActualWidth / status_str.Length * 2;
                if (statusLbl.FontSize > statusLbl.ActualHeight * 0.6)
                    statusLbl.FontSize = statusLbl.ActualHeight * 0.6;
                status_str = "End of play list";  // assume end of list
                for (int i = 0; i < wplayer.currentPlaylist.count - 1; i++)
                {
                    if (wplayer.controls.currentItem.sourceURL == wplayer.currentPlaylist.get_Item(i).sourceURL)
                        status_str = wplayer.currentPlaylist.get_Item(i + 1).name;
                }
                nextMediaLbl.Content = status_str;
                nextMediaLbl.FontSize = nextMediaLbl.ActualWidth / status_str.Length * 2;
                if (nextMediaLbl.FontSize > nextMediaLbl.ActualHeight * 0.6)
                    nextMediaLbl.FontSize = nextMediaLbl.ActualHeight * 0.6;
            }
            if (wplayer.settings.volume < volume)
            {
                wplayer.settings.volume += 1;
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
                        String tn = ph;
                        if (ph.EndsWith("***DELETE"))
                            tn = ph.Substring(0, ph.Length - 9);
                        if (File.Exists(tn))
                            podsHeard.Add(ph);  // leave suffix
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("ERROR loading pods heard" + e.ToString());
            }
        }

        private void recordPodsHeard(string fid)
        {
            try
            {
                // Write each directory name to a file. 
                using (StreamWriter sw = new StreamWriter("podsHeard.txt", true))  //append
                {
                    sw.WriteLine(fid);
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
                        String tn = ph;
                        if (ph.EndsWith("***DELETE"))
                            tn = ph.Substring(0, ph.Length - 9);
                        if (File.Exists(tn))
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
                                if (ph.Equals(pc + "***DELETE"))
                                {
                                    rep = -1;  // this one due for deletion
                                    break;
                                }
                                if (ph.Equals(pc)) rep++;
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
