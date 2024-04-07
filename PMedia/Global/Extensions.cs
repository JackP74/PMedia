using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PMedia;

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
        return File.Exists(FilePath) && Video.Contains(new FileInfo(FilePath).Extension);
    }

    public static bool IsSubtitle(string FilePath)
    {
        return File.Exists(FilePath) && Subtitle.Contains(new FileInfo(FilePath).Extension);
    }

    public static FileType GetFileType(string FilePath)
    {
        if (IsVideo(FilePath))
            return FileType.Video;

        else if (IsSubtitle(FilePath))
            return FileType.Video;

        return FileType.Unkown;
    }

    public static string WithMaxLength(this string Value, int maxLength)
    {
        return Value?[..Math.Min(Value.Length, maxLength)];
    }

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