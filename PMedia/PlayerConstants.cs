using System;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace PMedia
{
    internal static class PlayerConstants
    {
        public static class Images
        {
            public static readonly string btnAbout = "Resources/btnAbout.png";
            public static readonly string btnAcceleration = "Resources/btnAcceleration.png";
            public static readonly string btnAdd = "Resources/btnAdd.png";
            public static readonly string btnAudio = "Resources/btnAudio.png";
            public static readonly string btnBackward = "Resources/btnBackward.png";
            public static readonly string btnEdit = "Resources/btnEdit.png";
            public static readonly string btnFile = "Resources/btnFile.png";
            public static readonly string btnForward = "Resources/btnForward.png";
            public static readonly string btnFullScreenOff = "Resources/btnFullScreenOff.png";
            public static readonly string btnFullScreenOn = "Resources/btnFullScreenOn.png";
            public static readonly string btnGameModeOff = "Resources/btnGameModeOff.png";
            public static readonly string btnGameModeOn = "Resources/btnGameModeOn.png";
            public static readonly string btnMediaInfo = "Resources/btnMediaInfo.png";
            public static readonly string btnMute = "Resources/btnMute.png";
            public static readonly string btnNext = "Resources/btnNext.png";
            public static readonly string btnOnTop = "Resources/btnOnTop.png";
            public static readonly string btnOpen = "Resources/btnOpen.png";
            public static readonly string btnPause = "Resources/btnPause.png";
            public static readonly string btnPlay = "Resources/btnPlay.png";
            public static readonly string btnPlayback = "Resources/btnPlayback.png";
            public static readonly string btnPlaylist = "Resources/btnPlaylist.png";
            public static readonly string btnPrevious = "Resources/btnPrevious.png";
            public static readonly string btnQuit = "Resources/btnQuit.png";
            public static readonly string btnRecent = "Resources/btnRecent.png";
            public static readonly string btnScreenShot = "Resources/btnScreenShot.png";
            public static readonly string btnSelectTrack = "Resources/btnSelectTrack.png";
            public static readonly string btnSet = "Resources/btnSet.png";
            public static readonly string btnSettings = "Resources/btnSettings.png";
            public static readonly string btnShutDown = "Resources/btnShutDown.png";
            public static readonly string btnStop = "Resources/btnStop.png";
            public static readonly string btnSubtitle = "Resources/btnSubtitle.png";
            public static readonly string btnSwitch = "Resources/btnSwitch.png";
            public static readonly string btnTrash = "Resources/btnTrash.png";
            public static readonly string btnVideo = "Resources/btnVideo.png";
            public static readonly string btnVideoList = "Resources/btnVideoList.png";
            public static readonly string btnVolume1 = "Resources/BtnVolume1.png";
            public static readonly string btnVolume2 = "Resources/BtnVolume2.png";
            public static readonly string btnVolume3 = "Resources/BtnVolume3.png";
        }

        public static class ExternalCommands
        {
            public const int PM_PLAY = 0xFFF0;
            public const int PM_PAUSE = 0xFFF1;
            public const int PM_STOP = 0xFFF2;
            public const int PM_FORWARD = 0xFFF3;
            public const int PM_BACKWARD = 0xFFF4;
            public const int PM_NEXT = 0xFFF5;
            public const int PM_PREVIOUS = 0xFFF6;
            public const int PM_VOLUMEUP = 0xFFF7;
            public const int PM_VOLUMEDOWN = 0xFFF8;
            public const int PM_MUTE = 0xFFF9;
            public const int PM_AUTOPLAY = 0xFFFA;
            public const int PM_FILE = 0xFFFB;
            public const uint WM_COPYDATA = 0x004A;
        }

        public static BitmapImage ImageResource(string pathInApplication, Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetCallingAssembly();

            if (pathInApplication[0] == '/')
                pathInApplication = pathInApplication.Substring(1);

            return new BitmapImage(new Uri(@"pack://application:,,,/" + assembly.GetName().Name + ";component/" + pathInApplication, UriKind.Absolute));
        }
    }
}
