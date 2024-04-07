using System.Windows;

namespace PMedia;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static string[] Args;

    void Application_Startup(object sender, StartupEventArgs e)
    {
        if (e.Args.Length > 0)
        {
            Args = e.Args;
        }
    }
}
