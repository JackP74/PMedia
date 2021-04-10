#region "Imports"
using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Interop;
using System.Threading.Tasks;

using Ookii.Dialogs.WinForms;
using MessageCustomHandler;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

using MenuItem = System.Windows.Controls.MenuItem;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using Label = System.Windows.Controls.Label;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Slider = System.Windows.Controls.Slider;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

using static PMedia.ShutDownCommand;
using static PMedia.PlayerSettings;
using static PMedia.PlayerConstants;
#endregion

// TO DO: OPTIMIZE, FIX STOP
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
        private readonly Random random = new Random(Guid.NewGuid().GetHashCode());
        private readonly string AppName = Process.GetCurrentProcess().ProcessName;
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly string videoPositionDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private readonly string recentsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recents.ini");

        private readonly Settings settings = new Settings();
        private readonly TvShow tvShow = new TvShow();
        private ShutDownCommand shutDownCmd = new ShutDownCommand(ShutDownType.None, 0);

        private MediaPlayer mediaPlayer;
        private LibVLC libVLC;
        private VideoListWindow videoListWindow;
        private readonly VideoPosition videoPosition;
        private ContextMenuStrip PlayerContextMenu;
        private int MouseMoveTmr = 0;

        private readonly List<JumpCommand> jumpCommands = new List<JumpCommand>();
        private static bool isSliderControl = false;

        private WindowState lastState = WindowState.Normal;
        private readonly int BottomSize = 60;
        private Rect rect = new Rect(); // used in X-Y location
        private System.Windows.Forms.Timer MouseTimer;
        private System.Windows.Forms.Timer ShutdownTimer;
        
        private Screen UsedScreen;
        private System.Drawing.Rectangle BottomRect;
        private readonly Recents recents;
        private KeyboardHook keyboardHook = null;

        private ToolStripMenuItem SettingsMenuVideoTrack;
        private ToolStripMenuItem SettingsMenuAudioTrack;
        private ToolStripMenuItem SettingsMenuSubtitleTrack;

        private delegate void SafeBtnImgs(bool playImgs);
        #endregion

        #region "Proprieties"
        private PlayerOverlay MainOverlay { get; set; }

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

        public double TaskProgress
        {
            set
            {
                taskProgress = value.LimitToRange(0, 1);
                OnPropertyChanged("TaskProgress");
            }
            get
            {
                return taskProgress;
            }
        }

        public string PlayBtnTxt
        {
            set
            {
                playBtnTxt = value;
                SetPlayBtnImage(value.ToLower().Trim() == "play");

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
                        StartThread(() => { mediaPlayer.Mute = true; });
                }
                else // false
                {
                    if (mediaPlayer != null && mediaPlayer.Media != null)
                    {
                        StartThread(() => { mediaPlayer.Mute = false; });

                        if (Volume >= 67)
                            SetImage(MainOverlay.btnMuteImage, Images.btnVolume3);
                        else if (Volume >= 34)
                            SetImage(MainOverlay.btnMuteImage, Images.btnVolume2);
                        else
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
                SetLabelContent(MainOverlay.VolumeSlider.labelVolume, $"{FinalValue}%");

                settings.Volume = FinalValue;

                if(mediaPlayer != null && mediaPlayer.Media != null)
                {
                    StartThread(() => { mediaPlayer.Volume = FinalValue; });

                    string cPic = MainOverlay.btnMuteImage.Source.ToString();

                    if (FinalValue >= 67 && !cPic.EndsWith(Images.btnVolume3))
                        SetImage(MainOverlay.btnMuteImage, Images.btnVolume3);
                    else if (FinalValue >= 34 && !cPic.EndsWith(Images.btnVolume2))
                        SetImage(MainOverlay.btnMuteImage, Images.btnVolume2);
                    else if (!cPic.EndsWith(Images.btnVolume1))
                        SetImage(MainOverlay.btnMuteImage, Images.btnVolume1);

                    if (Mute && !IsLoading) // IsLoading used so mute is loaded from settings
                        Mute = false;
                }
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
                    StartThread(() => { mediaPlayer.SetRate(FinalSpeed); });
                
                SpeedText = $"Speed ({FinalSpeed}x)";
                SetOverlay($"Speed ({FinalSpeed}x)");
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

                JumpText = $"Jump ({FinalJump}s)";
                SetOverlay($"Jump ({FinalJump}s)");
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
                SetOverlay($"AutoPlay {finalText}");
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

                AutoPlayText = $"AutoPlay ({FinalAutoPlay}s)";
                SetOverlay($"AutoPlay ({FinalAutoPlay}s)");
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

                SetOverlay($"AspectRatio {value}");
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

                if (audioMode == AudioType.MonoL)
                    mediaPlayer.SetChannel(AudioOutputChannel.Left);

                if (audioMode == AudioType.MonoR)
                    mediaPlayer.SetChannel(AudioOutputChannel.Right);

                if (audioMode == AudioType.None || audioMode == AudioType.Surround)
                    mediaPlayer.SetChannel(AudioOutputChannel.Dolbys);

                if (audioMode == AudioType.Stereo)
                    mediaPlayer.SetChannel(AudioOutputChannel.Stereo);

                SetOverlay($"AudioMode {value}");
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
                return settings.Acceleration;
                //return false; // known bug, causes freeze on stop
            }
        }

        private bool GameMode
        {
            set
            {
                gameMode = value;
                SetMenuItemChecked(MainOverlay.MenuSettingsGameMode, value);

                if (keyboardHook != null)
                    keyboardHook.Dispose();

                if (value)
                    keyboardHook = new KeyboardHook();

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
                if (value && !topOpen)
                    MainOverlay.TopMenu.Visibility = Visibility.Visible;
                else if (!value && topOpen)
                    MainOverlay.TopMenu.Visibility = Visibility.Hidden;

                topOpen = value;
            }
        }

        private bool BottomOpen
        {
            set
            {
                if (value && !bottomOpen)
                    MainOverlay.BottomMenu.Visibility = Visibility.Visible;
                else if (!value && bottomOpen)
                    MainOverlay.BottomMenu.Visibility = Visibility.Hidden;

                bottomOpen = value;
            }
        }

        private bool SubtitleDisabled
        {
            set
            {
                settings.SubtitleDisable = value;
                SetMenuItemChecked(MainOverlay.MenuSettingsSubtitleDisable, value);

                string finalText = "Subtitles " + (value ? "disabled" : "enabled");
                SetOverlay(finalText);

                if (value)
                    mediaPlayer.SetSpu(-1);
                else
                {
                    if (mediaPlayer.SpuCount > 0)
                        mediaPlayer.SetSpu(0);
                }
            }
            get
            {
                return settings.SubtitleDisable;
            }
        }
        #endregion

        #region "Enums & Structs"
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

                    if (mediaPlayer.IsSeekable)
                    {
                        long finalJump = (long)jumpCommands.Sum(x => x.jump) * 1000;
                        jumpCommands.Clear();

                        if (finalJump == 0)
                            return;

                        long totalLenght = mediaPlayer.Media.Duration;
                        long currentLenght = mediaPlayer.Time;

                        StartThread(() =>
                        {
                            long finalTime = currentLenght + finalJump;
                            finalTime = finalTime.LimitToRange(500, totalLenght - 500);

                            if (AutoPlay && finalTime >= totalLenght - (AutoPlayTime * 1000))
                            Next();
                            else
                            {
                                mediaPlayer.Time = finalTime;
                            }
                        });
                        
                        SetOverlay(TimeSpan.FromMilliseconds(mediaPlayer.Time + finalJump).ToString(@"hh\:mm\:ss"));
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
                    if (!videoPosition.ErrorSaving) // don't keep trying to save
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

            MouseTimer.Tick += delegate
            {
                if (gameMode)
                    return;

                if (MouseMoveTmr <= 0 && MouseMoveTmr != 111)
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.None;
                    MouseMoveTmr = 111;
                }
                else if (MouseMoveTmr != 111)
                {
                    if (!forceMouse)
                        MouseMoveTmr--;

                    if (GetCursorPos(out POINT p))
                        BottomOpen = BottomRect.Contains(p);
                }
            };
        }

        private void CreateShutdownTimer()
        {
            ShutdownTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
        }
        #endregion

        #region "Set"
        private void SetLabelsColors()
        {
            LinearGradientBrush linearGradientBrush = new LinearGradientBrush();
            linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, (byte)97, (byte)156, (byte)202), 0));
            linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, (byte)91, (byte)135, (byte)184), 1));

            MainOverlay.labelLenght.Foreground = linearGradientBrush;
            MainOverlay.labelPosition.Foreground = linearGradientBrush;
        }

        private void SetImage(Image image, string newImage)
        {
            if (image.Dispatcher.CheckAccess()) // true
            image.Source = ImageResource(newImage);
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
                label.Content = newText;
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
            slider.Maximum = newMaximum;
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
                slider.Value = newValue;
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
                menuItem.IsEnabled = enabled;
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
                menuItem.IsChecked = toCheck;
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
            this.Topmost = newTopMost;
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
            Pause();
            Thread.Sleep(100);

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
                    shutDownCmd.Arg--;
                }

                return false;
            }
            else if (shutDownMode == ShutDownType.End)
            {
                if (!tvShow.neverSet && tvShow.episodeList.Last() == tvShow.GetCurrentEpisode())
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
            if (shutdownMode == ShutDownType.In)
            {
                ShutdownTimer.Stop();

                shutDownCmd = new ShutDownCommand(shutdownMode, Arg);

                if (Arg <= 2)
                ShutDownNow();
                else
                {
                    ShutdownTimer.Tick += delegate
                    {
                        Arg--;

                        if (Arg <= 0)
                        {
                            ShutdownTimer.Stop();

                            ShutDownNow();
                        }
                    };

                    ShutdownTimer.Start();
                }
            }
            else
            {
                ShutdownTimer.Stop();

                if (shutdownMode == ShutDownType.AfterN)
                {
                    if (!tvShow.neverSet && Arg > tvShow.episodeList.Count() - (tvShow.episodeList.IndexOf(tvShow.GetCurrentEpisode()) + 1))
                    {
                        var result = CMBox.Show("Warning", "Too few episodes left for this kind of shutdown", 
                            MessageCustomHandler.Style.Warning, Buttons.Custom, new string[] { "Cancel", "Auto-Adjust", "Keep" }, string.Empty);

                        if (result.CustomResult == "Cancel")
                        {
                            shutdownMode = ShutDownType.Cancel;
                            Arg = 0;
                        }
                        else if (result.CustomResult == "Auto-Adjust")
                        Arg = tvShow.episodeList.Count() - (tvShow.episodeList.IndexOf(tvShow.GetCurrentEpisode()) + 2);
                    }
                }

                shutDownCmd = new ShutDownCommand(shutdownMode, Arg);
            }
        }
        #endregion

        public MainWindow()
        {
            System.Windows.Forms.Application.EnableVisualStyles();

            string[] Args = App.Args;
            // TO DO add more arguments
            StartThread(() =>
            {
                ProcessArgs(Args);
            });
            
            InitializeComponent();

            MainOverlay = new PlayerOverlay(WinHost);
            MainOverlay.MouseMove += (s, e) => { OverlayPanel_MouseMove(s, null); };
            AddHandlers();

            DataContext = this;
            PlayBtnTxt = "Play";

            string vlcPath = AppDomain.CurrentDomain.BaseDirectory;
            if (Environment.Is64BitProcess) vlcPath += @"\libvlc\win-x64"; else vlcPath += @"\libvlc\win-x86";

            Core.Initialize(vlcPath);
            CreateMediaPlayer();
            SetLabelsColors();
            LoadSettings();

            this.Loaded += MainWindow_Loaded;
            this.ContentRendered += MainWindow_ContentRendered;
            this.StateChanged += MainWindow_StateChanged;
            this.KeyDown += MainWindow_KeyDown;

            videoPosition = new VideoPosition(videoPositionDir);
            recents = new Recents(recentsPath);

            MainOverlay.MenuPlaylistNext.IsEnabled = false;
            MainOverlay.MenuPlaylistPrevious.IsEnabled = false;
        }

        private void LoadSettings()
        {
            settings.Load();

            Volume = settings.Volume;
            Mute = settings.IsMute;
            Speed = settings.Rate;
            Jump = settings.Jump;
            AutoPlay = settings.AutoPlay;
            AutoPlayTime = settings.AutoPlayTime;
            AutoAudioSelect = settings.AutoAudio;
            AutoSubtitleSelect = settings.AutoSubtitle;
            Acceleration = settings.Acceleration;
            SubtitleDisabled = settings.SubtitleDisable;
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

            MainOverlay.MenuFileMediaSearch.Click += MenuFileMediaSearch_Click;
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

            MainOverlay.MenuSettingsAudioModeMonoLeft.Click += MenuSettingsAudioMode_Click;
            MainOverlay.MenuSettingsAudioModeMonoRight.Click += MenuSettingsAudioMode_Click;
            MainOverlay.MenuSettingsAudioModeStereo.Click += MenuSettingsAudioMode_Click;
            MainOverlay.MenuSettingsAudioModeSurrond.Click += MenuSettingsAudioMode_Click;
            MainOverlay.MenuSettingsAudioAutoSelect.Checked += MenuSettingsAudioAutoSelect_Checked;
            MainOverlay.MenuSettingsAudioAutoSelect.Unchecked += MenuSettingsAudioAutoSelect_Checked;

            MainOverlay.MenuSettingsSubtitleAdd.Click += MenuSettingsSubtitleAdd_Click;
            MainOverlay.MenuSettingsSubtitleAutoSelect.Checked += MenuSettingsSubtitleAutoSelect_Checked;
            MainOverlay.MenuSettingsSubtitleAutoSelect.Unchecked += MenuSettingsSubtitleAutoSelect_Checked;
            MainOverlay.MenuSettingsSubtitleDisable.Checked += MenuSettingsSubtitleDisable_Checked;
            MainOverlay.MenuSettingsSubtitleDisable.Unchecked += MenuSettingsSubtitleDisable_Checked;

            MainOverlay.MenuSettingsOnTop.Click += MenuSettingsOnTop_Click;
            MainOverlay.MenuSettingsAcceleration.Click += MenuSettingsAcceleration_Click;
            MainOverlay.MenuSettingsGameMode.Click += MenuSettingsGameMode_Click;

            MainOverlay.MenuSettingsShutDownAfterThis.Click += MenuSettingsShutDown_Click;
            MainOverlay.MenuSettingsShutDownAfterN.Click += MenuSettingsShutDown_Click;
            MainOverlay.MenuSettingsShutDownAfterTime.Click += MenuSettingsShutDown_Click;
            MainOverlay.MenuSettingsShutDownEndPlaylist.Click += MenuSettingsShutDown_Click;

            MainOverlay.MenuPlaylistImport.Click += PlaylistImport_Click;
            MainOverlay.MenuPlaylistExport.Click += PlaylistExport_Click;

            MainOverlay.MenuPlaylistAutoplay.Click += MenuPlaylistAutoplay_Click;
            MainOverlay.MenuPlaylistAutoplay.Loaded += MenuPlaylistAutoplay_Loaded;

            MainOverlay.AutoPlayChanged += SliderAutoplay_ValueChanged;
            MainOverlay.AutoPlayLoaded += SliderAutoplay_Loaded;

            MainOverlay.MenuPlaylistNext.Click += MenuPlaylistNext_Click;
            MainOverlay.MenuPlaylistPrevious.Click += MenuPlaylistPrevious_Click;

            MainOverlay.MenuPlaylistVideoList.Click += MenuPlaylistVideoList_Click;

            MainOverlay.MenuAbout.Click += MenuAbout_Click;

            //Taskbar items
            TskBtnPlay.Click += (s, e) => BtnPlay_Click(s, null);
            TskBtnStop.Click += (s, e) => BtnStop_Click(s, null);
            TskBtnBack.Click += (s, e) => BtnBackward_Click(s, null);
            TskBtnForward.Click += (s, e) => BtnForward_Click(s, null);
            TskBtnOpen.Click += (s, e) => BtnOpenFile_Click(s, null);
            TskBtnPrevious.Click += (s, e) => Next();
            TskBtnNext.Click += (s, e) => Previous();
        }

        private void ProcessArgs(string[] Args)
        {
            if (Args != null)
            {
                if (Args.Count() != 1)
                    return;

                bool FileFound = File.Exists(Args[0]);

                if (!FileFound)
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
                int Arg = (int)wParam;

                ProcessExternalCommand(Command, Arg);
            }
            else if (msg == ExternalCommands.WM_COPYDATA)
            {
                if (wParam == IntPtr.Zero)
                {
                    COPYDATASTRUCT cd = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));
                    string file = cd.lpData;

                    if (File.Exists(file))
                        ProcessDrop(new[] { file });
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
            return Screen.FromHandle(new WindowInteropHelper(this).Handle);
        }

        private void SetPlayBtnImage(bool playImgs)
        {
            if (!MainOverlay.Dispatcher.CheckAccess())
            {
                var d = new SafeBtnImgs(SetPlayBtnImage);
                MainOverlay.Dispatcher.Invoke(d, new object[] { playImgs });
            }
            else
            {
                ImageSource NewImg = ImageResource(playImgs ? Images.btnPlay : Images.btnPause);

                MainOverlay.btnPlayImage.Source = NewImg;
                MainOverlay.MenuPlaybackPlayImage.Source = NewImg;
                TskBtnPlay.ImageSource = NewImg;
            }
        }
        #endregion

        #region "MediaPlayer"
        private int IdFromTrackName(string Name)
        {
            try
            {
                string rawId = Regex.Match(Name, @"(\[(?<id>[^]]\d{0,4})\])").Groups["id"].Value;

                int newId = string.IsNullOrWhiteSpace(rawId) ? -1 : rawId.ToInt32();

                return newId;
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
                Filter = "Video Files|" + string.Join(";", Extensions.Video.Select(x => $"*{x}")) + "|All files|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (openFileDialog.FileNames.Count() != 1)
                {
                    Playlist newPlaylist = new Playlist();
                    List<EpisodeInfo> newFiles = new List<EpisodeInfo>();

                    foreach (string file in openFileDialog.FileNames)
                        newFiles.Add(tvShow.ParseFile(file));

                    newPlaylist.AddList(newFiles);
                    tvShow.episodeList.Clear();
                    tvShow.LoadPlaylist(newPlaylist);

                    if (newPlaylist.currentList.files.Count > 0)
                        OpenFile(newPlaylist.currentList.files[0].FilePath);
                }
                else
                {
                    OpenFile(openFileDialog.FileName);
                }
            }
        }

        public void OpenFile(string FilePath)
        {
            if (Extensions.IsVideo(FilePath))
                StartThread(() => { Media media = new Media(libVLC, FilePath, FromType.FromPath); if (!Acceleration) media.AddOption(@":avcodec-hw=none"); mediaPlayer.Play(media); });
        }

        public void ReOpenFile()
        {
            if (mediaPlayer.Media != null)
            {
                FileInfo currentFile = new FileInfo(System.Net.WebUtility.UrlDecode(new Uri(mediaPlayer.Media.Mrl).AbsolutePath));

                if (!currentFile.Exists)
                    return;

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
            string TrackName = mediaPlayer.SpuDescription[mediaPlayer.SpuCount - 1].Name;
            int TrackID = mediaPlayer.Spu;

            TrackName = string.IsNullOrEmpty(TrackName) ? "Track" : TrackName;
            TrackName += $" [{TrackID}]";

            // Add track to menu
            MenuItem newTrack = new MenuItem { Header = TrackName, Style = MainOverlay.MenuSettingsVideoTracks.Style };

            newTrack.Click += (sender, e) => { MenuItem menuItem = (MenuItem)sender; mediaPlayer.SetSpu(IdFromTrackName(menuItem.Header.ToString())); SetOverlay("New subtitle track"); };

            newTrack.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254));
            MainOverlay.MenuSettingsSubtitleTracks.Items.Add(newTrack);

            SettingsMenuSubtitleTrack.DropDownItems.Add(TrackName, null, (s, e) => { mediaPlayer.SetSpu(IdFromTrackName(TrackName)); SetOverlay("New subtitle track"); });
            SettingsMenuSubtitleTrack.DropDownItems[SettingsMenuSubtitleTrack.DropDownItems.Count - 1].ForeColor = SettingsMenuSubtitleTrack.ForeColor;
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
                        Play(true);
                        break;

                    case ExternalCommands.PM_PAUSE:
                        Pause();
                        break;

                    case ExternalCommands.PM_STOP:
                        StopMediaPlayer();
                        break;

                    case ExternalCommands.PM_FORWARD:
                        JumpForward();
                        break;

                    case ExternalCommands.PM_BACKWARD:
                        JumpBackward();
                        break;

                    case ExternalCommands.PM_NEXT:
                        Next();
                        break;

                    case ExternalCommands.PM_PREVIOUS:
                        Previous();
                        break;

                    case ExternalCommands.PM_VOLUMEUP:
                        Volume += Arg;
                        break;

                    case ExternalCommands.PM_VOLUMEDOWN:
                        Volume -= Arg;
                        break;

                    case ExternalCommands.PM_MUTE:
                        Mute = !Mute;
                        break;

                    case ExternalCommands.PM_AUTOPLAY:
                        AutoPlay = !AutoPlay;
                        break;
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

            PlayerContextMenu.Closed += (s, e) => { forceMouse = false; };
            PlayerContextMenu.Opened += (s, e) => { forceMouse = true; };

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
            overlayPanel.MouseMove += OverlayPanel_MouseMove;

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
            SettingsMenuVideoAR.DropDownItems.Add("Custom", Properties.Resources.btnEdit, (s, e) => { MenuSettingsVideoAspectRatio_Click(s, null); });
            SettingsMenuVideoAR.DropDownItems.Add("Reset", Properties.Resources.btnTrash, (s, e) => { MenuSettingsVideoAspectRatio_Click(s, null); });

            foreach (ToolStripItem item in SettingsMenuVideoAR.DropDownItems) { item.ForeColor = SettingsMenuVideoAR.ForeColor; }

            SettingsMenuVideoTrack = new ToolStripMenuItem("Sub Track", Properties.Resources.btnSelectTrack)
            { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };

            ToolStripMenuItem SettingsMenuVideo = new ToolStripMenuItem("Video", Properties.Resources.btnVideo) { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };
            SettingsMenuVideo.DropDownItems.AddRange(new[] { SettingsMenuVideoAR, SettingsMenuVideoTrack });

            ToolStripMenuItem SettingsMenuAudioMode = new ToolStripMenuItem("Mode", Properties.Resources.btnSet)
            { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };

            SettingsMenuAudioMode.DropDownItems.Add("Mono Left", null, (s, e) => { MenuSettingsAudioMode_Click(s, null); });
            SettingsMenuAudioMode.DropDownItems.Add("Mono Right", null, (s, e) => { MenuSettingsAudioMode_Click(s, null); });
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

            ToolStripMenuItem SettingsPlaylist = new ToolStripMenuItem("Playlist", Properties.Resources.btnPlaylist)
            { ForeColor = System.Drawing.Color.FromArgb(78, 173, 254) };

            SettingsPlaylist.DropDownItems.Add("Import", Properties.Resources.BtnImport, delegate { PlaylistImport_Click(null, null); });
            SettingsPlaylist.DropDownItems.Add("Export", Properties.Resources.btnExport, delegate { PlaylistExport_Click(null, null); });

            SettingsPlaylist.DropDownItems.Add(new ToolStripSeparator()); ////////////////////

            SettingsPlaylist.DropDownItems.Add("Next", Properties.Resources.btnNext, delegate { Next(); });
            SettingsPlaylist.DropDownItems.Add("Previous", Properties.Resources.btnPrevious, delegate { Previous(); });
            SettingsPlaylist.DropDownItems.Add("Video List", Properties.Resources.btnVideoList, delegate { MenuPlaylistVideoList_Click(null, null); });

            SettingsPlaylist.DropDownOpening += delegate
            {
                SettingsPlaylist.DropDownItems[3].Enabled = tvShow.HasNextEpisode();
                SettingsPlaylist.DropDownItems[4].Enabled = tvShow.HasPreviousEpisode();
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
                    LoadSubtitle(Path);
                else if (Extensions.IsVideo(Path) && !videoLoaded)
                {
                    OpenFile(Path);
                    videoLoaded = true;
                }
            }
        }

        private void ProcessShow(string FileName)
        {
            StartThread(() =>
            {
                tvShow.Load(FileName);

                if (ShutDownSignal(shutDownCmd.shutDownType, shutDownCmd.Arg))
                    return;

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
                mediaPlayer.Play();
                else
                {
                    if (OpenRecent && recents.GetList().Count > 0)
                    OpenFile(recents.GetList().Last());
                }
            }
            catch
            { }
        }

        private void StopMediaPlayer()
        {
            this.Dispatcher.Invoke(() =>
            {
                MainOverlay.IsEnabled = false;
                PlayerContextMenu.Enabled = false;
            });

            StartThread(async () =>
            {
                await Task.Run(() =>
                {
                    mediaPlayer.Stop();

                    if (mediaPlayer.Media != null)
                        mediaPlayer.Media.Dispose();
                    mediaPlayer.Media = null;

                    tvShow.episodeList.Clear();
                });
                
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
                jumpCommands.Add(new JumpCommand(JumpCommand.Direction.Forward, Jump));
        }

        private void JumpBackward()
        {
            if (mediaPlayer.IsSeekable)
                jumpCommands.Add(new JumpCommand(JumpCommand.Direction.Backward, Jump * -1));
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
                            Header = name.WithMaxLength(30),
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
                try
                {
                    int InSleep = 0;

                    // Need to wait for the tracks to load
                    while (InSleep < 25)
                    {
                        if (mediaPlayer.Media.Tracks.Count() > 0 && mediaPlayer.Media.Duration > 0)
                            break;

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
                    mediaPlayer.Mute = true;
                else
                    mediaPlayer.Volume = Volume;

                mediaPlayer.SetRate(Speed);

                // Things that need to run on the main thread
                this.Dispatcher.Invoke(() =>
                {
                    MainOverlay.MenuSettingsVideoTracks.Items.Clear();
                    MainOverlay.MenuSettingsAudioTracks.Items.Clear();
                    MainOverlay.MenuSettingsSubtitleTracks.Items.Clear();

                    SettingsMenuVideoTrack.DropDownItems.Clear();
                    SettingsMenuAudioTrack.DropDownItems.Clear();
                    SettingsMenuSubtitleTrack.DropDownItems.Clear();

                    MenuItem menuDisableVideo = new MenuItem { Header = "Disable", Style = MainOverlay.MenuSettingsVideoTracks.Style, Name = "VideoD" };
                    menuDisableVideo.Click += MenuTrackDisable_Click;

                    MenuItem menuDisableAudio = new MenuItem { Header = "Disable",  Style = MainOverlay.MenuSettingsVideoTracks.Style, Name = "AudioD" };
                    menuDisableAudio.Click += MenuTrackDisable_Click;

                    MainOverlay.MenuSettingsVideoTracks.Items.Add(menuDisableVideo);
                    MainOverlay.MenuSettingsAudioTracks.Items.Add(menuDisableAudio);

                    SettingsMenuVideoTrack.DropDownItems.Add("Disable Video", null, (s, e) => { MenuTrackDisable_Click(s, null); });
                    SettingsMenuAudioTrack.DropDownItems.Add("Disable Audio", null, (s, e) => { MenuTrackDisable_Click(s, null); });

                    videoPosition.SetNewFile(currentFile.Name, Convert.ToInt32(mediaPlayer.Media.Duration / 1000));

                    long currentPosition = videoPosition.GetPosition();
                    if(currentPosition != 0)
                        StartThread(() => { Thread.Sleep(1000); mediaPlayer.Time = currentPosition; });

                    recents.AddRecent(currentFile.FullName);
                    RefreshRecentsMenu();
                });

                foreach (MediaTrack mediaTrack in e.Media.Tracks)
                {
                    try
                    {
                        // Track name - description first, if not empty or und
                        string TrackName = (!string.IsNullOrEmpty(mediaTrack.Description) && mediaTrack.Description != "und") ? mediaTrack.Description : "Track";
                        TrackName += (!string.IsNullOrEmpty(mediaTrack.Language) && mediaTrack.Language != "und") ? $" - {mediaTrack.Language}" : string.Empty;
                        TrackName += $" [{mediaTrack.Id}]";

                        switch (mediaTrack.TrackType)
                        {
                            case TrackType.Video:
                                {
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        MenuItem newTrack = new MenuItem { Header = TrackName, Style = MainOverlay.MenuSettingsVideoTracks.Style };
                                        newTrack.Click += (s, e) => { mediaPlayer.SetVideoTrack(mediaTrack.Id); SetOverlay("New video track"); };
                                        MainOverlay.MenuSettingsVideoTracks.Items.Add(newTrack);

                                        SettingsMenuVideoTrack.DropDownItems.Add(TrackName, null, (s, e) => { mediaPlayer.SetVideoTrack(mediaTrack.Id); SetOverlay("New video track"); });
                                    });
                                    
                                    break;
                                }

                            case TrackType.Audio:
                                {
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        MenuItem newTrack = new MenuItem { Header = TrackName, Style = MainOverlay.MenuSettingsVideoTracks.Style };
                                        newTrack.Click += (s, e) => { mediaPlayer.SetAudioTrack(mediaTrack.Id); SetOverlay("New audio track"); };
                                        MainOverlay.MenuSettingsAudioTracks.Items.Add(newTrack);

                                        SettingsMenuAudioTrack.DropDownItems.Add(TrackName, null, (s, e) => { mediaPlayer.SetAudioTrack(mediaTrack.Id); SetOverlay("New audio track"); });
                                    });

                                    if (AutoAudioSelect && !AudioSelected && HasRegexMatch(TrackName, @"^.*\b(eng|english)\b.*$"))
                                    {
                                        mediaPlayer.SetAudioTrack(mediaTrack.Id);
                                        AudioSelected = true;
                                    }

                                    break;
                                }

                            default:
                                continue;
                        }
                    }
                    catch
                    { }
                }

                if (mediaPlayer.SpuCount > 0)
                {
                    for (int i = 0; i < mediaPlayer.SpuCount; i++)
                    {
                        try
                        {
                            string SubName = !string.IsNullOrEmpty(mediaPlayer.SpuDescription[i].Name) ? mediaPlayer.SpuDescription[i].Name : "Track";
                            SubName += $" [{mediaPlayer.SpuDescription[i].Id}]";

                            this.Dispatcher.Invoke(() =>
                            {
                                MenuItem newTrack = new MenuItem { Header = SubName, Style = MainOverlay.MenuSettingsVideoTracks.Style };
                                newTrack.Click += (s, e) => { MenuItem menuItem = (MenuItem)s; mediaPlayer.SetSpu(IdFromTrackName(menuItem.Header.ToString())); SetOverlay("New subtitle track"); };
                                MainOverlay.MenuSettingsSubtitleTracks.Items.Add(newTrack);

                                SettingsMenuSubtitleTrack.DropDownItems.Add(SubName, null, (s, e) => { mediaPlayer.SetSpu(IdFromTrackName(SubName)); SetOverlay("New subtitle track"); });
                            });

                            if (AutoSubtitleSelect && !SubtitleSelected && !SubtitleDisabled && HasRegexMatch(SubName, @"^.*\b(eng|english)\b.*$"))
                            {
                                mediaPlayer.SetSpu(mediaPlayer.SpuDescription[i].Id);
                                SubtitleSelected = true;
                            }
                            else if (SubtitleDisabled)
                            {
                                mediaPlayer.SetSpu(-1);
                                SubtitleSelected = true;
                            }
                        }
                        catch
                        { }
                    }
                }

                this.Dispatcher.Invoke(() =>
                {
                    foreach (ToolStripItem item in SettingsMenuVideoTrack.DropDownItems) { item.ForeColor = SettingsMenuVideoTrack.ForeColor; }
                    foreach (ToolStripItem item in SettingsMenuAudioTrack.DropDownItems) { item.ForeColor = SettingsMenuAudioTrack.ForeColor; }
                    foreach (ToolStripItem item in SettingsMenuSubtitleTrack.DropDownItems) { item.ForeColor = SettingsMenuSubtitleTrack.ForeColor; }

                    foreach (MenuItem item in MainOverlay.MenuSettingsVideoTracks.Items) { item.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254)); }
                    foreach (MenuItem item in MainOverlay.MenuSettingsAudioTracks.Items) { item.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254)); }
                    foreach (MenuItem item in MainOverlay.MenuSettingsSubtitleTracks.Items) { item.Foreground = new SolidColorBrush(Color.FromRgb(78, 173, 254)); }
                });
            });
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            StartThread(() =>
            {
                SetLabelContent(MainOverlay.labelPosition, TimeSpan.FromMilliseconds(e.Time).ToString(@"hh\:mm\:ss"));

                if (!isSliderControl)
                SetSliderValue(MainOverlay.SliderMedia, Convert.ToInt32(e.Time / 1000));
                ;

                if (AutoPlay)
                {
                    if (Math.Abs(mediaPlayer.Media.Duration - e.Time) / 1000 <= AutoPlayTime)
                    Next();
                }

                TaskProgress = (double)e.Time / (double)mediaPlayer.Media.Duration;
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

            this.Dispatcher.Invoke(() =>
            {
                MainOverlay.MenuSettingsVideoTracks.Items.Clear();
                MainOverlay.MenuSettingsAudioTracks.Items.Clear();
                MainOverlay.MenuSettingsSubtitleTracks.Items.Clear();

                SettingsMenuVideoTrack.DropDownItems.Clear();
                SettingsMenuAudioTrack.DropDownItems.Clear();
                SettingsMenuSubtitleTrack.DropDownItems.Clear();
            });

            TaskProgress = 0d;
        }

        private void OverlayPanel_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (MouseMoveTmr == 111)
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;

            MouseMoveTmr = 30;
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
            CreateShutdownTimer();

            // Video list
            videoListWindow = new VideoListWindow(tvShow) { Owner = this };

            //videoListWindow.Closed += (s, e) => { forceMouse = false; };
            videoListWindow.IsVisibleChanged += (s, e) => { forceMouse = (bool)e.NewValue; };

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

                int SideMargins = Convert.ToInt32(this.Width / 10).RoundOff();
                MainOverlay.BottomMenu.Margin = new Thickness(SideMargins, 0, SideMargins, 0);

                TopOpen = false;
                BottomOpen = false;
            }
            else
            {
                MouseTimer.Stop();
                MainOverlay.BottomMenu.Margin = new Thickness(0, 0, 0, 0);

                TopOpen = true;
                BottomOpen = true;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            switch (e.Key)
            {
                case Key.Space:
                    if (mediaPlayer.State == VLCState.Paused)
                        Play(false);
                    else if (mediaPlayer.CanPause)
                        Pause();
                    break;

                case Key.Up:
                    Volume += 5;
                    break;

                case Key.Down:
                    Volume -= 5;
                    break;

                case Key.Left:
                    JumpBackward();
                    break;

                case Key.Right:
                    JumpForward();
                    break;

                case Key.F:
                    BtnFullscreen_Click(null, null);
                    break;

                case Key.P:
                    MenuFileScreenShot_Click(null, null);
                    break;

                case Key.End:
                    StopMediaPlayer();
                    break;

                case Key.O:
                    OnTop = !OnTop;
                    break;

                case Key.G:
                    GameMode = !GameMode;
                    break;

                case Key.N:
                    Previous();
                    break;

                case Key.M:
                    Next();
                    break;

                case Key.S:
                    SubtitleDisabled = !SubtitleDisabled;
                    break;

                default:
                    e.Handled = false;
                    break;
            }
        }

        private void KeyboardHook_OnKeyPress(Key key)
        {
            if (!gameMode && keyboardHook != null)
            {
                keyboardHook.Dispose();
                return;
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
                Volume += 5;
            else if (e.Delta < 0)
                Volume -= 5;
        }

        private void OverlayPanel_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Delta > 0)
                Volume += 5;
            else if (e.Delta < 0)
                Volume -= 5;
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
        private void MenuFileMediaSearch_Click(object sender, RoutedEventArgs e)
        {
            MediaSearchWindow mediaSearch = new MediaSearchWindow();
            mediaSearch.Show();
        }

        private void MenuFileMediaInfo_Click(object sender, RoutedEventArgs e)
        {
            MediaInfoWindow frmInfo = new MediaInfoWindow(mediaPlayer.Media);
            frmInfo.IsVisibleChanged += (s, e) => { forceMouse = (bool)e.NewValue; };
            frmInfo.ShowDialog();
        }

        private void MenuFileScreenShot_Click(object sender, RoutedEventArgs e)
        {
            StartThread(() =>
            {
                string ScreenShotLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                if (!Directory.Exists(ScreenShotLocation)) Directory.CreateDirectory(ScreenShotLocation);
                
                string CurrentScreenShotLocation = Path.Combine(ScreenShotLocation, $"ScreenShot-{DateTime.Now:yyyy-MM-dd-HH-mm-ss-ff}-{GetRandom(100000, 999999)}.png");
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
            if (sender is Slider slider)
                slider.Value = Speed;
        }

        private void SliderJump_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Jump = Convert.ToInt32(e.NewValue);
        }

        private void SliderJump_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Slider slider)
                slider.Value = Jump;
        }

        private void SliderAutoplay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AutoPlayTime = Convert.ToInt32(e.NewValue);
        }

        private void SliderAutoplay_Loaded(object sender, RoutedEventArgs e)
        {
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

            if (sender is MenuItem cBtn && cBtn.Header is TextBlock cTxt)
            CustomSelection = cTxt.Text;
            else if (sender is ToolStripItem cTsi)
            CustomSelection = cTsi.Text;

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

                        if (newAspectRatio.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            string[] newInput = newAspectRatio.Input.Split(':');

                            if (newInput.Count() == 2 && IsNumeric(newInput[0]) && IsNumeric(newInput[1]))
                                AspectRatio = newAspectRatio.Input;
                        }

                        break;
                    }

                case "Reset":
                    AspectRatio = string.Empty;
                    break;

                default:
                    {
                        if (CustomSelection.Contains(":"))
                            AspectRatio = CustomSelection;
                        break;
                    }
            }
        }

        private void MenuSettingsAudioMode_Click(object sender, RoutedEventArgs e)
        {
            string CustomSelection = string.Empty;

            if (sender is MenuItem cBtn && cBtn.Header is TextBlock cTxt)
            CustomSelection = cTxt.Text;
            else if (sender is ToolStripItem cTsi)
            CustomSelection = cTsi.Text;

            switch (CustomSelection)
            {
                case "Mono Left":
                    AudioMode = AudioType.MonoL;
                    break;

                case "Mono Right":
                    AudioMode = AudioType.MonoR;
                    break;

                case "Stereo":
                    AudioMode = AudioType.Stereo;
                    break;

                case "Surround":
                    AudioMode = AudioType.Surround;
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
            LoadSubtitle(subtitleDialog.FileName);
        }

        private void MenuSettingsSubtitleDisable_Checked(object sender, RoutedEventArgs e)
        {
            SubtitleDisabled = MainOverlay.MenuSettingsSubtitleDisable.IsChecked;
        }

        private void MenuSettingsOnTop_Click(object sender, RoutedEventArgs e)
        {
            OnTop = MainOverlay.MenuSettingsOnTop.IsChecked;
        }

        private void MenuSettingsAcceleration_Click(object sender, RoutedEventArgs e)
        {
            Acceleration = MainOverlay.MenuSettingsAcceleration.IsChecked;
            ReOpenFile();
        }

        private void MenuSettingsGameMode_Click(object sender, RoutedEventArgs e)
        {
            GameMode = MainOverlay.MenuSettingsGameMode.IsChecked;
        }

        private void MenuSettingsShutDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (!menuItem.IsChecked)
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
                            ShutDown(ShutDownType.AfterN, argInput.Input.ToInt32());
                            SetOverlay($"Shutdown after {shutDownCmd.Arg} episodes");
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
                            ShutDown(ShutDownType.In, argInput.Input.ToInt32());
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

        private void PlaylistImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openPlaylist = new OpenFileDialog()
            {
                Title = "Open Playlist",
                Filter = "Playlist|*.xml",
                SupportMultiDottedExtensions = false,
                CheckFileExists = true,
                ValidateNames = true,
                Multiselect = false
            };

            if (openPlaylist.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Playlist exportPlaylist = new Playlist();
                bool Imported = exportPlaylist.Load(openPlaylist.FileName);

                if (Imported)
                {
                    if (tvShow.LoadPlaylist(exportPlaylist))
                        OpenFile(tvShow.GetCurrentEpisode().FilePath);
                }

                exportPlaylist.ClearFiles();

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void PlaylistExport_Click(object sender, RoutedEventArgs e)
        {
            if (tvShow.episodeList.Count == 0)
            {
                CMBox.Show("Warning", "No playlist to export", MessageCustomHandler.Style.Warning, Buttons.OK);
            }
            else
            {
                SaveFileDialog savePlaylist = new SaveFileDialog()
                { 
                    Title = "Save Playlist",
                    Filter = "Playlist|*.xml",
                    SupportMultiDottedExtensions = false,
                    CheckPathExists = true,
                    ValidateNames = true,
                    AddExtension = true
                };

                if (savePlaylist.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Playlist exportPlaylist = new Playlist();
                    exportPlaylist.AddList(tvShow.episodeList);
                    bool Exported = exportPlaylist.Save(savePlaylist.FileName);
                    exportPlaylist.ClearFiles();

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    if (Exported)
                        CMBox.Show("Info", "Playlist exported", MessageCustomHandler.Style.Info, Buttons.OK);
                }
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
            string SelectedName = string.Empty;

            if (sender is MenuItem dBtn)
                SelectedName = dBtn.Name;

            else if (sender is ToolStripItem dTis)
                SelectedName = dTis.Text;

            switch (SelectedName)
            {
                case "Disable Video":
                case "VideoD":
                    mediaPlayer.SetVideoTrack(-1);
                    break;

                case "Disable Audio":
                case "AudioD":
                    mediaPlayer.SetAudioTrack(-1);
                    break;
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
            string finalVersion = new LibVLC().Version;
            AboutWindow aboutWindow = new AboutWindow(finalVersion);
            aboutWindow.ShowDialog();
        }

        // UI Button Controls
        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (!mediaPlayer.IsPlaying)
            {
                if (mediaPlayer.Media == null) 
                {
                    if (recents.GetList().Count > 0 && File.Exists(recents.GetList().Last()))
                        OpenFile(recents.GetList().Last());
                    else
                        NewFile();
                }
                else
                    mediaPlayer.Play();
            }
            else if(mediaPlayer.CanPause)
                Pause();
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

                UsedScreen = GetCurrentScreen();
                BottomRect = new System.Drawing.Rectangle(UsedScreen.Bounds.X, UsedScreen.Bounds.Height - BottomSize, UsedScreen.Bounds.Width, BottomSize);
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

                    UsedScreen = GetCurrentScreen();
                    BottomRect = new System.Drawing.Rectangle(UsedScreen.Bounds.X, UsedScreen.Bounds.Height - BottomSize, UsedScreen.Bounds.Width, BottomSize);
                }
                else
                {
                    this.WindowState = lastState;
                    this.WindowStyle = WindowStyle.SingleBorderWindow;

                    SetImage(MainOverlay.btnFullscreenImage, Images.btnFullScreenOn);

                    MouseTimer.Stop();
                    MainOverlay.BottomMenu.Margin = new Thickness(0, 0, 0, 0);

                    TopOpen = true;
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