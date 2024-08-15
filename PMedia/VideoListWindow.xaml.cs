using MessageCustomHandler;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PMedia;

/// <summary>
/// Interaction logic for VideoListWindow.xaml
/// </summary>
public partial class VideoListWindow : Window
{
    private TvShow tvShow;

    public void SetTvShow(TvShow tvShow)
    {
        InfoList.Items.Clear();

        if (tvShow == null)
            return;

        if (tvShow.episodeList.Count() <= 0)
            return;

        this.tvShow = tvShow;

        foreach (EpisodeInfo episodeInfo in this.tvShow.episodeList)
        {
            ListViewItem NewItem = new ListViewItem
            {
                Content = $"{episodeInfo.Name} - {episodeInfo.Episode}"
            };

            if (episodeInfo == tvShow.GetCurrentEpisode())
                NewItem.FontWeight = FontWeights.Bold;

            InfoList.Items.Add(NewItem);
        }
    }

    public VideoListWindow(TvShow tvShow)
    {
        InitializeComponent();

        SetTvShow(tvShow);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void MenuCopy_Click(object sender, RoutedEventArgs e)
    {
        if (InfoList.Items.Count <= 0) // Ignore if no items
            return;

        if (InfoList.SelectedItems.Count <= 0) // Ignore if no selected items
            return;

        List<EpisodeInfo> selectedEpisodes = new List<EpisodeInfo>(); // Make a list to be used by all senders

        foreach (ListViewItem item in InfoList.SelectedItems)
        {
            selectedEpisodes.Add(tvShow.episodeList.Find(x => $"{x.Name} - {x.Episode}" == item.Content.ToString()));
        }

        if (sender is MenuItem menuItem)
        {
            if (menuItem.Name.EndsWith("File")) // Copy files
            {
                StringCollection paths = new StringCollection();
                
                foreach (EpisodeInfo episodeInfo in selectedEpisodes)
                {
                    if (!paths.Contains(episodeInfo.FilePath))
                        paths.Add(episodeInfo.FilePath);
                }

                if (paths.Count > 0)
                    Clipboard.SetFileDropList(paths);
            }

            else if (menuItem.Name.EndsWith("Dir")) // Copy directories
            {
                StringCollection paths = new StringCollection();

                foreach (EpisodeInfo episodeInfo in selectedEpisodes)
                {
                    string dir = new System.IO.FileInfo(episodeInfo.FilePath).DirectoryName;

                    if (!paths.Contains(dir))
                        paths.Add(dir);
                }

                if (paths.Count > 0)
                    Clipboard.SetFileDropList(paths);
            }

            else if (menuItem.Name.EndsWith("FilePath")) // Copy file path
            {
                List<string> names = new List<string>();

                foreach (EpisodeInfo episodeInfo in selectedEpisodes)
                {
                    if (!names.Contains(episodeInfo.FilePath))
                        names.Add(episodeInfo.FilePath);
                }

                string finalText = string.Join(Environment.NewLine, names).Trim();

                if (!string.IsNullOrEmpty(finalText))
                    Clipboard.SetText(finalText);
            }

            else if (menuItem.Name.EndsWith("DirPath")) // Copy directories path
            {
                List<string> paths = new List<string>();

                foreach (EpisodeInfo episodeInfo in selectedEpisodes)
                {
                    string dir = new System.IO.FileInfo(episodeInfo.FilePath).DirectoryName;

                    if (!paths.Contains(dir))
                        paths.Add(dir);
                }

                string finalText = string.Join(Environment.NewLine, paths).Trim();

                if (!string.IsNullOrEmpty(finalText))
                    Clipboard.SetText(finalText);
            }

        }
        
    }

    private void MenuOpen_Click(object sender, RoutedEventArgs e)
    {
        if (InfoList.Items.Count <= 0) // Ignore if no items
            return;

        if (InfoList.SelectedItems.Count <= 0) // Ignore if no selected items
            return;

        if (InfoList.SelectedItems.Count > 1)
        {
            CMBox.Show("Warning", "Multiple items selected not supported", MessageCustomHandler.Style.Warning, Buttons.OK);
            return;
        }

        EpisodeInfo selectedEpisode = null; // Make the item to be used by all senders

        foreach (ListViewItem item in InfoList.SelectedItems)
        {
            selectedEpisode = tvShow.episodeList.Find(x => $"{x.Name} - {x.Episode}" == item.Content.ToString());
            break;
        }

        if (sender is MenuItem menuItem)
        {
            if (menuItem.Name.EndsWith("File")) // Open file
            {
                //Process.Start(selectedEpisode.FilePath);
                ((MainWindow)this.Owner).OpenFile(selectedEpisode.FilePath);
            }

            else if (menuItem.Name.EndsWith("Dir")) // open directory
            {
                Process.Start(new System.IO.FileInfo(selectedEpisode.FilePath).DirectoryName);
            }
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        this.Hide();
    }
}
