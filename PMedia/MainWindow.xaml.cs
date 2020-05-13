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

using SuperContextMenu;
using Ookii.Dialogs.WinForms;
using MessageCustomHandler;
using LibVLCSharp.Shared;

using MenuItem = System.Windows.Controls.MenuItem;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using Label = System.Windows.Controls.Label;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Slider = System.Windows.Controls.Slider;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
#endregion

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
        private readonly string videoPositionDir = AppDomain.CurrentDomain.BaseDirectory + @"\Data";
        private readonly string recentsPath = AppDomain.CurrentDomain.BaseDirectory + @"\recents.ini";

        private readonly Random random = new Random(Guid.NewGuid().GetHashCode());
        private readonly Settings settings;

        private Rect rect = new Rect(); // used in X-Y location
        private System.Windows.Forms.Timer MouseTimer;

        private string AppName;
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

        private MediaPlayer mediaPlayer;
        private LibVLC libVLC;
        private VideoListWindow videoListWindow;
        private readonly VideoPosition videoPosition;

        private readonly List<JumpCommand> jumpCommands;
        private static bool isSliderControl = false;
        private Thread threadShutDown;

        private WindowState lastState = WindowState.Normal;
        private const int TopSize = 23;
        private const int BottomSize = 40;

        private ContextMenuMedia ContextMedia;
        private PoperContainer poperContextMedia;

        private readonly Recents recents;

        private KeyboardHook keyboardHook = null;
        #endregion

        #region "Proprieties"
        public System.Drawing.Point Location
        {
            get
            {
                return new System.Drawing.Point(rect.Left, rect.Top);
            }
            set
            {
                Left = value.X;
                Top = value.Y;
            }
        }

        public string PlayBtnTxt
        {
            get
            {
                return playBtnTxt;
            }

            set
            {
                playBtnTxt = value;

                if (value.ToLower().Trim() == "play")
                {
                    if (btnPlay.Dispatcher.CheckAccess())
                    {
                        btnPlayImage.Source = ImageResource(Images.btnPlay);
                        MenuPlaybackPlayImage.Source = ImageResource(Images.btnPlay);
                    }
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            btnPlayImage.Source = ImageResource(Images.btnPlay);
                            MenuPlaybackPlayImage.Source = ImageResource(Images.btnPlay);
                        });
                    }
                }
                else
                {
                    if (btnPlay.Dispatcher.CheckAccess())
                    {
                        btnPlayImage.Source = ImageResource(Images.btnPause);
                        MenuPlaybackPlayImage.Source = ImageResource(Images.btnPause);
                    }
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            btnPlayImage.Source = ImageResource(Images.btnPause);
                            MenuPlaybackPlayImage.Source = ImageResource(Images.btnPause);
                        });
                    }
                       
                }

                OnPropertyChanged("PlayBtnTxt");
            }
        }

        public string SpeedText
        {
            get
            {
                return speedText;
            }
            set
            {
                speedText = value;
                OnPropertyChanged("SpeedText");
            }
        }

        public string JumpText
        {
            get
            {
                return jumpText;
            }

            set
            {
                jumpText = value;
                OnPropertyChanged("JumpText");
            }
        }

        public string AutoPlayText
        {
            get
            {
                return autoPlayText;
            }

            set
            {
                autoPlayText = value;
                OnPropertyChanged("AutoPlayText");
            }
        }

        private bool Mute
        {
            set
            {
                settings.IsMute = value;

                if (value == true)
                {
                    SetImage(btnMuteImage, Images.btnMute);

                    if (mediaPlayer != null && mediaPlayer.Media != null)
                    {
                        StartThread(() =>
                        {
                            mediaPlayer.Mute = true;
                        });
                    }

                }
                else // false
                {

                    if (mediaPlayer != null && mediaPlayer.Media != null)
                    {
                        StartThread(() =>
                        {
                            mediaPlayer.Mute = false;
                        });
                    }

                    int cVolume = Volume;

                    if(cVolume >= 67)
                    {
                        SetImage(btnMuteImage, Images.btnVolume3);
                    }
                    else if(cVolume >= 34)
                    {
                        SetImage(btnMuteImage, Images.btnVolume2);
                    }
                    else
                    {
                        SetImage(btnMuteImage, Images.btnVolume1);
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
                int FinalValue = value;

                if (FinalValue > 200)
                    FinalValue = 200;

                if (FinalValue < 0)
                    FinalValue = 0;

                SetSliderValue(VolumeSlider.VolumeSlider, FinalValue);
                SetLabelContent(VolumeSlider.labelVolume, FinalValue.ToString() + @"%");

                settings.Volume = FinalValue;

                if(mediaPlayer != null && mediaPlayer.Media != null)
                {
                    StartThread(() =>
                    {
                        mediaPlayer.Volume = FinalValue;
                    });
                }

                int cVolume = Volume;
                string cPic = btnMuteImage.Source.ToString();

                if (cVolume >= 67 && cPic.EndsWith(Images.btnVolume3) == false)
                {
                    SetImage(btnMuteImage, Images.btnVolume3);
                }

                if (cVolume < 67 && cVolume >= 34 && cPic.EndsWith(Images.btnVolume2) == false)
                {
                    SetImage(btnMuteImage, Images.btnVolume2);
                }
                
                if (cVolume < 34 && cPic.EndsWith(Images.btnVolume1) == false)
                {
                    SetImage(btnMuteImage, Images.btnVolume1);
                }

                if (Mute == true && IsLoading == false) // IsLoading used so mute is loaded from settings
                    Mute = false;

                settings.Volume = FinalValue;

                SetOverlay("Volume " + FinalValue.ToString());
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
                int FinalSpeed = value;

                if (FinalSpeed < 1)
                    FinalSpeed = 1;

                if (FinalSpeed > 10)
                    FinalSpeed = 10;

                settings.Rate = FinalSpeed;

                if (mediaPlayer != null && mediaPlayer.Media != null)
                {
                    StartThread(() =>
                    {
                        mediaPlayer.SetRate((float)FinalSpeed);
                    });
                }

                SpeedText = "Speed (" + FinalSpeed.ToString() + "x)";

                SetOverlay("Speed " + FinalSpeed.ToString() + "x");
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
                int FinalJump = value;

                if (FinalJump < 1)
                    FinalJump = 1;

                if (FinalJump > 120)
                    FinalJump = 120;

                settings.Jump = FinalJump;

                JumpText = "Jump (" + FinalJump.ToString() + "s)";

                SetOverlay("Jump " + FinalJump.ToString() + "s");
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

                string finalText = "on";

                if (value == false)
                    finalText = "off";

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
                int FinalAutoPlay = value;

                if (FinalAutoPlay < 1)
                    FinalAutoPlay = 1;

                if (FinalAutoPlay > 120)
                    FinalAutoPlay = 120;

                settings.AutoPlayTime = FinalAutoPlay;

                AutoPlayText = "AutoPlay (" + FinalAutoPlay.ToString() + "s)";

                SetOverlay("AutoPlay " + FinalAutoPlay.ToString() + "s");
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
                return aspectRatio;
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

                SetOverlay("AudioMode " + value.ToString());
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

                SetMenuItemChecked(MenuSettingsAudioAutoSelect, value);
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

                SetMenuItemChecked(MenuSettingsSubtitleAutoSelect, value);
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

                SetMenuItemChecked(MenuSettingsOnTop, value);

                string finalValue = "on";
                if (value == false)
                    finalValue = "off";

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

                SetMenuItemChecked(MenuSettingsAcceleration, value);

                ReOpenFile();

                string finalValue = "on";
                if (value == false)
                    finalValue = "off";

                SetOverlay("Hardware acceleration " + finalValue);
            }
            get
            {
                return settings.Acceleration;
            }
        }

        private bool GameMode
        {
            set
            {
                gameMode = value;

                SetMenuItemChecked(MenuSettingsGameMode, value);

                if (value == true)
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

                string finalValue = "on";
                if (value == false)
                    finalValue = "off";

                SetOverlay("Game mode " + finalValue);
            }

            get
            {
                return gameMode;
            }
        }

        private ShutDownCommand shutDownCmd;
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

        internal class TransparentPanel : System.Windows.Forms.Panel
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                    return cp;
                }
            }
            protected override void OnPaintBackground(PaintEventArgs e)
            {
                //base.OnPaintBackground(e);
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

            JumpTimer.Elapsed += delegate
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

                    if (finalJump > 0 && (totalLenght - currentLenght) < (finalJump + 500))
                    {
                        finalTime = totalLenght - 500;
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

                    mediaPlayer.Time = finalTime;

                    SetOverlay(TimeSpan.FromMilliseconds(mediaPlayer.Time).ToString(@"hh\:mm\:ss"));

                }
                else
                {
                    jumpCommands.Clear();
                }
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
                if (settings.NeedsSaving == true)
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
                Interval = 200
            };

            MouseTimer.Tick += delegate
            {
                if (gameMode)
                    return;

                if (GetCursorPos(out POINT p))
                {
                    Screen screen = GetCurrentScreen();
                    bool XL = p.X >= screen.Bounds.X && p.X <= (screen.Bounds.Width + screen.Bounds.X - 1);

                    if (MainGrid.RowDefinitions[0].Height.Value <= 0 && p.Y < TopSize && XL)
                    {
                        MainGrid.RowDefinitions[0].Height = new GridLength(TopSize);
                    }
                    else if (MainGrid.RowDefinitions[2].Height.Value <= 0 && p.Y > (screen.Bounds.Height - BottomSize) && XL)
                    {
                        MainGrid.RowDefinitions[2].Height = new GridLength(BottomSize);
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

            labelLenght.Foreground = linearGradientBrush;
            labelPosition.Foreground = linearGradientBrush;
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
            SetMenuItemChecked(MenuSettingsShutDownAfterThis, false);
            SetMenuItemChecked(MenuSettingsShutDownAfterN, false);
            SetMenuItemChecked(MenuSettingsShutDownAfterTime, false);
            SetMenuItemChecked(MenuSettingsShutDownEndPlaylist, false);
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
                CMBox.Show("Error", "Couldn't shutdown", MessageCustomHandler.Style.Error, Buttons.OK, null, ex.ToString());
            }

        }

        private bool ShutDownSignal(ShutDownType shutDownMode, int Arg)
        {
            Pause();

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
                    });
                    threadShutDown.IsBackground = true;
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
            ProcessArgs(Args);

            InitializeComponent();

            DataContext = this;
            PlayBtnTxt = "Play";

            settings = new Settings();
            settings.Load();

            string vlcPath = AppDomain.CurrentDomain.BaseDirectory;
            if (Environment.Is64BitProcess == true) { vlcPath += @"\libvlc\win-x64"; } else { vlcPath += @"\libvlc\win-x86"; }

            Core.Initialize(vlcPath);

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

            MenuPlaylistNext.IsEnabled = false;
            MenuPlaylistPrevious.IsEnabled = false;

            shutDownCmd = new ShutDownCommand(ShutDownType.None, 0);

            videoPosition = new VideoPosition(videoPositionDir);

            recents = new Recents(recentsPath);
        }

        private void ProcessArgs(string[] Args)
        {
            if (Args != null)
            {
                if (Args.Count() != 1)
                    return;

                bool FileFound = FileExists(Args[0]);

                if (FileFound == false)
                    return;

                if (IsRunning() == true)
                {
                    SendFile(Args[0]);
                    this.Close();
                    return;
                }
                else
                {
                    StartThread(() =>
                    {
                        while (IsLoading == true)
                        {
                            Thread.Sleep(200);
                        }

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
                    COPYDATASTRUCT cd = new COPYDATASTRUCT();
                    cd = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));
                    string file = cd.lpData;

                    if (FileExists(file) == true)
                    OpenFile(file);
                }
            }

            return IntPtr.Zero;
        }

        private bool IsRunning()
        {
            return (Process.GetProcessesByName(AppName).Count() > 1);
        }

        private bool FileExists(string file)
        {
            try
            {
                return File.Exists(file);
            }
            catch
            {
                return false;
            }
        }

        private void SendFile(string file)
        {
            foreach (var p in Process.GetProcessesByName(AppName))
            {
                if (p.MainWindowHandle == Process.GetCurrentProcess().MainWindowHandle) continue;
                {
                    COPYDATASTRUCT cd = new COPYDATASTRUCT();
                    cd.lpData = file;
                    cd.dwData = p.MainWindowHandle;
                    cd.cbData = cd.lpData.Length + 1;

                    SendMessage(p.MainWindowHandle, ExternalCommands.WM_COPYDATA, IntPtr.Zero, ref cd);
                }
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

        private int IdFromTrackName(string Name)
        {
            try
            {
                return Convert.ToInt32(Name.Split("[".ToCharArray()).Last().Split("]".ToCharArray()).First());
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't get id from name, Error: " + ex.Message, MessageCustomHandler.Style.Error, Buttons.OK, null, ex.ToString());
                return -1;
            }
        }

        private void NewFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Title = "Open new media",
                Multiselect = true,
                Filter = "Video Files|" + string.Join(";", Extensions.Video.Select( x => @"*" + x )) + "|All files|*.*",
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
                StopMediaPlayer();

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
            MenuItem newTrack = new MenuItem { Header = TrackName, Style = MenuSettingsVideoTracks.Style };

            newTrack.Click += (sender, e) =>
            {
                MenuItem menuItem = (MenuItem)sender;
                int newSPU = IdFromTrackName(menuItem.Header.ToString());

                mediaPlayer.SetSpu(newSPU);
            };

            newTrack.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
            this.MenuSettingsSubtitleTracks.Items.Add(newTrack);

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
        private int ProcessExternalCommand(int Command, int Arg)
        {
            try
            {
                switch (Command)
                {
                    case ExternalCommands.PM_PLAY:
                        {
                            if (mediaPlayer.State == VLCState.Paused)
                                mediaPlayer.Play();

                            break;
                        }

                    case ExternalCommands.PM_PAUSE:
                        {
                            if (mediaPlayer.CanPause == true)
                                mediaPlayer.Pause();

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
                StartThread(() => { CMBox.Show("Error", "Couldn't process external command, Error: " + ex.Message, MessageCustomHandler.Style.Error, Buttons.OK, null, ex.ToString()); });
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

            // context menu hack for uniform style
            ContextMedia = new ContextMenuMedia();
            poperContextMedia = new PoperContainer(ContextMedia);

            // overlay panel for context menu and double click fullscreen
            TransparentPanel overlayPanel = new TransparentPanel()
            {
                Dock = DockStyle.Fill,
                AllowDrop = true
            };

            // fullscreen toggle on double click
            overlayPanel.MouseDoubleClick += delegate { BtnFullscreen_Click(null, null); };
            overlayPanel.MouseWheel += OverlayPanel_MouseWheel;
            overlayPanel.DragOver += OverlayPanel_DragOver;
            overlayPanel.DragDrop += OverlayPanel_DragDrop;

            // hide context menu on btn press and media controls when fullscreen because controls don't hide when context menu is visible
            ContextMedia.OnMouseClickBtn += delegate
            {
                poperContextMedia.HideContext();

                if (this.WindowStyle == WindowStyle.None)
                {
                    MainGrid.RowDefinitions[0].Height = new GridLength(0);
                    MainGrid.RowDefinitions[2].Height = new GridLength(0);
                }
            };

            // btns handles on existing handles for simplicity
            ContextMedia.OnPlayBtn += delegate { BtnPlay_Click(null, null); };
            ContextMedia.OnStopBtn += delegate { StopMediaPlayer(); };
            ContextMedia.OnBackwardBtn += delegate { JumpBackward(); };
            ContextMedia.OnForwardBtn += delegate { JumpForward(); };
            ContextMedia.OnVolumeUpBtn += delegate { Volume += 5; };
            ContextMedia.OnVolumeDownBtn += delegate { Volume -= 5; };
            ContextMedia.OnMuteBtn += delegate { Mute = !Mute; };
            ContextMedia.OnFullscreenBtn += delegate { BtnFullscreen_Click(null, null); };

            // add everything to win host
            System.Windows.Forms.Panel videoPanel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Black
            };
            videoPanel.Controls.Add(overlayPanel);
            videoPanel.Controls.Add(videoView);

            WinHost.Child = videoPanel;

            // Show context
            overlayPanel.MouseClick += (object sender, System.Windows.Forms.MouseEventArgs e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    poperContextMedia.ShowContext(overlayPanel, e.Location);
                }
            };
        }

        private void ProcessDrop(string[] Files)
        {
            if (Files.Count() != 1)
                return;

            bool videoLoaded = false;

            foreach (string Path in Files)
            {
                if (FileExists(Path) == false)
                    continue;

                if (Extensions.IsSubtitle(Path) == true)
                {
                    LoadSubtitle(Path);
                }
                else if (Extensions.IsVideo(Path) == true)
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

                SetMenuItemEnable(MenuPlaylistNext, tvShow.HasNextEpisode());
                SetMenuItemEnable(MenuPlaylistPrevious, tvShow.HasPreviousEpisode());

                this.Dispatcher.Invoke(delegate { videoListWindow.SetTvShow(tvShow); });
            });
        }

        private void SetOverlay(string newOverlay)
        {
            StartThread(() =>
            {
                if (mediaPlayer == null || mediaPlayer.IsSeekable == false)
                    return;

                mediaPlayer.SetMarqueeString(VideoMarqueeOption.Text, newOverlay);
            });
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

        private void Play()
        {
            try
            {
                if (mediaPlayer.State == VLCState.Paused)
                    mediaPlayer.Play();
            }
            catch
            { }
        }

        private void StopMediaPlayer()
        {
            ThreadPool.QueueUserWorkItem(_ => {
                this.mediaPlayer.Stop();

                if (this.mediaPlayer.Media != null)
                    this.mediaPlayer.Media.Dispose();
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
            }
        }

        private void JumpBackward()
        {
            if (mediaPlayer.IsSeekable)
            {
                jumpCommands.Add(new JumpCommand(JumpCommand.Direction.Backward, Jump));
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
                if (MenuPlaylistRecent.HasItems)
                    MenuPlaylistRecent.Items.Clear();

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
                            Style = MenuPlaylistRecent.Style,
                            Foreground = brush,
                        };
                        menuRecent.Click += (sender, e) => { OpenFile(Item); };

                        MenuPlaylistRecent.Items.Add(menuRecent);
                    }

                    // Separator
                    MenuItem newSeparator = new MenuItem
                    {
                        Style = MenuPlaylistSeparator01.Style,
                        BorderThickness = MenuPlaylistSeparator01.BorderThickness,
                        Background = MenuPlaylistSeparator01.Background,
                        Margin = new Thickness(0),
                        MinWidth = 115,
                        Height = 2
                    };
                    MenuPlaylistRecent.Items.Add(newSeparator);

                    // Clear Recents
                    MenuItem menuClearRecents = new MenuItem
                    {
                        Header = "Clear",
                        Style = MenuPlaylistRecent.Style,
                        Name = "ClearR",
                        Foreground = brush,
                        Icon = new Image { Source = ImageResource(Images.btnTrash) }
                    };
                    menuClearRecents.Click += (sender, e) =>
                    {
                        recents.ClearRecent();
                        RefreshRecentsMenu();
                    };
                    MenuPlaylistRecent.Items.Add(menuClearRecents);
                }
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't refresh recents, Error: " + ex.Message, MessageCustomHandler.Style.Error, Buttons.OK, null, ex.ToString());
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
                int InSleep = 0;

                // Need to wait for the tracks to load
                while (mediaPlayer.Media.Tracks.Count() <= 0 && mediaPlayer.Media.Duration <= 0 && InSleep <= 15)
                {
                    Thread.Sleep(200);
                    InSleep += 1;
                }

                FileInfo currentFile = new FileInfo(System.Net.WebUtility.UrlDecode(new Uri(e.Media.Mrl).AbsolutePath));

                ProcessShow(currentFile.FullName);

                SetLabelContent(labelTitle, currentFile.Name);
                SetLabelContent(labelLenght, TimeSpan.FromMilliseconds(mediaPlayer.Media.Duration).ToString(@"hh\:mm\:ss"));
                SetSliderValue(SliderMedia, 0);
                SetSliderMaximum(SliderMedia, Convert.ToInt32(mediaPlayer.Media.Duration / 1000));

                // Re-set media settings
                if (Mute == true)
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
                    this.MenuSettingsVideoTracks.Items.Clear();
                    this.MenuSettingsAudioTracks.Items.Clear();
                    this.MenuSettingsSubtitleTracks.Items.Clear();

                    MenuItem menuDisableVideo = new MenuItem { Header = "Disable", Style = MenuSettingsVideoTracks.Style, Name = "VideoD" };
                    menuDisableVideo.Click += MenuTrackDisable_Click;

                    MenuItem menuDisableAudio = new MenuItem { Header = "Disable",  Style = MenuSettingsVideoTracks.Style, Name = "AudioD" };
                    menuDisableAudio.Click += MenuTrackDisable_Click;

                    MenuItem menuDisableSubtitle = new MenuItem { Header = "Disable", Style = MenuSettingsVideoTracks.Style, Name = "SubtitleD"};
                    menuDisableSubtitle.Click += MenuTrackDisable_Click;

                    menuDisableVideo.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
                    menuDisableAudio.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
                    menuDisableSubtitle.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));

                    this.MenuSettingsVideoTracks.Items.Add(menuDisableVideo);
                    this.MenuSettingsAudioTracks.Items.Add(menuDisableAudio);

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
                                        MenuItem newTrack = new MenuItem { Header = TrackName, Style = MenuSettingsVideoTracks.Style };

                                        newTrack.Click += delegate
                                        {
                                            mediaPlayer.SetVideoTrack(TrackID);

                                            SetOverlay("New video track");
                                        };

                                        newTrack.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
                                        this.MenuSettingsVideoTracks.Items.Add(newTrack);
                                    });

                                    break;
                                }

                            // Audio track
                            case TrackType.Audio:
                                {
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        MenuItem newTrack = new MenuItem { Header = TrackName, Style = MenuSettingsVideoTracks.Style };

                                        newTrack.Click += delegate
                                        {
                                            mediaPlayer.SetAudioTrack(TrackID);

                                            SetOverlay("New audio track");
                                        };

                                        newTrack.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
                                        this.MenuSettingsAudioTracks.Items.Add(newTrack);
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
                        MenuItem newTrack = new MenuItem { Header = SubName, Style = MenuSettingsVideoTracks.Style };

                        newTrack.Click += (sender, e) =>
                        {
                            MenuItem menuItem = (MenuItem)sender;
                            int newSPU = IdFromTrackName(menuItem.Header.ToString());

                            mediaPlayer.SetSpu(newSPU);

                            SetOverlay("New subtitle track");
                        };

                        newTrack.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
                        this.MenuSettingsSubtitleTracks.Items.Add(newTrack);
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
            });
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            StartThread(() =>
            {
                SetLabelContent(labelPosition, TimeSpan.FromMilliseconds(e.Time).ToString(@"hh\:mm\:ss"));

                if (isSliderControl == false)
                {
                    SetSliderValue(SliderMedia, Convert.ToInt32(e.Time / 1000));
                };

                if (AutoPlay)
                {
                    if (Math.Abs(mediaPlayer.Media.Duration - e.Time) / 1000 <= AutoPlayTime)
                    {
                        if (ShutDownSignal(shutDownCmd.shutDownType, shutDownCmd.Arg) == false && tvShow.HasNextEpisode())
                        {
                            OpenFile(tvShow.NextEpisode().FilePath);
                        }
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

            SetLabelContent(labelTitle, string.Empty);

            SetLabelContent(labelPosition, "00:00:00");
            SetLabelContent(labelLenght, "00:00:00");

            SetSliderMaximum(SliderMedia, 1);
            SetSliderValue(SliderMedia, 0);

            if (mediaPlayer.Media != null)
                mediaPlayer.Media.Dispose();

            mediaPlayer.Media = null;

            this.Dispatcher.Invoke(() =>
            {
                this.MenuSettingsVideoTracks.Items.Clear();
                this.MenuSettingsAudioTracks.Items.Clear();
                this.MenuSettingsSubtitleTracks.Items.Clear();
            });
        }
        #endregion

        #region "Handles"
        // Initial Loading
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Controls handles init
            VolumeSlider.VolumeSlider.ValueChanged += (s, nE) =>
            {
                Volume = Convert.ToInt32(nE.NewValue);
            };

            this.MouseLeave += MainWindow_MouseLeave;

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

            // Marquee - for info on actions
            mediaPlayer.SetMarqueeInt(VideoMarqueeOption.X, 15);
            mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Y, 15);
            mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Timeout, 4000);
            mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Refresh, 150);
            mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Enable, 1);

            // Others
            KeyboardHook.OnKeyPress += KeyboardHook_OnKeyPress;
            IsLoading = false;
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            // Fix for click at new position on slider
            try
            {
                Thumb thumb = (SliderMedia.Template.FindName("PART_Track", SliderMedia) as Track).Thumb;
                thumb.MouseEnter += new System.Windows.Input.MouseEventHandler(SliderMedia_MouseEnter);
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't initialize thumb, Error: " + ex.Message, MessageCustomHandler.Style.Error, Buttons.OK, null, ex.ToString());
            }

            // Can't set new image until old one is rendered, because fuck WPF
            if(Mute == true)
                SetImage(btnMuteImage, Images.btnMute);
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
        private void MainWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.WindowStyle == WindowStyle.None)
            {
                MainGrid.RowDefinitions[0].Height = new GridLength(0);
                MainGrid.RowDefinitions[2].Height = new GridLength(0);
            }
        }

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
            }
            else
            {
                MouseTimer.Stop();
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (gameMode == true)
                return;

            if (e.Key == Key.Space)
            {
                e.Handled = true;

                if (mediaPlayer.State == VLCState.Paused)
                {
                    mediaPlayer.Play();
                }
                else if (mediaPlayer.CanPause == true)
                {
                    Pause();
                }
            }
            if (e.Key == Key.Up)
            {
                e.Handled = true;
                Volume += 5;
            }
            if (e.Key == Key.Down)
            {
                e.Handled = true;
                Volume -= 5;
            }
            else if (e.Key == Key.Left)
            {
                e.Handled = true;

                if (mediaPlayer.IsSeekable)
                {
                    jumpCommands.Add(new JumpCommand(JumpCommand.Direction.Backward, Jump));
                }
            }
            else if (e.Key == Key.Right)
            {
                e.Handled = true;

                if (mediaPlayer.IsSeekable)
                {
                    jumpCommands.Add(new JumpCommand(JumpCommand.Direction.Forward, Jump));
                }
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
                    Play();
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
                slider.Value = AutoPlayTime;
            }
        }

        private void MenuSettingsVideoAspectRatio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem cBtn)
            {
                if (cBtn.Header is TextBlock cTxt)
                {
                    string CustomSelection = cTxt.Text;

                    switch (CustomSelection)
                    {
                        case "Custom":
                            {
                                InputDialog newAspectRatio = new InputDialog()
                                {
                                    WindowTitle = "New Aspect Ratio",
                                    MainInstruction = "Input format is {0}:{1}",
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
            }
        }

        private void MenuSettingsAudioMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem cBtn)
            {
                if (cBtn.Header is TextBlock cTxt)
                {
                    string CustomSelection = cTxt.Text;

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
            }
        }

        private void MenuSettingsAudioAutoSelect_Checked(object sender, RoutedEventArgs e)
        {
            AutoAudioSelect = MenuSettingsAudioAutoSelect.IsChecked;
        }

        private void MenuSettingsSubtitleAutoSelect_Checked(object sender, RoutedEventArgs e)
        {
            AutoSubtitleSelect = MenuSettingsSubtitleAutoSelect.IsChecked;
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
            OnTop = MenuSettingsOnTop.IsChecked;
        }

        private void MenuSettingsAcceleration_Click(object sender, RoutedEventArgs e)
        {
            Acceleration = MenuSettingsAcceleration.IsChecked;
        }

        private void MenuSettingsGameMode_Click(object sender, RoutedEventArgs e)
        {
            GameMode = MenuSettingsGameMode.IsChecked;
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
                    return;
                }

                UnCheckShutDown();

                if (menuItem == MenuSettingsShutDownAfterThis)
                {
                    ShutDown(ShutDownType.After, 0);
                }
                else if (menuItem == MenuSettingsShutDownAfterN)
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
                        }
                        else
                        {
                            CMBox.Show("Warning", "Invalid input", MessageCustomHandler.Style.Warning, Buttons.OK);
                        }
                    }
                }
                else if (menuItem == MenuSettingsShutDownAfterTime)
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
                        }
                        else
                        {
                            CMBox.Show("Warning", "Invalid input", MessageCustomHandler.Style.Warning, Buttons.OK);
                        }
                    }
                }
                else if (menuItem == MenuSettingsShutDownEndPlaylist)
                {
                    ShutDown(ShutDownType.End, 0);
                }

                SetMenuItemChecked(menuItem, true);
            }
        }

        private void MenuPlaylistAutoplay_Click(object sender, RoutedEventArgs e)
        {
            AutoPlay = MenuPlaylistAutoplay.IsChecked;
        }

        private void MenuPlaylistAutoplay_Loaded(object sender, RoutedEventArgs e)
        {
            MenuPlaylistAutoplay.IsChecked = AutoPlay;
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
                    NewFile();
                } 
                else
                {
                    mediaPlayer.Play();
                }
            }
            else if(mediaPlayer.CanPause == true)
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

                SetImage(btnFullscreenImage, Images.btnFullScreenOff);

                Grid.SetRow(WinHost, 1);
                Grid.SetRowSpan(WinHost, 1);

                MainGrid.RowDefinitions[0].Height = new GridLength(0);
                MainGrid.RowDefinitions[2].Height = new GridLength(0);
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                if (this.WindowStyle == WindowStyle.SingleBorderWindow)
                {
                    lastState = this.WindowState;

                    this.WindowState = WindowState.Normal;
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;

                    SetImage(btnFullscreenImage, Images.btnFullScreenOff);

                    Grid.SetRow(WinHost, 1);
                    Grid.SetRowSpan(WinHost, 1);

                    MainGrid.RowDefinitions[0].Height = new GridLength(0);
                    MainGrid.RowDefinitions[2].Height = new GridLength(0);
                }
                else
                {
                    this.WindowState = lastState;
                    this.WindowStyle = WindowStyle.SingleBorderWindow;

                    SetImage(btnFullscreenImage, Images.btnFullScreenOn);

                    Grid.SetRow(WinHost, 1);
                    Grid.SetRowSpan(WinHost, 1);

                    MainGrid.RowDefinitions[0].Height = new GridLength(TopSize);
                    MainGrid.RowDefinitions[2].Height = new GridLength(BottomSize);
                }
            }
        }

        // Slider Control
        private void SliderMedia_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            mediaPlayer.Time = Convert.ToInt64(SliderMedia.Value * 1000);
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