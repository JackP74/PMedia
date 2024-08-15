using System.Windows;

namespace PMedia;

/// <summary>
/// Interaction logic for AboutWindow.xaml
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow(string libVersion)
    {
        InitializeComponent();

        LabelVersion.Content = $"VLC Lib Version: {libVersion}";
    }
}
