using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PMedia
{
    internal static class Extensions
    {
        public enum FileType
        {
            Video,
            Subtitle,
            Unkown
        }

        public static List<string> Video = new string[]
        {
                ".3g2", ".3gp", ".3gp2", ".3gpp", ".amv", ".asf", ".avi", ".bik", ".divx", ".drc", ".dv", ".dvr-ms", ".evo", ".f4v", ".flv", ".gvi", ".gxf", ".m1v", ".m2t", ".m2v", ".m2ts", ".m4v", ".mkv", ".mov", ".mp2v", ".mp4", ".mp4v", ".mpa", ".mpe", ".mpeg", ".mpeg1", ".mpeg2", ".mpeg4", ".mpg", ".mpv2", ".mts", ".mtv", ".mxf", ".nsv", ".nuv", ".ogg", ".ogm", ".ogx", ".ogv", ".rec", ".rm", ".rmvb", ".rpl", ".thp", ".tod", ".tp", ".ts", ".tts", ".vob", ".vro", ".webm", ".wmv", ".wtv", ".xesc", ".3ga", ".669", ".a52", ".aac", ".ac3", ".adt", ".adts", ".aif", ".aifc", ".aiff", ".au", ".amr", ".aob", ".ape", ".caf", ".cda", ".dts", ".dsf", ".dff", ".flac", ".it", ".m4a", ".m4p", ".mid", ".mka", ".mlp", ".mod", ".mp1", ".mp2", ".mp3", ".mpc", ".mpga", ".oga", ".oma", ".opus", ".qcp", ".ra", ".rmi", ".snd", ".s3m", ".spx", ".tta", ".voc", ".vqf", ".w64", ".wav", ".wma", ".wv", ".xa", ".xm"
        }.ToList();

        public static List<string> Subtitle = new string[]
        {
                ".cdg", ".idx", ".srt", ".sub", ".utf", ".ass", ".ssa", ".aqt", ".jss", ".psb", ".rt", ".sami", ".smi", ".txt", ".smil", ".stl", ".usf", ".dks", ".pjs", ".mpl2", ".mks", ".vtt", ".tt", ".ttml", ".dfxp", ".scc"
        }.ToList();

        public static bool IsVideo(string FilePath)
        {
            return Video.Contains(new FileInfo(FilePath).Extension);
        }

        public static bool IsSubtitle(string FilePath)
        {
            return Subtitle.Contains(new FileInfo(FilePath).Extension);
        }

        public static FileType GetFileType(string FilePath)
        {
            if (IsVideo(FilePath))
                return FileType.Video;

            if (IsSubtitle(FilePath))
                return FileType.Video;

            return FileType.Unkown;
        }
    }

    internal static class Player
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
    }

    public static class InputExtensions
    {
        public static int LimitToRange(this int value, int Minimum, int Maximum)
        {
            if (value < Minimum) { return Minimum; }
            if (value > Maximum) { return Maximum; }
            return value;
        }

        public static long LimitToRange(this long value, long Minimum, long Maximum)
        {
            if (value < Minimum) { return Minimum; }
            if (value > Maximum) { return Maximum; }
            return value;
        }

        public static double LimitToRange(this double value, double Minimum, double Maximum)
        {
            if (value < Minimum) { return Minimum; }
            if (value > Maximum) { return Maximum; }
            return value;
        }

        public static int RoundOff(this int i)
        {
            return ((int)Math.Round(i / 10.0)) * 10;
        }

        public static int ToInt32(this string value)
        {
            if (int.TryParse(value, out _))
            {
                return Convert.ToInt32(value);
            }

            return -1;
        }
    }
}