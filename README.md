<h1>PodPlayer</h1>
<p>Original by Robert Parker 2014 released under the <a href="http://opensource.org/licenses/MIT">MIT Licence</a> from his GitHub <a href="https://github.com/pyblendnet-js/podplayer">repository</a>.</p>
<p>This is a edited copy of help.html found in /PodPlayer/bin/Debug.</p>
<h2>Introduction</h2>
<p>PodPlayer is an open source audio file player for automated podcast and music listening.  Being at an age when early morning sleep sometimes eludes me, I like to listen to the range of interesting podcasts available from ABC Radio National.  <a href="http://juicereceiver.sourceforge.net/faq/">Juice</a> is a fairly good downloadeder but I find the playback options limited.  This player will randomly play the least heard of the downloaded podcasts and keep track of which have been heard. However, back to back documentaries can leave little space for cogitation, so this player can play music from your collection between documentaries.  In this mode it will play a quieter music item first, so if this player is used in conjunction with my other program, WakeUp, the  process of returning consciousness is less painful.
<h2>Installation</h2>
<p>Download the project and compile.  Development so far has been on Visual Studio 2010 and it has been tested on Windows8.0 and XP SP2 with DotNet Framework 3.5 installed. I have not provided a binary as this is far from a finished product and I would not care to be responsible for a virus being spread.  The only files written by this program will be located in the execution directory.  It can also delete files located in the podcast download path - so beware.
<h2>Configeration</h2>
<p>Run the program and you will see a "config" button in the bottom right corner.  Clicking on this will open the primary configeration dialog.
<p>The different items are as follows:
<ul>
<li>fadeInSpeed% = how quickly the volume rises for the first item.
<li>Play music between podCast = for when back to back documentary is too much.
<li>Set Hot Keys = raises a dialog to allow user customisation for the keyboard layout being used.
<li>PodFile Path = location where the podcast downloader (such as <a href="http://juicereceiver.sourceforge.net/faq/">Juice</a>) has loaded the files - typically mp3.  Juice creates seperate subdirectories for subject, so PodPlayer will grab of list of files from this base and all subdirectories when it is run.  It then seperates the podcasts in order of least heard to most heard and then randomises these.
<li>WakeSongList Playlist = the windows media player playlist of songs you would prefer to wake to.  The least heard song will be played when the program is first run.
<li>SongList Playlist = music you would like to hear between the pod cast documentaries.
<li>Save podcasts heard which exist = this will resave the podcasts heard list but only for the items that have not been deleted.  Usefull only if the file is getting so large that program startup is being slowed.
<li>Delete pods marked DELETE = if you have heard a podcast too many times it can be tagged by pressing the
DELETE key (which is usually the delete key :)) and then, if you are sure, you can press click this button and it will  offer to delete the marked podcast files.
<li>Review pods heard = This will bring up a dialog list of all the podcasts with descriptions if available.
<li>SaveConfig = save alterations to the configeration.
</ul>
<p>For minimum installation you should only need to set the podcast download directory.
<h2>HotKeys</h2>
<p>You can view the hot keys from the main window by pressing the K key (default setting).
<p>Keyboard actions for the main windows can be reconfigered in the following dialog accessed from the config dialog.</p>
<p>The different actions in the current version are:
<ul>
<li>Exit - on the first, this causes the program to flag a pending shutdown once the current audio is finished.  Pressing a second time will cause the program to exit immediately.
<li>Pause - pauses the current audio.
<li>Full Screen - toggles full screen mode.
<li>Fast Forward - jumps through the current audio in steps of 10% of the duration.
<li>Restart - jumps back to the start of the current audio.
<li>Delete - marks the heard log record that the current item is to be deleted and also skips to the next item.
<li>Keep - marks the heard log record that the current item is worth of keeping.
<li>Configure - opens the configure dialog window.
<li>Vol+ - increase the volume.
<li>Vol- - guess what this does.
<li>Help - opens the help.html file with the default html program - e.g. a browser.
<li>HotKeys - Shows a list of action descriptors and the associated key descriptors.
<li>Print Keys - prints a list of keyboard descriptors to the console if there is one.
</ul>
