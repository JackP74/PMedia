using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

using LibVLCSharp.Shared;
using MessageCustomHandler;
using System.Reflection.Emit;

namespace LibVLCSharp.WPF
{
    internal partial class PlayerOverlay : Window
    {
        Window? _wndhost;
        readonly FrameworkElement _bckgnd;
        UIElement? _content;
        readonly Point _zeroPoint = new Point(0, 0);

        public event MouseWheelEventHandler MouseScrollDone;
        public event KeyEventHandler KeyDownDone;

        public event DragStartedEventHandler SliderDragStarted;
        public event DragCompletedEventHandler SliderDragCompleted;
        public event MouseEventHandler SliderMouseEnter;
        public event RoutedPropertyChangedEventHandler<double> SpeedChanged;
        public event RoutedEventHandler SpeedLoaded;
        public event RoutedPropertyChangedEventHandler<double> JumpChanged;
        public event RoutedEventHandler JumpLoaded;
        public event RoutedPropertyChangedEventHandler<double> AutoPlayChanged;
        public event RoutedEventHandler AutoPlayLoaded;

        private string lastOverlayText = string.Empty;
        private System.Windows.Forms.Timer OverlayTimer;
        private int overlayTimeout = 15;
        private delegate void SafeNewOverlay(string NewOverlay);

        internal new UIElement? Content
        {
            get => _content;
            set
            {
                _content = value;
                PART_Content.Children.Clear();
                if (_content != null)
                {
                    PART_Content.Children.Add(_content);
                }
            }
        }

        internal PlayerOverlay(FrameworkElement background)
        {
            InitializeComponent();

            this.ContentRendered += PlayerOverlay_ContentRendered;

            DataContext = background.DataContext;

            _bckgnd = background;
            _bckgnd.DataContextChanged += Background_DataContextChanged;
            _bckgnd.Loaded += Background_Loaded;
            _bckgnd.Unloaded += Background_Unloaded;

            OverlayTimer = new System.Windows.Forms.Timer()
            {
                Interval = 200,
                Enabled = false
            };

            OverlayTimer.Tick += OverlayTimer_Tick;
        }

        private void OverlayTimer_Tick(object sender, EventArgs e)
        {
            string currentText = LabelOverlay.Text;

            if (currentText != lastOverlayText)
            {
                overlayTimeout = 15;
                lastOverlayText = currentText;
            }
            else
            {
                overlayTimeout--;

                if (overlayTimeout <= 0)
                {
                    overlayTimeout = 15;
                    LabelOverlay.Text = string.Empty;
                    OverlayTimer.Stop();
                }
            }
        }

        private void PlayerOverlay_ContentRendered(object sender, EventArgs e)
        {
            try
            {
                Thumb thumb = (SliderMedia.Template.FindName("PART_Track", SliderMedia) as Track)?.Thumb;
                thumb.MouseEnter += new MouseEventHandler(SliderMedia_MouseEnter);
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't initialize thumb, Error: " + ex.Message, MessageCustomHandler.Style.Error, Buttons.OK, ex.ToString());
            }
        }

        void Background_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DataContext = e.NewValue;
        }

        void Background_Unloaded(object sender, RoutedEventArgs e)
        {
            _bckgnd.SizeChanged -= Wndhost_SizeChanged;
            if (_wndhost != null)
            {
                _wndhost.Closing -= Wndhost_Closing;
                _wndhost.LocationChanged -= Wndhost_LocationChanged;
            }

            Hide();
        }

        void Background_Loaded(object sender, RoutedEventArgs e)
        {
            _wndhost = GetWindow(_bckgnd);
            Trace.Assert(_wndhost != null);
            if (_wndhost == null)
            {
                return;
            }

            Owner = _wndhost;

            _wndhost.Closing += Wndhost_Closing;
            _bckgnd.SizeChanged += Wndhost_SizeChanged;
            _wndhost.LocationChanged += Wndhost_LocationChanged;

            try
            {
                var locationFromScreen = _bckgnd.PointToScreen(_zeroPoint);
                var source = PresentationSource.FromVisual(_wndhost);
                var targetPoints = source.CompositionTarget.TransformFromDevice.Transform(locationFromScreen);
                Left = targetPoints.X;
                Top = targetPoints.Y;
                var size = new Point(_bckgnd.ActualWidth, _bckgnd.ActualHeight);
                Height = size.Y;
                Width = size.X;
                Show();
                _wndhost.Focus();
            }
            catch
            {
                Hide();
                throw new VLCException("Unable to create WPF Window in VideoView.");
            }
        }

        void Wndhost_LocationChanged(object? sender, EventArgs e)
        {
            var locationFromScreen = _bckgnd.PointToScreen(_zeroPoint);
            var source = PresentationSource.FromVisual(_wndhost);
            var targetPoints = source.CompositionTarget.TransformFromDevice.Transform(locationFromScreen);
            Left = targetPoints.X;
            Top = targetPoints.Y;
        }

        void Wndhost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var locationFromScreen = _bckgnd.PointToScreen(_zeroPoint);
            var source = PresentationSource.FromVisual(_wndhost);
            var targetPoints = source.CompositionTarget.TransformFromDevice.Transform(locationFromScreen);
            Left = targetPoints.X;
            Top = targetPoints.Y;
            var size = new Point(_bckgnd.ActualWidth, _bckgnd.ActualHeight);
            Height = size.Y;
            Width = size.X;
        }

        void Wndhost_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                _wndhost?.Focus();
            }
        }

        private void SliderMedia_DragStarted(object sender, DragStartedEventArgs e)
        {
            SliderDragStarted?.Invoke(sender, e);
        }

        private void SliderMedia_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            SliderDragCompleted?.Invoke(sender, e);
        }

        private void SliderMedia_MouseEnter(object sender, MouseEventArgs e)
        {
            SliderMouseEnter?.Invoke(sender, e);
        }

        private void SliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SpeedChanged?.Invoke(sender, e);
        }

        private void SliderSpeed_Loaded(object sender, RoutedEventArgs e)
        {
            SpeedLoaded?.Invoke(sender, e);
        }

        private void SliderJump_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            JumpChanged?.Invoke(sender, e);
        }

        private void SliderJump_Loaded(object sender, RoutedEventArgs e)
        {
            JumpLoaded?.Invoke(sender, e);
        }

        private void SliderAutoplay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AutoPlayChanged?.Invoke(sender, e);
        }

        private void SliderAutoplay_Loaded(object sender, RoutedEventArgs e)
        {
            AutoPlayLoaded?.Invoke(sender, e);
        }

        public void SetOverlayText(string newText)
        {
            if (!LabelOverlay.Dispatcher.CheckAccess())
            {
                var d = new SafeNewOverlay(SetOverlayText);
                LabelOverlay.Dispatcher.Invoke(d, new object[] { newText });
            }
            else
            {
                LabelOverlay.Text = newText;
                OverlayTimer.Start();
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MouseScrollDone?.Invoke(sender, e);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            KeyDownDone?.Invoke(sender, e);
        }
    }
}
