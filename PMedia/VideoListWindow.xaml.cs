using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PMedia
{
    /// <summary>
    /// Interaction logic for VideoListWindow.xaml
    /// </summary>
    public partial class VideoListWindow : Window
    {
        public VideoListWindow(TvShow tvShow)
        {
            InitializeComponent();

            if (tvShow == null)
                return;

            if (tvShow.episodeList.Count() <= 0)
                return;

            foreach ( EpisodeInfo episodeInfo in tvShow.episodeList)
            {
                string name = $"{episodeInfo.Name} - {episodeInfo.Episode}";

                if (episodeInfo == tvShow.GetCurrentEpisode())
                    name = @"# " + name;

                InfoList.Items.Add(name);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
