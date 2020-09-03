#region "Imports"
using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Interop;

using Ookii.Dialogs.WinForms;
using MessageCustomHandler;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

using MenuItem = System.Windows.Controls.MenuItem;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using Label = System.Windows.Controls.Label;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Slider = System.Windows.Controls.Slider;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
#endregion

// TO DO: OPTIMIZE
namespace PMedia
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region "Win32 Imports"
        // WPF has no real X-Y location, this fixes that
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);
        #endregion

        #region "Variables"
        private PlayerOverlay MainOverlay { get; set; }

        private readonly string videoPositionDir = AppDomain.CurrentDomain.BaseDirectory + @"\Data";
        private readonly string recentsPath = AppDomain.CurrentDomain.BaseDirectory + @"\recents.ini";

        private readonly Random random = new Random(Guid.NewGuid().GetHashCode());
        private readonly Settings settings;

        private Rect rect = new Rect(); // used in X-Y location
        private System.Windows.Forms.Timer MouseTimer;

        private readonly string AppName;
        public event PropertyChangedEventHandler PropertyChanged;
        private string playBtnTxt = "Play";
        private string speedText = "Speed (1x)";
        private string jumpText = "Jump (10s)";
        private string autoPlayText = "Autoplay (5s)";
        private string aspectRatio = string.Empty;
        private AudioType audioMode = AudioType.None;
        private readonly TvShow tvShow;
        private bool IsLoading = true;
        private bool onTop = false;
        private bool gameMode = false;
        private ShutDownCommand shutDownCmd;

        private MediaPlayer mediaPlayer;
        private LibVLC libVLC;
        private VideoListWindow videoListWindow;
        private readonly VideoPosition videoPosition;
        private ContextMenuStrip PlayerContextMenu;

        private readonly List<JumpCommand> jumpCommands;
        private static bool isSliderControl = false;
        private Thread threadShutDown;

        private WindowState lastState = WindowState.Normal;
        private const int BottomSize = 40;

        private Screen screen;
        private readonly Recents recents;
        private KeyboardHook keyboardHook = null;

        private ToolStripMenuItem SettingsMenuVideoTrack;
        private ToolStripMenuItem SettingsMenuAudioTrack;
        private ToolStripMenuItem SettingsMenuSubtitleTrack;
        #endregion

        #region "Proprieties"
        public System.Drawing.Point Location
        {
            set
            {
                Left = value.X;
                Top = value.Y;
            }

            get
            {
                return new System.Drawing.Point(rect.Left, rect.Top);
            }
        }

        public string PlayBtnTxt
        {
            set
            {
                playBtnTxt = value;

                if (value.ToLower().Trim() == "play")
                {
                    if (MainOverlay.btnPlay.Dispatcher.CheckAccess())
                    {
                        MainOverlay.btnPlayImage.Source = ImageResource(Images.btnPlay);
                        MainOverlay.MenuPlaybackPlayImage.Source = ImageResource(Images.btnPlay);
                    }
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            MainOverlay.btnPlayImage.Source = ImageResource(Images.btnPlay);
                            MainOverlay.MenuPlaybackPlayImage.Source = ImageResource(Images.btnPlay);
                        });
                    }
                }
                else
                {
                    if (MainOverlay.btnPlay.Dispatcher.CheckAccess())
                    {
                        MainOverlay.btnPlayImage.Source = ImageResource(Images.btnPause);
                        MainOverlay.MenuPlaybackPlayImage.Source = ImageResource(Images.btnPause);
                    }
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            MainOverlay.btnPlayImage.Source = ImageResource(Images.btnPause);
                            MainOverlay.MenuPlaybackPlayImage.Source = ImageResource(Images.btnPause);
                        });
                    }
                       
                }

                OnPropertyChanged("PlayBtnTxt");
            }

            get
            {
                return playBtnTxt;
            }
        }

        public string SpeedText
        {
            set
            {
                speedText = value;
                OnPropertyChanged("SpeedText");
            }

            get
            {
                return speedText;
            }
        }

        public string JumpText
        {
            set
            {
                jumpText = value;
                OnPropertyChanged("JumpText");
            }

            get
            {
                return jumpText;
            }
        }

        public string AutoPlayText
        {
            set
            {
                autoPlayText = value;
                OnPropertyChanged("AutoPlayText");
            }

            get
            {
                return autoPlayText;
            }
        }

        private bool Mute
        {
            set
            {
                settings.IsMute = value;

                if (value)
                {
                    SetImage(MainOverlay.btnMuteImage, Images.btnMute);

                    if (mediaPlayer != null && mediaPlayer.Media != null)
                    {
                        StartThread(() => { mediaPlayer.Mute = true; });
                    }

                }
                else // false
                {

                    if (mediaPlayer != null && mediaPlayer.Media != null)
                    {
                        StartThread(() => { mediaPlayer.Mute = false; });
                    }

                    int cVolume = Volume;

                    if(cVolume >= 67)
                    {
                        SetImage(MainOverlay.btnMuteImage, Images.btnVolume3);
                    }
                    else if(cVolume >= 34)
                    {
                        SetImage(MainOverlay.btnMuteImage, Images.btnVolume2);
                    }
                    else
                    {
                        SetImage(MainOverlay.btnMuteImage, Images.btnVolume1);
                    }

                }
            }
            get
            {
                return settings.IsMute;
            }
        }

        private int Volume
        {
            set
            {
                int FinalValue = value.LimitToRange(0, 200);

                SetSliderValue(MainOverlay.VolumeSlider.VolumeSlider, FinalValue);
                SetLabelContent(MainOverlay.VolumeSlider.labelVolume, FinalValue.ToString() + @"%");

                settings.Volume = FinalValue;

                if(mediaPlayer != null && mediaPlayer.Media != null)
                {
                    StartThread(() => { mediaPlayer.Volume = FinalValue; });
                }

                int cVolume = Volume;
                string cPic = MainOverlay.btnMuteImage.Source.ToString();

                if (cVolume >= 67 && !cPic.EndsWith(Images.btnVolume3))
                {
                    SetImage(MainOverlay.btnMuteImage, Images.btnVolume3);
                }
                else if (cVolume < 67 && cVolume >= 34 && !cPic.EndsWith(Images.btnVolume2))
                {
                    SetImage(MainOverlay.btnMuteImage, Images.btnVolume2);
                }
                else if (cVolume < 34 && !cPic.EndsWith(Images.btnVolume1))
                {
                    SetImage(MainOverlay.btnMuteImage, Images.btnVolume1);
                }

                if (Mute && !IsLoading) // IsLoading used so mute is loaded from settings
                    Mute = false;

                SetOverlay($"Volume {FinalValue}%");
            }

            get
            {
                return settings.Volume;
            }
        }

        private int Speed
        {
            set
            {
                int FinalSpeed = value.LimitToRange(1, 10);
                settings.Rate = FinalSpeed;

                if (mediaPlayer != null && mediaPlayer.Media != null)
                {
                    StartThread(() => { mediaPlayer.SetRate((float)FinalSpeed); });
                }
                
                SpeedText = $"Speed ({FinalSpeed}x)";
                SetOverlay($"Speed ({FinalSpeed}x");
            }
            get
            {
                return settings.Rate;
            }
        }

        private int Jump
        {
            set
            {
                int FinalJump = value.LimitToRange(1, 120);
                settings.Jump = FinalJump;

                JumpText = $"Jump ({ FinalJump} s)";
                SetOverlay($"Jump ({ FinalJump} s)");
            }

            get
            {
                return settings.Jump;
            }
        }

        public bool AutoPlay
        {
            set
            {
                settings.AutoPlay = value;

                string finalText = value ? "on" : "off";
                SetOverlay("AutoPlay " + finalText);
            }

            get
            {
                return settings.AutoPlay;
            }
        }

        private int AutoPlayTime
        {
            set
            {
                int FinalAutoPlay = value.LimitToRange(1, 120);
                settings.AutoPlayTime = FinalAutoPlay;

                AutoPlayText = $"AutoPlay ({FinalAutoPlay} s)";
                SetOverlay($"AutoPlay ({FinalAutoPlay} s)");
            }

            get
            {
                return settings.AutoPlayTime;
            }
        }

        private string AspectRatio
        {
            set
            {
                aspectRatio = value;
                mediaPlayer.AspectRatio = aspectRatio;

                SetOverlay("AspectRatio " + value);
            }

            get
            {
                return aspectRatio == string.Empty ? "default" : aspectRatio;
            }
        }

        private AudioType AudioMode
        {
            set
            {
                audioMode = value;

                if (audioMode == AudioType.None || audioMode == AudioType.Surround)
                    mediaPlayer.SetChannel(AudioOutputChannel.Dolbys);

                if (audioMode == AudioType.Stereo)
                    mediaPlayer.SetChannel(AudioOutputChannel.Stereo);

                SetOverlay($"AudioMode {value}");
            }

            get
            {
                return audioMode;
            }
        }

        private bool AutoAudioSelect 
        { 
            set
            {
                settings.AutoAudio = value;
                SetMenuItemChecked(MainOverlay.MenuSettingsAudioAutoSelect, value);
            }

            get
            {
                return settings.AutoAudio;
            }
        }

        private bool AutoSubtitleSelect
        {
            set
            {
                settings.AutoSubtitle = value;
                SetMenuItemChecked(MainOverlay.MenuSettingsSubtitleAutoSelect, value);
            }

            get
            {
                return settings.AutoSubtitle;
            }
        }

        private bool OnTop
        {
            set
            {
                onTop = value;
                SetTopMost(value);

                SetMenuItemChecked(MainOverlay.MenuSettingsOnTop, value);

                string finalValue = value ? "on" : "off";
                SetOverlay("AudioMode " + finalValue);
            }
            get
            {
                return onTop;
            }
        }

        private bool Acceleration
        {
            set
            {
                settings.Acceleration = value;
                SetMenuItemChecked(MainOverlay.MenuSettingsAcceleration, value);
            }
            get
            {
                //return settings.Acceleration;
                return false; // known bug, causes freeze on stop
            }
        }

        private bool GameMode
        {
            set
            {
                gameMode = value;
                SetMenuItemChecked(MainOverlay.MenuSettingsGameMode, value);

                if (value)
                {
                    if (keyboardHook != null)
                        keyboardHook.Dispose();

                    keyboardHook = new KeyboardHook();
                }
                else
                {
                    if (keyboardHook != null)
                        keyboardHook.Dispose();
                }

                string finalValue = value ? "on" : "off";
                SetOverlay("Game mode " + finalValue);
            }

            get
            {
                return gameMode;
            }
        }

        private bool TopOpen
        {
            set
            {
                if (value)
                {
                    MainOverlay.TopMenu.Visibility = Visibility.Visible;
                }
                else
                {
                    MainOverlay.TopMenu.Visibility = Visibility.Hidden;
                }
            }

            get
            {
                return MainOverlay.TopMenu.Visibility == Visibility.Visible;
            }
        }

        private bool BottomOpen
        {
            set
            {
                if (value)
                {
                    MainOverlay.BottomMenu.Visibility = Visibility.Visible;
                }
                else
                {
                    MainOverlay.BottomMenu.Visibility = Visibility.Hidden;
                }
            }

            get
            {
                return MainOverlay.BottomMenu.Visibility == Visibility.Visible;
            }
        }
        #endregion

        #region "Enums & Structs"
        private enum AudioType
        {
            Stereo = 0,
            Surround = 1,
            None = 2
        }

        private enum ShutDownType
        {
            Cancel = 0,
            After = 1,
            AfterN = 2,
            In = 3,
            End = 4,
            None = 5
        }

        internal struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                if (obj is Rect cObj)
                {
                    return (Left == cObj.Left && Top == cObj.Top && Right == cObj.Right && Bottom == cObj.Bottom);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return (Left.ToString() + Top.ToString() + Right.ToString() + Bottom.ToString()).GetHashCode();
            }

            public static bool operator ==(Rect left, Rect right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Rect left, Rect right)
            {
                return !(left == right);
            }
        }

        [StructLayout(LayoutKind.Sequential)] 
        internal struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                if (obj is POINT point)
                {
                    return (point.X == X && point.Y == Y);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return (X + Y).ToString().GetHashCode();
            }

            public static bool operator ==(POINT left, POINT right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(POINT left, POINT right)
            {
                return !(left == right);
            }

            public override string ToString()
            {
                return $"{X}x{Y}";
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] 
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;
        }
        #endregion

        #region "Internal Classes"
        internal static class Images
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

        internal class JumpCommand
        {
            public Direction direction;
            public int jump;

            public enum Direction
            {
                Backward = 0,
                Forward = 1
            }

            public JumpCommand(Direction direction, int jump)
            {
                this.direction = direction;
                this.jump = jump;
            }
        }

        private class ShutDownCommand
        {
            public ShutDownType shutDownType;
            public int Arg;

            public ShutDownCommand(ShutDownType shutDownType, int Arg)
            {
                this.shutDownType = shutDownType;
                this.Arg = Arg;
            }

        }

        internal static class ExternalCommands
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
            public const int WM_COPYDATA = 0x004A;
        }
        #endregion

        #region "Functions"
        #region "Timers"
        private void CreateJumpTimer()
        {
            // created a timer for jumping this was so you can go in any direcion without stutter
            System.Timers.Timer JumpTimer = new System.Timers.Timer()
            {
                Interval = 250
            };

            jumpCommands.Clear();

            JumpTimer.Elapsed += delegate
            {
                try
                {
                    if (jumpCommands.Count <= 0)
                        return;

                    bool isSeekable = mediaPlayer.IsSeekable;

                    if (isSeekable)
                    {
                        long finalJump = (long)jumpCommands.Where(x => x.direction == JumpCommand.Direction.Forward).ToList().Sum(x => x.jump);
                        finalJump -= (long)jumpCommands.Where(x => x.direction == JumpCommand.Direction.Backward).ToList().Sum(x => x.jump);
                        finalJump *= 1000;

                        jumpCommands.Clear();

                        long totalLenght = mediaPlayer.Media.Duration;
                        long currentLenght = mediaPlayer.Time;

                        long finalTime = 0;

                        int maxPlusValue = 500;
                        if (AutoPlay)
                            maxPlusValue = AutoPlayTime;

                        if (finalJump > 0 && (totalLenght - currentLenght) < (finalJump + maxPlusValue))
                        {
                            if (AutoPlay)
                            {
                                Next();
                                return;
                            }
                            else
                            {
                                finalTime = totalLenght - maxPlusValue;
                            }
                        }
                        else if (finalJump < 0 && currentLenght < finalJump)
                        {
                            finalTime = 0;
                        }
                        else
                        {
                            finalTime = currentLenght + finalJump;
                        }

                        if (finalJump == 0)
                            return;

                        StartThread(() =>
                        {
                            mediaPlayer.Time = finalTime;
                        });
                        
                        SetOverlay(TimeSpan.FromMilliseconds(mediaPlayer.Time).ToString(@"hh\:mm\:ss"));
                    }
                    else
                    {
                        jumpCommands.Clear();
                    }
                }
                catch { }
            };

            JumpTimer.Start();
        }

        private void CreateSaveTimer()
        {
            System.Timers.Timer SaveTimer = new System.Timers.Timer()
            {
                Interval = 2000
            };

            SaveTimer.Elapsed += delegate
            {
                if (settings.NeedsSaving)
                    settings.Save();

                if (mediaPlayer.IsPlaying || mediaPlayer.State == VLCState.Paused)
                    videoPosition.SavePosition(Convert.ToInt32(mediaPlayer.Time / 1000));
            };

            SaveTimer.Start();
        }

        private void CreateMouseTimer()
        {
            MouseTimer = new System.Windows.Forms.Timer
            {
                Interval = 150
            };

            int MouseOffset = 20;

            MouseTimer.Tick += delegate
            {
                if (gameMode)
                    return;

                if (GetCursorPos(out POINT p))
                {
                    bool XL = p.X >= screen.Bounds.X && p.X <= (screen.Bounds.Width + screen.Bounds.X - 1);

                    if (p.Y >= (screen.Bounds.Height - BottomSize - MouseOffset) && XL)
                    {
                        if (!BottomOpen)
                            BottomOpen = true;
                    }
                    else
                    {
                        if (BottomOpen)
                            BottomOpen = false;
                    }

                }
            };
        }
        #endregion

        #region "Set"
        private void SetLabelsColors()
        {
            LinearGradientBrush linearGradientBrush = new LinearGradientBrush();
            linearGradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(255, (byte)97, (byte)156, (byte)202), 0));
            linearGradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(255, (byte)91, (byte)135, (byte)184), 1));

            MainOverlay.labelLenght.Foreground = linearGradientBrush;
            MainOverlay.labelPosition.Foreground = linearGradientBrush;
        }

        private void SetImage(Image image, string newImage)
        {
            if (image.Dispatcher.CheckAccess()) // true
            {
                image.Source = ImageResource(newImage);
            }
            else // false
            {
                image.Dispatcher.Invoke(() =>
                {
                    image.Source = ImageResource(newImage);
                });
            }
        }

        private void SetLabelContent(Label label, string newText)
        {
            try
            {
                if (label.Dispatcher.CheckAccess()) // true
                {
                    label.Content = newText;
                }
                else // false
                {
                    label.Dispatcher.Invoke(() =>
                    {
                        label.Content = newText;
                    });
                }
            } catch { }
        }

        private void SetSliderMaximum(Slider slider, int newMaximum)
        {
            if (slider.Dispatcher.CheckAccess()) // true
            {
                slider.Maximum = newMaximum;
            }
            else // false
            {
                slider.Dispatcher.Invoke(() =>
                {
                    slider.Maximum = newMaximum;
                });
            }
        }

        private void SetSliderValue(Slider slider, int newValue)
        {
            try
            {
                if (slider.Dispatcher.CheckAccess()) // true
                {
                    slider.Value = newValue;
                }
                else // false
                {
                    slider.Dispatcher.Invoke(() =>
                    {
                        slider.Value = newValue;
                    });
                }

            } catch { }
        }

        private void SetMenuItemEnable(MenuItem menuItem, bool enabled)
        {
            try
            {
                if (menuItem.Dispatcher.CheckAccess()) // true
                {
                    menuItem.IsEnabled = enabled;
                }
                else // false
                {
                    menuItem.Dispatcher.Invoke(() =>
                    {
                        menuItem.IsEnabled = enabled;
                    });
                }
            }
            catch { }
        }

        private void SetMenuItemChecked(MenuItem menuItem, bool toCheck)
        {
            try
            {
                if (menuItem.Dispatcher.CheckAccess()) // true
                {
                    menuItem.IsChecked = toCheck;
                }
                else // false
                {
                    menuItem.Dispatcher.Invoke(() =>
                    {
                        menuItem.IsChecked = toCheck;
                    });
                }
            }
            catch { }
        }

        private void SetTopMost(bool newTopMost)
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.Topmost = newTopMost;
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.Topmost = newTopMost;
                });
            }
        }
        #endregion

        #region "ShutDown"
        private void UnCheckShutDown()
        {
            SetMenuItemChecked(MainOverlay.MenuSettingsShutDownAfterThis, false);
            SetMenuItemChecked(MainOverlay.MenuSettingsShutDownAfterN, false);
            SetMenuItemChecked(MainOverlay.MenuSettingsShutDownAfterTime, false);
            SetMenuItemChecked(MainOverlay.MenuSettingsShutDownEndPlaylist, false);
        }

        private void ShutDownNow()
        {
            try
            {
                StopMediaPlayer();
                Thread.Sleep(100);
            }
            catch
            { }

            try
            {
                var processStartInfo = new ProcessStartInfo("shutdown", "/s /f /t 0")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't shutdown", MessageCustomHandler.Style.Error, Buttons.OK, ex.ToString());
            }

        }

        private bool ShutDownSignal(ShutDownType shutDownMode, int Arg)
        {
            if (shutDownMode == ShutDownType.After)
            {
                ShutDownNow();
                return true;
            }
            else if (shutDownMode == ShutDownType.AfterN)
            {
                if (Arg <= 0)
                {
                    ShutDownNow();
                    return true;
                }
                else
                {
                    shutDownCmd.Arg = Arg - 1;
                }

                return false;
            }
            else if (shutDownMode == ShutDownType.End)
            {
                if (tvShow.episodeList.Last() == tvShow.GetCurrentEpisode())
                {
                    ShutDownNow();
                    return true;
                }

                return false;
            }
            else
            {
                return false;
            }
        }

        private void ShutDown(ShutDownType shutdownMode, int Arg = 0)
        {
            if (shutdownMode == ShutDownType.Cancel)
            {
                if (threadShutDown != null)
                {
                    try
                    {
                        threadShutDown.Abort();
                        Thread.Sleep(500);
                        threadShutDown = null;
                    }
                    catch
                    {
                        threadShutDown = null;
                    }
                }
                else
                {
                    threadShutDown = null;
                }

                shutDownCmd = new ShutDownCommand(ShutDownType.None, 0);
            }
            else if (shutdownMode == ShutDownType.In)
            {
                if (threadShutDown != null)
                {
                    try
                    {
                        threadShutDown.Abort();
                        Thread.Sleep(500);
                        threadShutDown = null;
                    }
                    catch
                    {
                        threadShutDown = null;
                    }
                }

                if (threadShutDown == null && shutdownMode == ShutDownType.In)
                {
                    threadShutDown = new Thread(() =>
                    {
                        {
                            if (Arg <= 2)
                            {
                                ShutDownNow();
                            }
                            else
                            {
                                DateTime endDate = DateTime.Now.Add(TimeSpan.FromSeconds(Arg));

                                while (true)
                                {
                                    Thread.Sleep(2000);

                                    if (endDate.Subtract(DateTime.Now).TotalSeconds <= 0)
                                    {
                                        ShutDownNow();
                                        break;
                                    }
                                }
                            }
                        }
                    })
                    {
                        IsBackground = true
                    };
                    threadShutDown.SetApartmentState(ApartmentState.STA);
                    threadShutDown.Start();
                }

                shutDownCmd = new ShutDownCommand(shutdownMode, Arg);
            }
            else
            {
                shutDownCmd = new ShutDownCommand(shutdownMode, Arg);
            }
        }
        #endregion

        public MainWindow()
        {
            System.Windows.Forms.Application.EnableVisualStyles();

            AppName = Process.GetCurrentProcess().ProcessName;
            string[] Args = App.Args;

            // TO DO add more arguments
            StartThread(() =>
            {
                ProcessArgs(Args);
            });
            
            InitializeComponent();

            MainOverlay = new PlayerOverlay(WinHost, this);
            AddHandlers();

            DataContext = this;
            PlayBtnTxt = "Play";

            settings = new Settings();
            settings.Load();

            string vlcPath = AppDomain.CurrentDomain.BaseDirectory;
            if (Environment.Is64BitProcess) { vlcPath += @"\libvlc\win-x64"; } else { vlcPath += @"\libvlc\win-x86"; }

            Core.Initialize(vlcPath);
            //Core.Initialize();
            CreateMediaPlayer();

            Volume = settings.Volume;
            Mute = settings.IsMute;
            Speed = settings.Rate;
            Jump = settings.Jump;
            AutoPlay = settings.AutoPlay;
            AutoPlayTime = settings.AutoPlayTime;
            AutoAudioSelect = settings.AutoAudio;
            AutoSubtitleSelect = settings.AutoSubtitle;
            Acceleration = settings.Acceleration;

            jumpCommands = new List<JumpCommand>();
            SetLabelsColors();

            this.Loaded += MainWindow_Loaded;
            this.ContentRendered += MainWindow_ContentRendered;
            this.StateChanged += MainWindow_StateChanged;
            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);

            tvShow = new TvShow();

            MainOverlay.MenuPlaylistNext.IsEnabled = false;
            MainOverlay.MenuPlaylistPrevious.IsEnabled = false;

            shutDownCmd = new ShutDownCommand(ShutDownType.None, 0);

            videoPosition = new VideoPosition(videoPositionDir);

            recents = new Recents(recentsPath);
        }

        private void AddHandlers()
        {
            //Input handlers
            MainOverlay.KeyDown += MainWindow_KeyDown;
            MainOverlay.MouseWheel += MainWindow_MouseWheel;

            //Bottom media controls
            MainOverlay.btnPlay.Click += BtnPlay_Click;
            MainOverlay.btnBackward.Click += BtnBackward_Click;
            MainOverlay.btnStop.Click += BtnStop_Click;
            MainOverlay.btnForward.Click += BtnForward_Click;
            MainOverlay.btnOpenFile.Click += BtnOpenFile_Click;

            MainOverlay.SliderDragStarted += SliderMedia_DragStarted;
            MainOverlay.SliderDragCompleted += SliderMedia_DragCompleted;
            MainOverlay.SliderMouseEnter += SliderMedia_MouseEnter;

            MainOverlay.btnMute.Click += BtnMute_Click;
            MainOverlay.btnFullscreen.Click += BtnFullscreen_Click;

            //Top menu controls
            MainOverlay.MenuFileMediaInfo.Click += MenuFileMediaInfo_Click;
            MainOverlay.MenuFileScreenShot.Click += MenuFileScreenShot_Click;
            MainOverlay.MenuFileOpenFile.Click += BtnOpenFile_Click;
            MainOverlay.MenuFileQuit.Click += MenuFileQuit_Click;

            MainOverlay.MenuPlaybackPlay.Click += BtnPlay_Click;
            MainOverlay.MenuPlaybackStop.Click += BtnStop_Click;
            MainOverlay.MenuPlaybackBackward.Click += BtnBackward_Click;
            MainOverlay.MenuPlaybackForward.Click += BtnForward_Click;
            MainOverlay.MenuPlaybackVolumeUp.Click += MenuPlaybackVolumeUp_Click;
            MainOverlay.MenuPlaybackVolumeDown.Click += MenuPlaybackVolumeDown_Click;
            MainOverlay.MenuPlaybackMute.Click += BtnMute_Click;
            MainOverlay.SpeedChanged += SliderSpeed_ValueChanged;
            MainOverlay.SpeedLoaded += SliderSpeed_Loaded;
            MainOverlay.JumpChanged += SliderJump_ValueChanged;
            MainOverlay.JumpLoaded += SliderJump_Loaded;

            MainOverlay.MenuSettingsVideoAspectRatio43.Click += MenuSettingsVideoAspectRatio_Click;
            MainOverlay.MenuSettingsVideoAspectRatio54.Click += MenuSettingsVideoAspectRatio_Click;
            MainOverlay.MenuSettingsVideoAspectRatio169.Click += MenuSettingsVideoAspectRatio_Click;
            MainOverlay.MenuSettingsVideoAspectRatio1610.Click += MenuSettingsVideoAspectRatio_Click;
            MainOverlay.MenuSettingsVideoAspectRatio189.Click += MenuSettingsVideoAspectRatio_Click;
            MainOverlay.MenuSettingsVideoAspectRatio219.Click += MenuSettingsVideoAspectRatio_Click;
            MainOverlay.MenuSettingVideoAspectRatioCustom.Click += MenuSettingsVideoAspectRatio_Click;
            MainOverlay.MenuSettingVideoAspectRatioReset.Click += MenuSettingsVideoAspectRatio_Click;

            MainOverlay.MenuSettingsAudioModeStereo.Click += MenuSettingsAudioMode_Click;
            MainOverlay.MenuSettingsAudioModeSurrond.Click += MenuSettingsAudioMode_Click;
            MainOverlay.MenuSettingsAudioAutoSelect.Checked += MenuSettingsAudioAutoSelect_Checked;
            MainOverlay.MenuSettingsAudioAutoSelect.Unchecked += MenuSettingsAudioAutoSelect_Checked;

            MainOverlay.MenuSettingsSubtitleAdd.Click += MenuSettingsSubtitleAdd_Click;
            MainOverlay.MenuSettingsSubtitleAutoSelect.Checked += MenuSettingsSubtitleAutoSelect_Checked;
            MainOverlay.MenuSettingsSubtitleAutoSelect.Unchecked += MenuSettingsSubtitleAutoSelect_Checked;

            MainOverlay.MenuSettingsOnTop.Click += MenuSettingsOnTop_Click;
            MainOverlay.MenuSettingsAcceleration.Click += MenuSettingsAcceleration_Click;
            MainOverlay.MenuSettingsGameMode.Click += MenuSettingsGameMode_Click;

            MainOverlay.MenuSettingsShutDownAfterThis.Click += MenuSettingsShutDown_Click;
            MainOverlay.MenuSettingsShutDownAfterN.Click += MenuSettingsShutDown_Click;
            MainOverlay.MenuSettingsShutDownAfterTime.Click += MenuSettingsShutDown_Click;
            MainOverlay.MenuSettingsShutDownEndPlaylist.Click += MenuSettingsShutDown_Click;

            MainOverlay.MenuPlaylistAutoplay.Click += MenuPlaylistAutoplay_Click;
            MainOverlay.MenuPlaylistAutoplay.Loaded += MenuPlaylistAutoplay_Loaded;

            MainOverlay.AutoPlayChanged += SliderAutoplay_ValueChanged;
            MainOverlay.AutoPlayLoaded += SliderAutoplay_Loaded;

            MainOverlay.MenuPlaylistNext.Click += MenuPlaylistNext_Click;
            MainOverlay.MenuPlaylistPrevious.Click += MenuPlaylistPrevious_Click;

            MainOverlay.MenuPlaylistVideoList.Click += MenuPlaylistVideoList_Click;

            MainOverlay.MenuAbout.Click += MenuAbout_Click;
        }

        private void ProcessArgs(string[] Args)
        {
            if (Args != null)
            {
                if (Args.Count() != 1)
                    return;

                bool FileFound = File.Exists(Args[0]);

                if (FileFound == false)
                    return;

                if (IsRunning())
                {
                    SendFile(Args[0]);
                    this.Close();
                    return;
                }
                else
                {
                    StartThread(() =>
                    {
                        while (IsLoading)
                        {
                            Thread.Sleep(200);
                        }

                        Thread.Sleep(500);

                        OpenFile(Args[0]);
                    });
                }
            }
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg >= ExternalCommands.PM_PLAY && msg <= ExternalCommands.PM_AUTOPLAY)
            {
                int Command = msg;
                int Arg = (Int32)wParam;

                ProcessExternalCommand(Command, Arg);
            }
            else if (msg == ExternalCommands.WM_COPYDATA)
            {
                if (wParam == IntPtr.Zero)
                {
                    COPYDATASTRUCT cd = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));
                    string file = cd.lpData;

                    if (File.Exists(file))
                    OpenFile(file);
                }
            }

            return IntPtr.Zero;
        }

        private bool IsRunning()
        {
            try
            {
                return (Process.GetProcessesByName(AppName).Count() > 1);
            }
            catch
            {
                return false;
            }
        }

        private void StartThread(ThreadStart newStart)
        {
            Thread newThread = new Thread(newStart)
            {
                IsBackground = true
            };
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.Start();
        }
                    
        private int GetRandom(int Min, int Max)
        {
            try
            {
                if (Min > Max)
                    return random.Next(Max, Min + 1);

                if (Min == Max)
                    return Min;

                return random.Next(Min, Max + 1);
            }
            catch
            {
                return Min;
            }
        }

        private bool IsNumeric(string Input)
        {
            return int.TryParse(Input, out _);
        }

        private bool HasRegexMatch(string ToCompare, string RegexMatch)
        {
            return Regex.IsMatch(ToCompare, RegexMatch, RegexOptions.IgnoreCase);
        }
        
        private Screen GetCurrentScreen()
        {
            return Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
        }

        private string WithMaxLength(string Value, int maxLength)
        {
            return Value?.Substring(0, Math.Min(Value.Length, maxLength));
        }
        #endregion

        #region "MediaPlayer"
        private int IdFromTrackName(string Name)
        {
            try
            {
                return Convert.ToInt32(Name.Split("[".ToCharArray()).Last().Split("]".ToCharArray()).First());
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't get id from name, Error: " + ex.Message, MessageCustomHandler.Style.Error, Buttons.OK, ex.ToString());
                return -1;
            }
        }

        private void NewFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Title = "Open new media",
                Multiselect = true,
                Filter = "Video Files|" + string.Join(";", Extensions.Video.Select(x => @"*" + x)) + "|All files|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (openFileDialog.FileNames.Count() != 1)
                {
                    CMBox.Show("Error", "Multiselect is not supported yet", MessageCustomHandler.Style.Error, Buttons.OK);
                    return;
                }

                OpenFile(openFileDialog.FileName);
            }
        }

        public void OpenFile(string FilePath)
        {
            if (Extensions.IsVideo(FilePath))
            {
                //StopMediaPlayer();

                StartThread(() =>
                {
                    Media media = new Media(libVLC, FilePath, FromType.FromPath);

                    if (settings.Acceleration == false)
                        media.AddOption(@":avcodec-hw=none");

                    mediaPlayer.Play(media);
                });
            }
        }

        public void ReOpenFile()
        {
            if (mediaPlayer.Media != null)
            {
                FileInfo currentFile = new FileInfo(System.Net.WebUtility.UrlDecode(new Uri(mediaPlayer.Media.Mrl).AbsolutePath));

                if (currentFile.Exists == false)
                    return;

                StopMediaPlayer();

                OpenFile(currentFile.FullName);
            }
        }

        public void LoadSubtitle(string FilePath)
        {
            if (mediaPlayer.Media == null)
                return;

            mediaPlayer.AddSlave(MediaSlaveType.Subtitle, new Uri(FilePath).ToString(), true);

            Thread.Sleep(1000);

            // Track name
            string TrackName = string.Empty;
            int TrackID = mediaPlayer.Spu;

            TrackName = mediaPlayer.SpuDescription[mediaPlayer.SpuCount - 1].Name;

            if (string.IsNullOrWhiteSpace(TrackName))
                TrackName = "Track";

            TrackName += $" [{TrackID}]";

            // Add track to menu
            MenuItem newTrack = new MenuItem { Header = TrackName, Style = MainOverlay.MenuSettingsVideoTracks.Style };

            newTrack.Click += (sender, e) =>
            {
                MenuItem menuItem = (MenuItem)sender;
                int newSPU = IdFromTrackName(menuItem.Header.ToString());

                mediaPlayer.SetSpu(newSPU);
            };

            newTrack.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
            this.MainOverlay.MenuSettingsSubtitleTracks.Items.Add(newTrack);

        }

        private void SendFile(string file)
        {
            foreach (var p in Process.GetProcessesByName(AppName))
            {
                if (p.MainWindowHandle == Process.GetCurrentProcess().MainWindowHandle) continue;
                {
                    COPYDATASTRUCT cd = new COPYDATASTRUCT
                    {
                        lpData = file,
                        dwData = p.MainWindowHandle
                    };
                    cd.cbData = cd.lpData.Length + 1;

                    SendMessage(p.MainWindowHandle, ExternalCommands.WM_COPYDATA, IntPtr.Zero, ref cd);
                }
            }
        }

        private int ProcessExternalCommand(int Command, int Arg)
        {
            try
            {
                switch (Command)
                {
                    case ExternalCommands.PM_PLAY:
                        {
                            Play(true);
                            break;
                        }

                    case ExternalCommands.PM_PAUSE:
                        {
                            Pause();
                            break;
                        }

                    case ExternalCommands.PM_STOP:
                        {
                            StopMediaPlayer();
                            break;
                        }

                    case ExternalCommands.PM_FORWARD:
                        {
                            JumpForward();
                            break;
                        }

                    case ExternalCommands.PM_BACKWARD:
                        {
                            JumpBackward();
                            break;
                        }

                    case ExternalCommands.PM_NEXT:
                        {
                            Next();
                            break;
                        }

                    case ExternalCommands.PM_PREVIOUS:
                        {
                            Previous();
                            break;
                        }

                    case ExternalCommands.PM_VOLUMEUP:
                        {
                            Volume += Arg;
                            break;
                        }

                    case ExternalCommands.PM_VOLUMEDOWN:
                        {
                            Volume -= Arg;
                            break;
                        }

                    case ExternalCommands.PM_MUTE:
                        {
                            Mute = !Mute;
                            break;
                        }

                    case ExternalCommands.PM_AUTOPLAY:
                        {
                            AutoPlay = !AutoPlay;
                            break;
                        }
                }

                return 0;
            }
            catch (Exception ex)
            {
                StartThread(() => { CMBox.Show("Error", "Couldn't process external command, Error: " + ex.Message, MessageCustomHandler.Style.Error, Buttons.OK, ex.ToString()); });
                return 0;
            }
        }

        private void CreateMediaPlayer()
        {
            // VLC init
            libVLC = new LibVLC();

            mediaPlayer = new MediaPlayer(libVLC)
            {
                EnableMouseInput = false
            };

            mediaPlayer.SetVideoTitleDisplay(Position.Bottom, 3000);

            // media player events
            mediaPlayer.Playing += MediaPlayer_Playing;
            mediaPlayer.Paused += MediaPlayer_Paused;
            mediaPlayer.MediaChanged += MediaPlayer_MediaChanged;
            mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            mediaPlayer.EndReached += MediaPlayer_EndReached;
            mediaPlayer.Stopped += MediaPlayer_Stopped;

            // WinForm styles
            LibVLCSharp.WinForms.VideoView videoView = new LibVLCSharp.WinForms.VideoView()
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Black
            };
            videoView.MediaPlayer = mediaPlayer;

            // context menu
            PlayerContextMenu = CreatePlayerMenu();

            // overlay panel for context menu and double click fullscreen
            TransparentPanel overlayPanel = new TransparentPanel()
            {
                Dock = DockStyle.Fill,
                AllowDrop = true
            };

            overlayPanel.ContextMenuStrip = PlayerContextMenu;
            overlayPanel.MouseDoubleClick += delegate { BtnFullscreen_Click(null, null); };
            overlayPanel.MouseWheel += OverlayPanel_MouseWheel;
            overlayPanel.DragOver += OverlayPanel_DragOver;
            overlayPanel.DragDrop += OverlayPanel_DragDrop;

            // add everything to win host
            System.Windows.Forms.Panel videoPanel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Black
            };
            videoPanel.Controls.Add(overlayPanel);
            videoPanel.Controls.Add(videoView);

            WinHost.Child = videoPanel;
        }

        private ContextMenuStrip CreatePlayerMenu()
        {
            PlayerContextMenu = new ContextMenuStrip()
            {
                Renderer = new PlayerMenuRenderer(),
                ForeColor = System.Drawing.Color.FromArgb(78, 173, 254)
            };

            PlayerContextMenu.Items.Add("Media Info", Properties.Resources.btnMediaInfo, delegate { MenuFileMediaInfo_Click(null, null); });
            PlayerContextMenu.Items.Add("Screenshot", Properties.Resources.btnScreenShot, delegate { MenuFileScreenShot_Click(null, null); });
            PlayerContextMenu.Items.Add("Open...", Properties.Resources.btnOpen, delegate { NewFile(); });
            PlayerContextMenu.Items.Add(new ToolStripSeparator()); //////////////
            PlayerContextMenu.Items.Add("Play/Pause", Properties.Resources.btnPlay, delegate { BtnPlay_Click(null, null); });
            PlayerContextMenu.Items.Add("Stop", Properties.Resources.btnStop, delegate { StopMediaPlayer(); });
            PlayerContextMenu.Items.Add("Forward", Properties.Resources.btnForward, delegate { JumpForward(); });
            PlayerContextMenu.Items.Add("Backward", Properties.Resources.btnBackward, delegate { JumpBackward(); });
            PlayerContextMenu.Items.Add(new ToolStripSeparator()); //////////////
            PlayerContextMenu.Items.Add("Volume Up", Properties.Resources.BtnVolume3, delegate { Volume += 5; });
            PlayerContextMenu.Items.Add("Volume Down", Properties.Resources.BtnVolume1, delegate { Volume -= 5; });
            PlayerContextMenu.Items.Add("Mute", Properties.Resources.btnMute, delegate { Mute = !Mute; });
            PlayerContextMenu.Items.Add(new ToolStripSeparator()); //////////////

            ToolStripMenuItem SettingsMenuVideoAR = new ToolStripMenuItem("Aspect Ratio", Properties.Resources.btnSet)
            { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };

            SettingsMenuVideoAR.DropDownItems.Add("4:3", null, (s, e) => { MenuSettingsVideoAspectRatio_Click(s, null); });
            SettingsMenuVideoAR.DropDownItems.Add("5:4", null, (s, e) => { MenuSettingsVideoAspectRatio_Click(s, null); });
            SettingsMenuVideoAR.DropDownItems.Add("16:9", null, (s, e) => { MenuSettingsVideoAspectRatio_Click(s, null); });
            SettingsMenuVideoAR.DropDownItems.Add("16:10", null, (s, e) => { MenuSettingsVideoAspectRatio_Click(s, null); });
            SettingsMenuVideoAR.DropDownItems.Add("18:9", null, (s, e) => { MenuSettingsVideoAspectRatio_Click(s, null); });
            SettingsMenuVideoAR.DropDownItems.Add("21:9", null, (s, e) => { MenuSettingsVideoAspectRatio_Click(s, null); });
            SettingsMenuVideoAR.DropDownItems.Add(new ToolStripSeparator());
            SettingsMenuVideoAR.DropDownItems.Add("Custom", null, (s, e) => { MenuSettingsVideoAspectRatio_Click(s, null); });
            SettingsMenuVideoAR.DropDownItems.Add("Reset", null, (s, e) => { MenuSettingsVideoAspectRatio_Click(s, null); });

            foreach (ToolStripItem item in SettingsMenuVideoAR.DropDownItems) { item.ForeColor = SettingsMenuVideoAR.ForeColor; }

            SettingsMenuVideoTrack = new ToolStripMenuItem("Sub Track", Properties.Resources.btnSelectTrack)
            { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };

            ToolStripMenuItem SettingsMenuVideo = new ToolStripMenuItem("Video", Properties.Resources.btnVideo) { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };
            SettingsMenuVideo.DropDownItems.AddRange(new[] { SettingsMenuVideoAR, SettingsMenuVideoTrack });

            ToolStripMenuItem SettingsMenuAudioMode = new ToolStripMenuItem("Mode", Properties.Resources.btnSet)
            { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };

            SettingsMenuAudioMode.DropDownItems.Add("Stereo", null, (s, e) => { MenuSettingsAudioMode_Click(s, null); });
            SettingsMenuAudioMode.DropDownItems.Add("Surround", null, (s, e) => { MenuSettingsAudioMode_Click(s, null); });

            foreach (ToolStripItem item in SettingsMenuAudioMode.DropDownItems) { item.ForeColor = SettingsMenuVideoAR.ForeColor; }

            SettingsMenuAudioTrack = new ToolStripMenuItem("Sub Track", Properties.Resources.btnSelectTrack)
            { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };

            ToolStripMenuItem SettingsMenuAudio = new ToolStripMenuItem("Audio", Properties.Resources.btnAudio) { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };
            SettingsMenuAudio.DropDownItems.AddRange(new ToolStripMenuItem[] { SettingsMenuAudioMode, SettingsMenuAudioTrack });

            SettingsMenuSubtitleTrack = new ToolStripMenuItem("Sub Track", Properties.Resources.btnSelectTrack)
            { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };

            ToolStripMenuItem SettingsMenuSubtitle = new ToolStripMenuItem("Subtitle", Properties.Resources.btnSubtitle) { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };

            SettingsMenuSubtitle.DropDownItems.Add("Add Subtitle", Properties.Resources.btnAdd, delegate { MenuSettingsSubtitleAdd_Click(null, null); });
            SettingsMenuSubtitle.DropDownItems.Add(SettingsMenuSubtitleTrack);

            foreach (ToolStripItem item in SettingsMenuSubtitle.DropDownItems) { item.ForeColor = SettingsMenuVideoAR.ForeColor; }

            ToolStripMenuItem SettingsMenu = new ToolStripMenuItem("Settings", Properties.Resources.btnSettings);
            SettingsMenu.DropDownItems.AddRange(new[] { SettingsMenuVideo, SettingsMenuAudio, SettingsMenuSubtitle });

            PlayerContextMenu.Items.Add(SettingsMenu);

            PlayerContextMenu.Items.Add(new ToolStripSeparator()); //////////////

            ToolStripMenuItem SettingsPlaylist = new ToolStripMenuItem("Playlist", Properties.Resources.btnPlaylist)
            { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };

            SettingsPlaylist.DropDownItems.Add("Next", Properties.Resources.btnNext, delegate { Next(); });
            SettingsPlaylist.DropDownItems.Add("Previous", Properties.Resources.btnPrevious, delegate { Previous(); });
            SettingsPlaylist.DropDownItems.Add("Video List", Properties.Resources.btnVideoList, delegate { MenuPlaylistVideoList_Click(null, null); });

            SettingsPlaylist.DropDownOpening += delegate
            {
                SettingsPlaylist.DropDownItems[0].Enabled = tvShow.HasNextEpisode();
                SettingsPlaylist.DropDownItems[1].Enabled = tvShow.HasPreviousEpisode();
            };

            foreach (ToolStripItem item in SettingsPlaylist.DropDownItems) { item.ForeColor = SettingsMenuVideoAR.ForeColor; }

            PlayerContextMenu.Items.Add(SettingsPlaylist);
            PlayerContextMenu.Items.Add(new ToolStripSeparator()); //////////////
            PlayerContextMenu.Items.Add("Quit", Properties.Resources.btnQuit, delegate { this.Close(); });

            return PlayerContextMenu;
        }

        private void ProcessDrop(string[] Files)
        {
            if (Files.Count() != 1)
                return;

            bool videoLoaded = false;

            foreach (string Path in Files)
            {
                if (!File.Exists(Path))
                    continue;

                if (Extensions.IsSubtitle(Path))
                {
                    LoadSubtitle(Path);
                }
                else if (Extensions.IsVideo(Path))
                {
                    if (videoLoaded == false)
                        OpenFile(Path);
                }
            }
        }

        private void ProcessShow(string FileName)
        {
            StartThread(() =>
            {
                tvShow.Load(FileName);

                SetMenuItemEnable(MainOverlay.MenuPlaylistNext, tvShow.HasNextEpisode());
                SetMenuItemEnable(MainOverlay.MenuPlaylistPrevious, tvShow.HasPreviousEpisode());

                this.Dispatcher.Invoke(delegate { videoListWindow.SetTvShow(tvShow); });
            });
        }

        private void SetOverlay(string newOverlay)
        {
            if (IsLoading)
                return;

            MainOverlay.SetOverlayText(newOverlay);
        }

        private void Pause()
        {
            try
            {
                if (mediaPlayer.CanPause)
                    mediaPlayer.Pause();
            }
            catch
            { }
        }

        private void Play(bool OpenRecent)
        {
            try
            {
                if (mediaPlayer.State == VLCState.Paused)
                {
                    mediaPlayer.Play();
                }
                else
                {
                    if (OpenRecent && recents.GetList().Count > 0)
                    {
                        OpenFile(recents.GetList().Last());
                    }
                }
                    
            }
            catch
            { }
        }

        private void StopMediaPlayer() // Bug: can freeze the app, maybe workaround?
        {
            this.Dispatcher.Invoke(() =>
            {
                MainOverlay.IsEnabled = false;
                PlayerContextMenu.Enabled = false;

                if (mediaPlayer.State == VLCState.Paused)
                    mediaPlayer.Play();
            });

            StartThread(() =>
            {
                Thread.Sleep(200);

                this.mediaPlayer.Stop();
                this.mediaPlayer.Media = null;

                SetOverlay("Stopped");

                this.Dispatcher.Invoke(() =>
                {
                    MainOverlay.IsEnabled = true;
                    PlayerContextMenu.Enabled = true;
                });
            });
        }

        private void Next()
        {
            if (tvShow.HasNextEpisode() && tvShow.NextEpisode().IsTvShow)
                OpenFile(tvShow.NextEpisode().FilePath);
        }

        private void Previous()
        {
            if (tvShow.HasPreviousEpisode() && tvShow.PreviousEpisode().IsTvShow)
                OpenFile(tvShow.PreviousEpisode().FilePath);
        }

        private void JumpForward()
        {
            if (mediaPlayer.IsSeekable)
            {
                jumpCommands.Add(new JumpCommand(JumpCommand.Direction.Forward, Jump));
                //StartThread(() => { mediaPlayer.Time += Jump * 1000; });
            }
        }

        private void JumpBackward()
        {
            if (mediaPlayer.IsSeekable)
            {
                jumpCommands.Add(new JumpCommand(JumpCommand.Direction.Backward, Jump));
                //StartThread(() => { mediaPlayer.Time -= Jump * 1000; });
            }
        }

        private BitmapImage ImageResource(string pathInApplication, Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            if (pathInApplication[0] == '/')
            {
                pathInApplication = pathInApplication.Substring(1);
            }
            return new BitmapImage(new Uri(@"pack://application:,,,/" + assembly.GetName().Name + ";component/" + pathInApplication, UriKind.Absolute));
        }

        private void RefreshRecentsMenu()
        {
            try
            {
                if (MainOverlay.MenuPlaylistRecent.HasItems)
                    MainOverlay.MenuPlaylistRecent.Items.Clear();

                List<string> RecentList = recents.GetList();

                if (RecentList.Count > 0)
                {
                    // Foreground color
                    var brush = new SolidColorBrush(Color.FromArgb(255, (byte)78, (byte)173, (byte)254));

                    // File list
                    foreach (string Item in RecentList)
                    {
                        string name = new FileInfo(Item).Name;

                        MenuItem menuRecent = new MenuItem
                        {
                            Header = WithMaxLength(name, 30),
                            Style = MainOverlay.MenuPlaylistRecent.Style,
                            Foreground = brush,
                        };
                        menuRecent.Click += (sender, e) => { OpenFile(Item); };

                        MainOverlay.MenuPlaylistRecent.Items.Add(menuRecent);
                    }

                    // Separator
                    MenuItem newSeparator = new MenuItem
                    {
                        Style = MainOverlay.MenuPlaylistSeparator01.Style,
                        BorderThickness = MainOverlay.MenuPlaylistSeparator01.BorderThickness,
                        Background = MainOverlay.MenuPlaylistSeparator01.Background,
                        Margin = new Thickness(0),
                        MinWidth = 115,
                        Height = 2
                    };
                    MainOverlay.MenuPlaylistRecent.Items.Add(newSeparator);

                    // Clear Recents
                    MenuItem menuClearRecents = new MenuItem
                    {
                        Header = "Clear",
                        Style = MainOverlay.MenuPlaylistRecent.Style,
                        Name = "ClearR",
                        Foreground = brush,
                        Icon = new Image { Source = ImageResource(Images.btnTrash) }
                    };
                    menuClearRecents.Click += (sender, e) =>
                    {
                        recents.ClearRecent();
                        RefreshRecentsMenu();
                    };
                    MainOverlay.MenuPlaylistRecent.Items.Add(menuClearRecents);
                }
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't refresh recents, Error: " + ex.Message, MessageCustomHandler.Style.Error, Buttons.OK, ex.ToString());
            }
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                PlayBtnTxt = "Pause";
            });

            SetOverlay("Playing");
        }

        private void MediaPlayer_Paused(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                PlayBtnTxt = "Play";
            });

            SetOverlay("Paused");
        }

        private void MediaPlayer_MediaChanged(object sender, MediaPlayerMediaChangedEventArgs e)
        {
            bool AudioSelected = false;
            bool SubtitleSelected = false;

            StartThread(() =>
            {
                ShutDownSignal(shutDownCmd.shutDownType, shutDownCmd.Arg);

                try
                {
                    int InSleep = 0;

                    // Need to wait for the tracks to load
                    while (mediaPlayer.Media.Tracks.Count() <= 0 && mediaPlayer.Media.Duration <= 0 && InSleep <= 15)
                    {
                        Thread.Sleep(200);
                        InSleep += 1;
                    }
                }
                catch
                {
                    Thread.CurrentThread.Abort();
                }
                
                FileInfo currentFile = new FileInfo(System.Net.WebUtility.UrlDecode(new Uri(e.Media.Mrl).AbsolutePath));

                ProcessShow(currentFile.FullName);

                SetLabelContent(MainOverlay.labelTitle, currentFile.Name);
                SetLabelContent(MainOverlay.labelLenght, TimeSpan.FromMilliseconds(mediaPlayer.Media.Duration).ToString(@"hh\:mm\:ss"));
                SetSliderValue(MainOverlay.SliderMedia, 0);
                SetSliderMaximum(MainOverlay.SliderMedia, Convert.ToInt32(mediaPlayer.Media.Duration / 1000));

                // Re-set media settings
                if (Mute)
                {
                    mediaPlayer.Mute = true;
                }
                else
                {
                    mediaPlayer.Volume = Volume;
                }

                mediaPlayer.SetRate((float)Speed);

                // Things that need to run on the main thread
                this.Dispatcher.Invoke(() =>
                {
                    this.MainOverlay.MenuSettingsVideoTracks.Items.Clear();
                    this.MainOverlay.MenuSettingsAudioTracks.Items.Clear();
                    this.MainOverlay.MenuSettingsSubtitleTracks.Items.Clear();

                    MenuItem menuDisableVideo = new MenuItem { Header = "Disable", Style = MainOverlay.MenuSettingsVideoTracks.Style, Name = "VideoD" };
                    menuDisableVideo.Click += MenuTrackDisable_Click;

                    MenuItem menuDisableAudio = new MenuItem { Header = "Disable",  Style = MainOverlay.MenuSettingsVideoTracks.Style, Name = "AudioD" };
                    menuDisableAudio.Click += MenuTrackDisable_Click;

                    MenuItem menuDisableSubtitle = new MenuItem { Header = "Disable", Style = MainOverlay.MenuSettingsVideoTracks.Style, Name = "SubtitleD"};
                    menuDisableSubtitle.Click += MenuTrackDisable_Click;

                    menuDisableVideo.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
                    menuDisableAudio.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
                    menuDisableSubtitle.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));

                    this.MainOverlay.MenuSettingsVideoTracks.Items.Add(menuDisableVideo);
                    this.MainOverlay.MenuSettingsAudioTracks.Items.Add(menuDisableAudio);

                    SettingsMenuVideoTrack.DropDownItems.Clear();
                    SettingsMenuAudioTrack.DropDownItems.Clear();
                    SettingsMenuSubtitleTrack.DropDownItems.Clear();

                    SettingsMenuVideoTrack.DropDownItems.Add("Disable Video", null, (s, e) => { MenuTrackDisable_Click(s, null); });
                    SettingsMenuAudioTrack.DropDownItems.Add("Disable Audio", null, (s, e) => { MenuTrackDisable_Click(s, null); });

                    videoPosition.SetNewFile(currentFile.Name, Convert.ToInt32(mediaPlayer.Media.Duration / 1000));

                    long currentPosition = videoPosition.GetPosition();

                    if(currentPosition != 0)
                    {
                        StartThread(() =>
                        {
                            Thread.Sleep(1000);
                            mediaPlayer.Time = currentPosition;
                        });
                    }

                    recents.AddRecent(currentFile.FullName);
                    RefreshRecentsMenu();
                });

                // Load tracks
                foreach(MediaTrack mediaTrack in e.Media.Tracks)
                {
                    try
                    {
                        // Track name - doesn't matter what type it is
                        string TrackName = string.Empty;
                        int TrackID = mediaTrack.Id;

                        if (mediaTrack.Description != null && string.IsNullOrWhiteSpace(mediaTrack.Description) == false && mediaTrack.Description != "und")
                        {
                            TrackName = mediaTrack.Description;

                            if (mediaTrack.Language != null && string.IsNullOrWhiteSpace(mediaTrack.Language) == false && mediaTrack.Language != "und")
                            {
                                TrackName += @" - " + mediaTrack.Language;
                            }
                        }
                        else
                        {
                            if (mediaTrack.Language != null && string.IsNullOrWhiteSpace(mediaTrack.Language) == false && mediaTrack.Language != "und")
                            {
                                TrackName = mediaTrack.Language;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(TrackName))
                            TrackName = "Track";

                        switch (mediaTrack.TrackType)
                        {
                            // Video track
                            case TrackType.Video:
                                {
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        MenuItem newTrack = new MenuItem { Header = TrackName, Style = MainOverlay.MenuSettingsVideoTracks.Style };

                                        newTrack.Click += delegate
                                        {
                                            mediaPlayer.SetVideoTrack(TrackID);

                                            SetOverlay("New video track");
                                        };

                                        newTrack.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
                                        this.MainOverlay.MenuSettingsVideoTracks.Items.Add(newTrack);

                                        SettingsMenuVideoTrack.DropDownItems.Add(TrackName, null, (s, e) => { mediaPlayer.SetVideoTrack(TrackID); SetOverlay("New video track"); });
                                    });

                                    break;
                                }

                            // Audio track
                            case TrackType.Audio:
                                {
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        MenuItem newTrack = new MenuItem { Header = TrackName, Style = MainOverlay.MenuSettingsVideoTracks.Style };

                                        newTrack.Click += delegate
                                        {
                                            mediaPlayer.SetAudioTrack(TrackID);

                                            SetOverlay("New audio track");
                                        };

                                        newTrack.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
                                        this.MainOverlay.MenuSettingsAudioTracks.Items.Add(newTrack);

                                        SettingsMenuAudioTrack.DropDownItems.Add(TrackName, null, (s, e) => { mediaPlayer.SetAudioTrack(TrackID); SetOverlay("New audio track"); });
                                    });

                                    if (AutoAudioSelect && AudioSelected == false)
                                    {
                                        if (HasRegexMatch(TrackName, @"^.*\b(eng|english)\b.*$"))
                                        {
                                            mediaPlayer.SetAudioTrack(TrackID);
                                            AudioSelected = true;
                                        }
                                    }

                                    break;
                                }

                            // Subtitle track
                            case TrackType.Text:
                                {
                                    // Nothing needed
                                    break;
                                }

                            default:
                                break;
                        }

                    } catch { }
                }

                try
                {
                    // Subtitle track - TO DO: See Track Id Selection
                    for (int i = 0; i < mediaPlayer.SpuCount; i++)
                    {
                        string SubName = mediaPlayer.SpuDescription[i].Name;
                        int SubID = mediaPlayer.SpuDescription[i].Id;

                        if (string.IsNullOrWhiteSpace(SubName))
                            SubName = $"Track";

                        SubName += $" [{SubID}]";

                        this.Dispatcher.Invoke(() =>
                        {
                            MenuItem newTrack = new MenuItem { Header = SubName, Style = MainOverlay.MenuSettingsVideoTracks.Style };

                            newTrack.Click += (sender, e) =>
                            {
                                MenuItem menuItem = (MenuItem)sender;
                                int newSPU = IdFromTrackName(menuItem.Header.ToString());

                                mediaPlayer.SetSpu(newSPU);

                                SetOverlay("New subtitle track");
                            };

                            newTrack.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
                            this.MainOverlay.MenuSettingsSubtitleTracks.Items.Add(newTrack);

                            SettingsMenuSubtitleTrack.DropDownItems.Add(SubName, null, (s, e) => 
                            {
                                int newSPU = IdFromTrackName(SubName);

                                mediaPlayer.SetSpu(newSPU);

                                SetOverlay("New subtitle track");
                            });
                        });

                        Thread.Sleep(500);

                        // Subtitle auto select
                        if (AutoSubtitleSelect && SubtitleSelected == false)
                        {
                            if (HasRegexMatch(SubName, @"^.*\b(eng|english)\b.*$"))
                            {
                                mediaPlayer.SetSpu(mediaPlayer.SpuDescription[i].Id);
                                SubtitleSelected = true;
                            }
                        }
                    }
                } catch { }

                Thread.Sleep(100);

                this.Dispatcher.Invoke(() =>
                {
                    foreach (ToolStripItem item in SettingsMenuVideoTrack.DropDownItems) { item.ForeColor = SettingsMenuVideoTrack.ForeColor; }
                    foreach (ToolStripItem item in SettingsMenuAudioTrack.DropDownItems) { item.ForeColor = SettingsMenuAudioTrack.ForeColor; }
                    foreach (ToolStripItem item in SettingsMenuSubtitleTrack.DropDownItems) { item.ForeColor = SettingsMenuSubtitleTrack.ForeColor; }
                });
            });
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            StartThread(() =>
            {
                SetLabelContent(MainOverlay.labelPosition, TimeSpan.FromMilliseconds(e.Time).ToString(@"hh\:mm\:ss"));

                if (isSliderControl == false)
                {
                    SetSliderValue(MainOverlay.SliderMedia, Convert.ToInt32(e.Time / 1000));
                };

                if (AutoPlay)
                {
                    if (Math.Abs(mediaPlayer.Media.Duration - e.Time) / 1000 <= AutoPlayTime)
                    {
                        Next();
                    }
                }
            });
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            StopMediaPlayer();
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                videoPosition.ClearName();
                PlayBtnTxt = "Play";
            });

            SetLabelContent(MainOverlay.labelTitle, string.Empty);

            SetLabelContent(MainOverlay.labelPosition, "00:00:00");
            SetLabelContent(MainOverlay.labelLenght, "00:00:00");

            SetSliderMaximum(MainOverlay.SliderMedia, 1);
            SetSliderValue(MainOverlay.SliderMedia, 0);

            if (mediaPlayer.Media != null)
                mediaPlayer.Media.Dispose();

            mediaPlayer.Media = null;

            this.Dispatcher.Invoke(() =>
            {
                this.MainOverlay.MenuSettingsVideoTracks.Items.Clear();
                this.MainOverlay.MenuSettingsAudioTracks.Items.Clear();
                this.MainOverlay.MenuSettingsSubtitleTracks.Items.Clear();
            });
        }
        #endregion

        #region "Handles"
        // Initial Loading
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Controls handles init
            MainOverlay.VolumeSlider.VolumeSlider.ValueChanged += (s, nE) =>
            {
                Volume = Convert.ToInt32(nE.NewValue);
            };

            // Timers
            CreateJumpTimer();
            CreateSaveTimer();
            CreateMouseTimer();

            // Video list
            videoListWindow = new VideoListWindow(tvShow)
            {
                Owner = this
            };

            // Recents
            recents.Load();
            RefreshRecentsMenu();

            // Others
            KeyboardHook.OnKeyPress += KeyboardHook_OnKeyPress;
            IsLoading = false;
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            // Can't set new image until old one is rendered, because fuck WPF
            if(Mute)
                SetImage(MainOverlay.btnMuteImage, Images.btnMute);
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr windowHandle = (new WindowInteropHelper(this)).Handle;
            HwndSource src = HwndSource.FromHwnd(windowHandle);
            src.AddHook(new HwndSourceHook(WndProc));
        }

        private void MainWindow_Closing(object sender, EventArgs e)
        {
            IntPtr windowHandle = (new WindowInteropHelper(this)).Handle;
            HwndSource src = HwndSource.FromHwnd(windowHandle);
            src.RemoveHook(new HwndSourceHook(this.WndProc));

            if (keyboardHook != null) 
                keyboardHook.Dispose();
        }

        // Form Events
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowStyle == WindowStyle.None && this.WindowState == WindowState.Maximized)
            {
                Process process = Process.GetCurrentProcess();
                IntPtr ptr = process.MainWindowHandle;
                GetWindowRect(ptr, ref rect);

                rect.Top += 7;
                rect.Left += 7;

                MouseTimer.Start();

                TopOpen = false;

                int SideMargins = Convert.ToInt32(this.Width / 10).RoundOff();
                MainOverlay.BottomMenu.Margin = new Thickness(SideMargins, 0, SideMargins, 0);
            }
            else
            {
                MouseTimer.Stop();

                TopOpen = true;

                MainOverlay.BottomMenu.Margin = new Thickness(0, 0, 0, 0);
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;

                if (mediaPlayer.State == VLCState.Paused)
                {
                    mediaPlayer.Play();
                }
                else if (mediaPlayer.CanPause)
                {
                    Pause();
                }
            }
            if (e.Key == Key.Up)
            {
                Volume += 5;
            }
            if (e.Key == Key.Down)
            {
                Volume -= 5;
            }
            else if (e.Key == Key.Left)
            {
                e.Handled = true;
                JumpBackward();
            }
            else if (e.Key == Key.Right)
            {
                e.Handled = true;
                JumpForward();
            }
            else if (e.Key == Key.F)
            {
                BtnFullscreen_Click(null, null);
            }
            else if (e.Key == Key.P)
            {
                MenuFileScreenShot_Click(null, null);
            }
            else if (e.Key == Key.End)
            {
                StopMediaPlayer();
            }
            else if (e.Key == Key.O)
            {
                OnTop = !OnTop;
            }
            else if (e.Key == Key.G)
            {
                GameMode = !GameMode;
            }
            else if (e.Key == Key.N)
            {
                Previous();
            }
            else if (e.Key == Key.M)
            {
                Next();
            }
        }

        private void KeyboardHook_OnKeyPress(Key key)
        {
            if (gameMode == false)
            {
                if (keyboardHook != null)
                    keyboardHook.Dispose();
            }

            switch (key)
            {
                case Key.F9:
                    Play(false);
                    break;

                case Key.F10:
                    Pause();
                    break;

                case Key.PageUp:
                    JumpForward();
                    break;

                case Key.PageDown:
                    JumpBackward();
                    break;
            }
        }

        private void MainWindow_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
                e.Effects = System.Windows.DragDropEffects.Copy;
        }

        private void MainWindow_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[] Files = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);

            ProcessDrop(Files);
        }

        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                Volume += 5;
            }
            else if (e.Delta < 0)
            {
                Volume -= 5;
            }
        }

        private void OverlayPanel_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                Volume += 5;
            }
            else if (e.Delta < 0)
            {
                Volume -= 5;
            }
        }

        private void OverlayPanel_DragOver(object sender, System.Windows.Forms.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
                e.Effect = System.Windows.Forms.DragDropEffects.Copy;
        }

        private void OverlayPanel_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            string[] Files = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);

            ProcessDrop(Files);
        }

        // Top Menu
        private void MenuFileMediaInfo_Click(object sender, RoutedEventArgs e)
        {
            MediaInfoWindow frmInfo = new MediaInfoWindow(mediaPlayer.Media);
            frmInfo.ShowDialog();
        }

        private void MenuFileScreenShot_Click(object sender, RoutedEventArgs e)
        {
            StartThread(() =>
            {
                string ScreenShotLocation = AppDomain.CurrentDomain.BaseDirectory + @"\Screenshots";

                if (Directory.Exists(ScreenShotLocation) == false)
                    Directory.CreateDirectory(ScreenShotLocation);

                string CurrentScreenShotLocation = ScreenShotLocation + @"\ScreenShot-" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" + DateTime.Now.Second.ToString() + "-" + DateTime.Now.Millisecond.ToString() + "-" + GetRandom(100000, 999999).ToString() + ".png";

                mediaPlayer.TakeSnapshot(0, CurrentScreenShotLocation, 0, 0);
            });
        }

        private void MenuFileQuit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuPlaybackVolumeUp_Click(object sender, RoutedEventArgs e)
        {
            Volume += 5;
        }

        private void MenuPlaybackVolumeDown_Click(object sender, RoutedEventArgs e)
        {
            Volume -= 5;
        }

        private void SliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Speed = Convert.ToInt32(e.NewValue);
        }

        private void SliderSpeed_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender == null)
                return;

            if (sender is Slider slider)
            {
                slider.Value = Speed;
            }
        }

        private void SliderJump_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Jump = Convert.ToInt32(e.NewValue);
        }

        private void SliderJump_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender == null)
                return;

            if (sender is Slider slider)
            {
                slider.Value = Jump;
            }
        }

        private void SliderAutoplay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AutoPlayTime = Convert.ToInt32(e.NewValue);
        }

        private void SliderAutoplay_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender == null)
                return;

            if (sender is Slider slider)
            {
                IsLoading = true; // bug: shows overlay on first load. temporary until something good

                slider.Value = AutoPlayTime;

                IsLoading = false;
            }
        }

        private void MenuSettingsVideoAspectRatio_Click(object sender, RoutedEventArgs e)
        {
            string CustomSelection = string.Empty;

            if (sender is MenuItem cBtn)
            {
                if (cBtn.Header is TextBlock cTxt)
                {
                    CustomSelection = cTxt.Text;
                }
            }
            else if (sender is ToolStripItem cTsi)
            {
                CustomSelection = cTsi.Text;
            }

            switch (CustomSelection)
            {
                case "Custom":
                    {
                        InputDialog newAspectRatio = new InputDialog()
                        {
                            WindowTitle = "New Aspect Ratio",
                            MainInstruction = "Input format is {0}:{1}, current aspect ratio is " + AspectRatio,
                            MaxLength = 10
                        };

                        if (newAspectRatio.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                            break;

                        string newInput = newAspectRatio.Input;

                        if (newInput.Contains(":") == false)
                            break;

                        if (newInput.Count(x => x == ':') != 1)
                            break;

                        if (IsNumeric(newInput.Split(":".ToCharArray())[0]) && IsNumeric(newInput.Split(":".ToCharArray())[1]))
                            AspectRatio = newInput;

                        break;
                    }

                case "Reset":
                    {
                        AspectRatio = string.Empty;
                        break;
                    }

                default:
                    {
                        if (CustomSelection.Contains(":") == false)
                            break;

                        AspectRatio = CustomSelection;
                        break;
                    }
            }
        }

        private void MenuSettingsAudioMode_Click(object sender, RoutedEventArgs e)
        {
            string CustomSelection = string.Empty;

            if (sender is MenuItem cBtn)
            {
                if (cBtn.Header is TextBlock cTxt)
                {
                    CustomSelection = cTxt.Text;
                }
            }
            else if (sender is ToolStripItem cTsi)
            {
                CustomSelection = cTsi.Text;
            }

            switch (CustomSelection)
            {
                case "Stereo":
                    {
                        AudioMode = AudioType.Stereo;
                        break;
                    }

                case "Surround":
                    {
                        AudioMode = AudioType.Surround;
                        break;
                    }

                default:
                    break;
            }
        }

        private void MenuSettingsAudioAutoSelect_Checked(object sender, RoutedEventArgs e)
        {
            AutoAudioSelect = MainOverlay.MenuSettingsAudioAutoSelect.IsChecked;
        }

        private void MenuSettingsSubtitleAutoSelect_Checked(object sender, RoutedEventArgs e)
        {
            AutoSubtitleSelect = MainOverlay.MenuSettingsSubtitleAutoSelect.IsChecked;
        }

        private void MenuSettingsSubtitleAdd_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Media == null)
                return;

            OpenFileDialog subtitleDialog = new OpenFileDialog()
            {
                Filter = @"Subtitle|" + string.Join(";", Extensions.Subtitle.Select( x => @"*" + x )) + "|All files|*.*",
                Title = "Add new subtitle",
                CheckFileExists = true,
                SupportMultiDottedExtensions = false,
                Multiselect = false
            };

            if (subtitleDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadSubtitle(subtitleDialog.FileName);
            }
        }

        private void MenuSettingsOnTop_Click(object sender, RoutedEventArgs e)
        {
            OnTop = MainOverlay.MenuSettingsOnTop.IsChecked;
        }

        private void MenuSettingsAcceleration_Click(object sender, RoutedEventArgs e)
        {
            Acceleration = MainOverlay.MenuSettingsAcceleration.IsChecked;

            if (CMBox.Show("Info", "Hardware Acceleration has been changed, for the best results a player restart is needed, restart now?", MessageCustomHandler.Style.Question, Buttons.YesNo).MainResult == result.Yes)
            {
                System.Windows.Forms.Application.Restart();
            }
            else
            {
                ReOpenFile();
            }
        }

        private void MenuSettingsGameMode_Click(object sender, RoutedEventArgs e)
        {
            GameMode = MainOverlay.MenuSettingsGameMode.IsChecked;
        }

        private void MenuSettingsShutDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender == null)
                return;

            if (sender is MenuItem menuItem)
            {
                if (menuItem.IsChecked == false)
                {
                    ShutDown(ShutDownType.Cancel, 0);
                    UnCheckShutDown();

                    SetOverlay("Shutdown cancelled");
                    return;
                }

                UnCheckShutDown();

                if (menuItem == MainOverlay.MenuSettingsShutDownAfterThis)
                {
                    ShutDown(ShutDownType.After, 0);

                    SetOverlay("Shutdown after this episode");
                }
                else if (menuItem == MainOverlay.MenuSettingsShutDownAfterN)
                {
                    InputDialog argInput = new InputDialog
                    {
                        WindowTitle = "ShutDown",
                        MainInstruction = "After 'n' episodes shut down the computer",
                        MaxLength = 3
                    };

                    if (argInput.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        if (IsNumeric(argInput.Input))
                        {
                            ShutDown(ShutDownType.AfterN, Convert.ToInt32(argInput.Input));

                            SetOverlay($"Shutdown after {argInput.Input} episodes");
                        }
                        else
                        {
                            CMBox.Show("Warning", "Invalid input", MessageCustomHandler.Style.Warning, Buttons.OK);
                        }
                    }
                }
                else if (menuItem == MainOverlay.MenuSettingsShutDownAfterTime)
                {
                    InputDialog argInput = new InputDialog
                    {
                        WindowTitle = "ShutDown",
                        MainInstruction = "After 'n' seconds shut down the computer",
                        MaxLength = 8
                    };

                    if (argInput.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        if (IsNumeric(argInput.Input))
                        {
                            ShutDown(ShutDownType.In, Convert.ToInt32(argInput.Input));

                            SetOverlay($"Shutdown in {argInput.Input} seconds");
                        }
                        else
                        {
                            CMBox.Show("Warning", "Invalid input", MessageCustomHandler.Style.Warning, Buttons.OK);
                        }
                    }
                }
                else if (menuItem == MainOverlay.MenuSettingsShutDownEndPlaylist)
                {
                    ShutDown(ShutDownType.End, 0);

                    SetOverlay("Shutdown at the end of playlist");
                }

                SetMenuItemChecked(menuItem, true);
            }
        }

        private void MenuPlaylistAutoplay_Click(object sender, RoutedEventArgs e)
        {
            AutoPlay = MainOverlay.MenuPlaylistAutoplay.IsChecked;
        }

        private void MenuPlaylistAutoplay_Loaded(object sender, RoutedEventArgs e)
        {
            MainOverlay.MenuPlaylistAutoplay.IsChecked = AutoPlay;
        }

        private void MenuTrackDisable_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem dBtn)
            {
                switch(dBtn.Name)
                {
                    case "VideoD":
                        {
                            mediaPlayer.SetVideoTrack(-1);
                            break;
                        }

                    case "AudioD":
                        {
                            mediaPlayer.SetAudioTrack(-1);
                            break;
                        }

                    case "SubtitleD":
                        {
                            mediaPlayer.SetSpu(-1);
                            break;
                        }
                }
            }
            else if (sender is ToolStripItem dTis)
            {
                switch(dTis.Text)
                {
                    case "Disable Video":
                        {
                            mediaPlayer.SetVideoTrack(-1);
                            break;
                        }

                    case "Disable Audio":
                        {
                            mediaPlayer.SetAudioTrack(-1);
                            break;
                        }

                    case "Disable Subtitle":
                        {
                            mediaPlayer.SetSpu(-1);
                            break;
                        }
                }
            }
        }

        private void MenuPlaylistNext_Click(object sender, RoutedEventArgs e)
        {
            Next();
        }

        private void MenuPlaylistPrevious_Click(object sender, RoutedEventArgs e)
        {
            Previous();
        }

        private void MenuPlaylistVideoList_Click(object sender, RoutedEventArgs e)
        {
            videoListWindow.Show();
            videoListWindow.SetTvShow(tvShow);
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        // UI Button Controls
        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (!mediaPlayer.IsPlaying)
            {
                if (mediaPlayer.Media == null) 
                {
                    if (recents.GetList().Count > 0)
                    {
                        OpenFile(recents.GetList().First());
                    }
                    else
                    {
                        NewFile();
                    }
                    //mediaPlayer.Play(new Media(libVLC, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4")));
                } 
                else
                {
                    mediaPlayer.Play();
                }
            }
            else if(mediaPlayer.CanPause)
            {
                Pause();
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            StopMediaPlayer();
        }

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            JumpForward();
        }

        private void BtnBackward_Click(object sender, RoutedEventArgs e)
        {
            JumpBackward();
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            NewFile();
        }

        private void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            Mute = !Mute;
        }

        private void BtnFullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                lastState = this.WindowState;

                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;

                SetImage(MainOverlay.btnFullscreenImage, Images.btnFullScreenOff);

                BottomOpen = false;

                screen = GetCurrentScreen();
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                if (this.WindowStyle == WindowStyle.SingleBorderWindow)
                {
                    lastState = this.WindowState;

                    this.WindowState = WindowState.Normal;
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;

                    SetImage(MainOverlay.btnFullscreenImage, Images.btnFullScreenOff);

                    BottomOpen = false;
                }
                else
                {
                    this.WindowState = lastState;
                    this.WindowStyle = WindowStyle.SingleBorderWindow;

                    SetImage(MainOverlay.btnFullscreenImage, Images.btnFullScreenOn);

                    BottomOpen = true;
                }
            }
        }

        // Slider Control
        private void SliderMedia_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            mediaPlayer.Time = Convert.ToInt64(MainOverlay.SliderMedia.Value * 1000);
            isSliderControl = false;
        }

        private void SliderMedia_DragStarted(object sender, DragStartedEventArgs e)
        {
            isSliderControl = true;
        }

        private void SliderMedia_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed  && e.MouseDevice.Captured == null)
            {
                MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonDownEvent
                };

                (sender as Thumb).RaiseEvent(args);

            }
        }

        // Others
        private void OnPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
        #endregion
    }
}