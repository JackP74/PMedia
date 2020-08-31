using System;
using System.Windows;

using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

namespace PMedia
{
    /// <summary>
    /// Interaction logic for TestWind.xaml
    /// </summary>
    public partial class TestWind : Window
    {
        public TestWind()
        {
            InitializeComponent();

            PlayerOverlay MainOverlay = new PlayerOverlay(WinHost);

            string vlcPath = AppDomain.CurrentDomain.BaseDirectory;
            if (Environment.Is64BitProcess) { vlcPath += @"\libvlc\win-x64"; } else { vlcPath += @"\libvlc\win-x86"; }

            Core.Initialize(vlcPath);

            LibVLC libVLC = new LibVLC();

            MediaPlayer mediaPlayer = new MediaPlayer(libVLC)
            {
                EnableMouseInput = false
            };

            LibVLCSharp.WinForms.VideoView videoView = new LibVLCSharp.WinForms.VideoView()
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.Black
            };

            videoView.MediaPlayer = mediaPlayer;

            TransparentPanel overlayPanel = new TransparentPanel()
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                AllowDrop = true
            };

            System.Windows.Forms.Panel videoPanel = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.Black
            };

            // context menu
            System.Windows.Forms.ContextMenuStrip PlayerContextMenu = new System.Windows.Forms.ContextMenuStrip();
            PlayerContextMenu.Items.Add("Play/Pause", null, delegate 
            {
                if (!mediaPlayer.IsPlaying)
                {
                    if (mediaPlayer.State == VLCState.Paused)
                    {
                        mediaPlayer.Pause();
                    }
                    else
                    {
                        mediaPlayer.Play(new Media(libVLC, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4")));
                    }
                }
                else if (mediaPlayer.CanPause)
                {
                    mediaPlayer.Pause();
                }
            });

            overlayPanel.ContextMenuStrip = PlayerContextMenu;

            videoPanel.Controls.Add(overlayPanel);
            videoPanel.Controls.Add(videoView);

            WinHost.Child = videoPanel;

            MainOverlay.btnPlay.Click += delegate
            {
                if (!mediaPlayer.IsPlaying)
                {
                    if (mediaPlayer.State == VLCState.Paused)
                    {
                        mediaPlayer.Pause();
                    }
                    else
                    {
                        mediaPlayer.Play(new Media(libVLC, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4")));
                    }
                }
                else if(mediaPlayer.CanPause)
                {
                    mediaPlayer.Pause();
                }
            };


            MainOverlay.btnStop.Click += delegate
            {
                mediaPlayer.Stop();
            };

        }
    }
}
