﻿#region "Imports"
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Timers;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using MessageCustomHandler;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using System.Windows.Forms;
using System.Diagnostics;
#endregion

namespace PMedia
{
    public partial class MainWindow : Window
    {
        #region "Win32 Imports"
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);
        #endregion

        #region "Variables"
        private readonly Settings settings;
        MediaPlayer mediaPlayer;
        private LibVLC libVLC;

        private readonly List<JumpCommand> jumpCommands;
        private static bool isSliderControl = false;

        readonly string TempPath = @"E:\qbittorrent\Avenue.5.S01E04.Then.Who.Was.That.on.the.Ladder.1080p.AMZN.WEB-DL.DDP5.1.H.264-NTb.mkv";

        private bool isMute = false;
        private int volume = 0;

        private Direction textDirection = Direction.Forward;
        private WindowState lastState = WindowState.Normal;

        private const int TopSize = 23;
        private const int BottomSize = 40;
        private const int MouseOffset = 15;

        System.Windows.Forms.Timer MouseTimer;

        Rect rect = new Rect();
        #endregion

        #region "Proprieties"
        private bool Mute
        {
            set
            {
                isMute = value;

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
                return isMute;
            }
        }

        private int Volume
        {
            set
            {
                SetSliderValue(VolumeSlider.VolumeSlider, value);
                SetLabelContent(VolumeSlider.labelVolume, value.ToString() + @"%");

                volume = value;

                if(mediaPlayer != null && mediaPlayer.Media != null)
                {
                    StartThread(() =>
                    {
                        mediaPlayer.Volume = value;
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

                if (Mute == true)
                    Mute = false;

                settings.Volume = value;
            }
            get
            {
                return volume;
            }
        }

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
        #endregion

        #region "Enums"
        private enum Direction
        {
            Forward = 0,
            Backward = 1
        }
        #endregion

        #region "Structs"
        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }
        #endregion

        #region "Internal Classes"
        internal static class Images
        {
            public static string btnPlay = "Resources/btnPlay.png";
            public static string btnPause = "Resources/btnPause.png";
            public static string btnStop = "Resources/btnStop.png";
            public static string btnForward = "Resources/btnForward.png";
            public static string btnBackward = "Resources/btnBackward.png";
            public static string btnMute = "Resources/btnMute.png";
            public static string btnVolume1 = "Resources/btnVolume1.png";
            public static string btnVolume2 = "Resources/btnVolume2.png";
            public static string btnVolume3 = "Resources/btnVolume3.png";
            public static string btnFullScreenOn = "Resources/btnFullScreenOn.png";
            public static string btnFullScreenOff = "Resources/btnFullScreenOff.png";
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
        #endregion

        #region "Functions"

        #region "Timers"
        private void CreateJumpTimer()
        {
            System.Timers.Timer JumpTimer = new System.Timers.Timer()
            {
                Interval = 250
            };

            JumpTimer.Elapsed += delegate
            {

                if (settings.Jump == 0)
                    throw new Exception("Jump value cannot be 0");

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

                    mediaPlayer.Time = finalTime;

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
                Interval = 1000
            };

            SaveTimer.Elapsed += delegate
            {

                if (settings.NeedsSaving == true)
                    settings.Save();

            };

            SaveTimer.Start();
        }

        private void CreateTitleTimer()
        {
            //Doesn't work, labelTitle - no way to get actual width and not render size yet
            return;


            System.Windows.Forms.Timer TitleTimer = new System.Windows.Forms.Timer();
            TitleTimer.Interval = 40;

            TitleTimer.Tick += delegate
            {

                if (labelTitle.Width <= labelTitlePanel.Width)
                    return;

                switch (textDirection)
                {
                    case Direction.Forward:
                        {
                            if (Math.Abs(labelTitle.Margin.Left) < Math.Abs(labelTitle.Width - labelTitlePanel.Width))
                            {
                                labelTitle.Margin = new Thickness(labelTitle.Margin.Left - 1, 0, 0, 0);
                            }
                            else
                            {
                                //labelTitle.Margin = new Thickness(-Math.Abs(labelTitle.Width - labelTitlePanel.Width), 0, 0, 0);
                                textDirection = Direction.Backward;
                            }

                            break;
                        }

                    case Direction.Backward:
                        {

                            break;
                        }

                    default:
                        {
                            textDirection = Direction.Forward;
                            labelTitle.Margin = new Thickness(0);

                            break;
                        }
                }

            };

            TitleTimer.Start();
        }

        private void CreateMouseTimer()
        {
            MouseTimer = new System.Windows.Forms.Timer
            {
                Interval = 100
            };

            MouseTimer.Tick += delegate
            {

                System.Drawing.Point curPos = System.Windows.Forms.Cursor.Position;

                if (curPos.Y < (TopSize + MouseOffset) && curPos.X >= Location.X && curPos.X <= Location.X + this.ActualWidth)
                {
                    MainGrid.RowDefinitions[0].Height = new GridLength(TopSize);
                }
                else if (MainGrid.RowDefinitions[0].Height.Value >= TopSize)
                {
                    MainGrid.RowDefinitions[0].Height = new GridLength(0);
                }

                if (curPos.Y > ((Location.Y + this.ActualHeight) - (BottomSize + MouseOffset)) && curPos.X >= Location.X && curPos.X <= Location.X + this.ActualWidth)
                {
                    MainGrid.RowDefinitions[2].Height = new GridLength(BottomSize);
                }
                else if (MainGrid.RowDefinitions[2].Height.Value >= BottomSize)
                {
                    MainGrid.RowDefinitions[2].Height = new GridLength(0);
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

        private void SetImage(System.Windows.Controls.Image image, string newImage)
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

        private void SetLabelContent(System.Windows.Controls.Label label, string newText)
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
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            string vlcPath = AppDomain.CurrentDomain.BaseDirectory;
            if (Environment.Is64BitProcess == true) { vlcPath += @"\libvlc\win-x64"; } else { vlcPath += @"\libvlc\win-x86"; }

            Core.Initialize(vlcPath);
            VideoView.Loaded += VideoView_Loaded;

            settings = new Settings();
            settings.Load();

            jumpCommands = new List<JumpCommand>();
            SetLabelsColors();

            this.Loaded += MainWindow_Loaded;
            this.ContentRendered += MainWindow_ContentRendered;
            this.StateChanged += MainWindow_StateChanged;
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

        private void OpenFile(string FilePath)
        {
            if (Extensions.IsVideo(FilePath))
            {
                StopMediaPlayer();

                StartThread(() =>
                {
                    mediaPlayer.Play(new Media(libVLC, FilePath, FromType.FromPath));
                });
            }
        }
        #endregion

        #region "MediaPlayer Functions"
        private void StopMediaPlayer()
        {
            ThreadPool.QueueUserWorkItem(_ => {
                this.mediaPlayer.Stop();

                if (this.mediaPlayer.Media != null)
                    this.mediaPlayer.Media.Dispose();
            });
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
        #endregion

        #region "MediaPlayer Handles"
        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            StartThread(() =>
            {
                SetImage(btnPlayImage, Images.btnPause);
            });
        }

        private void MediaPlayer_Paused(object sender, EventArgs e)
        {
            StartThread(() =>
            {
                SetImage(btnPlayImage, Images.btnPlay);
            });
        }

        private void MediaPlayer_MediaChanged(object sender, MediaPlayerMediaChangedEventArgs e)
        {
            StartThread(() =>
            {
                int InSleep = 0;

                while (mediaPlayer.Media.Tracks.Count() <= 0 && mediaPlayer.Media.Duration <= 0 && InSleep <= 15)
                {
                    Thread.Sleep(200);
                    InSleep += 1;
                }

                SetLabelContent(labelTitle, new System.IO.FileInfo(System.Net.WebUtility.UrlDecode(new Uri(e.Media.Mrl).AbsolutePath)).Name);
                SetLabelContent(labelLenght, TimeSpan.FromMilliseconds(mediaPlayer.Media.Duration).ToString(@"hh\:mm\:ss"));
                SetSliderValue(SliderMedia, 0);
                SetSliderMaximum(SliderMedia, Convert.ToInt32(mediaPlayer.Media.Duration / 1000));

                if (Mute == true)
                {
                    mediaPlayer.Mute = true;
                }
                else
                {
                    mediaPlayer.Volume = Volume;
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
            });
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            StopMediaPlayer();
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {
            SetImage(btnPlayImage, Images.btnPlay);
            SetLabelContent(labelTitle, string.Empty);

            SetLabelContent(labelPosition, "00:00:00");
            SetLabelContent(labelLenght, "00:00:00");

            SetSliderMaximum(SliderMedia, 1);
            SetSliderValue(SliderMedia, 0);

            if (mediaPlayer.Media != null)
                mediaPlayer.Media.Dispose();
            mediaPlayer.Media = null;
        }
        #endregion

        #region "Handles"
        // Initial Loading
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Volume = settings.Volume;

            VolumeSlider.VolumeSlider.ValueChanged += (s, nE) =>
            {
                Volume = Convert.ToInt32(nE.NewValue);
            };

            CreateJumpTimer();
            CreateSaveTimer();
            CreateMouseTimer();

            this.MouseLeave += MainWindow_MouseLeave;
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            //CreateTitleTimer(); //Not working, TO DO

            try
            {
                Thumb thumb = (SliderMedia.Template.FindName("PART_Track", SliderMedia) as Track).Thumb;
                thumb.MouseEnter += new System.Windows.Input.MouseEventHandler(SliderMedia_MouseEnter);
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't initialize thumb", MessageCustomHandler.Style.Error, Buttons.OK, null, ex.ToString());
            }
        }

        private void VideoView_Loaded(object sender, RoutedEventArgs e)
        {
            libVLC = new LibVLC();
            mediaPlayer = new MediaPlayer(libVLC);
            VideoView.MediaPlayer = mediaPlayer;

            mediaPlayer.Playing += MediaPlayer_Playing;
            mediaPlayer.Paused += MediaPlayer_Paused;
            mediaPlayer.MediaChanged += MediaPlayer_MediaChanged;
            mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            mediaPlayer.EndReached += MediaPlayer_EndReached;
            mediaPlayer.Stopped += MediaPlayer_Stopped;
        }

        // Form Events
        private void MainWindow_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
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
                mediaPlayer.Pause();
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.IsPlaying || mediaPlayer.State == VLCState.Paused)
            {
                StopMediaPlayer();
            }

        }

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            if(mediaPlayer.IsSeekable)
            {
                jumpCommands.Add(new JumpCommand(JumpCommand.Direction.Forward, settings.Jump));
            }
        }

        private void BtnBackward_Click(object sender, RoutedEventArgs e)
        {
            if(mediaPlayer.IsSeekable)
            {
                jumpCommands.Add(new JumpCommand(JumpCommand.Direction.Backward, settings.Jump));
            }
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

                Grid.SetRow(VideoView, 1);
                Grid.SetRowSpan(VideoView, 1);

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

                    Grid.SetRow(VideoView, 1);
                    Grid.SetRowSpan(VideoView, 1);

                    MainGrid.RowDefinitions[0].Height = new GridLength(0);
                    MainGrid.RowDefinitions[2].Height = new GridLength(0);
                }
                else
                {
                    this.WindowState = lastState;
                    this.WindowStyle = WindowStyle.SingleBorderWindow;

                    SetImage(btnFullscreenImage, Images.btnFullScreenOn);

                    Grid.SetRow(VideoView, 1);
                    Grid.SetRowSpan(VideoView, 1);

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

        private void SliderMedia_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
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
        #endregion

        private void MenuFileMediaInfo_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
