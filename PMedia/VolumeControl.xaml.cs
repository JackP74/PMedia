using MessageCustomHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PMedia
{
    /// <summary>
    /// Interaction logic for VolumeControl.xaml
    /// </summary>
    public partial class VolumeControl : UserControl
    {
        public VolumeControl()
        {
            InitializeComponent();

            this.Loaded += delegate
            {
                try
                {
                    Thumb thumb = (VolumeSlider.Template.FindName("PART_Track", VolumeSlider) as Track).Thumb;
                    thumb.MouseEnter += new MouseEventHandler(VolumeSlider_MouseEnter);
                }
                catch (Exception ex)
                {
                    CMBox.Show("Error", "Couldn't initialize thumb", MessageCustomHandler.Style.Error, Buttons.OK, null, ex.ToString());
                }

                LinearGradientBrush linearGradientBrush = new LinearGradientBrush();
                linearGradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb((byte)255, (byte)123, (byte)223, (byte)246), 0));
                linearGradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb((byte)255, (byte)120, (byte)187, (byte)239), 1));

                VolumeSlider.Background = linearGradientBrush;
                VolumeSlider.BorderBrush = linearGradientBrush;
                VolumeSlider.ValueChanged += (s, e) => 
                {
                    labelVolume.Content = Convert.ToInt32(e.NewValue).ToString() + @"%";
                };

                labelVolume.Foreground = linearGradientBrush;
            };
        }

        private void VolumeSlider_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.MouseDevice.Captured == null)
            {
                MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonDownEvent
                };

                (sender as Thumb).RaiseEvent(args);

            }
        }
    }
}
