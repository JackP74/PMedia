namespace PMedia
{
    public enum AudioType
    {
        None = 0,
        MonoL = 1,
        MonoR = 2,
        Stereo = 3,
        Surround = 4
    }

    public static class PlayerSettings
    {
        public static bool IsLoading = true;

        public static string playBtnTxt = "Play";
        public static string speedText = "Speed (1x)";
        public static string jumpText = "Jump (10s)";
        public static string autoPlayText = "Autoplay (5s)";

        public static string aspectRatio = string.Empty;

        public static bool onTop = false;
        public static bool gameMode = false;

        public static AudioType audioMode = AudioType.None;

        public static bool bottomOpen = true;
        public static bool topOpen = true;
        public static bool forceMouse = false;

        public static double taskProgress = 0d;
    }
}