using LibVLCSharp.Shared;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;

namespace PMedia
{
    /// <summary>
    /// Interaction logic for MediaInfoWindow.xaml
    /// </summary>
    public partial class MediaInfoWindow : Window
    {
        internal class ListItem
        {
            public string Name { get; set; }

            public string Value { get; set; }
        }

        public MediaInfoWindow(Media media)
        {
            InitializeComponent();

            ColumnName.DisplayMemberBinding = new Binding("Name");
            ColumnValue.DisplayMemberBinding = new Binding("Value");

            MediaInfo mediaInfo = new MediaInfo(media);

            int iTrack = 0;

            foreach(MediaVideoInfo videoInfo in mediaInfo.videoInfos)
            {
                if (InfoList.Items.Count > 0)
                {
                    InfoList.Items.Add(new ListItem { Name = string.Empty, Value = string.Empty });
                }

                InfoList.Items.Add(new ListItem { Name = "Video Track " + iTrack.ToString(), Value = string.Empty });
                InfoList.Items.Add(new ListItem { Name = "Codec", Value = videoInfo.Codec });
                InfoList.Items.Add(new ListItem { Name = "Resolution", Value = videoInfo.Resolution });
                InfoList.Items.Add(new ListItem { Name = "Frame Rate", Value = videoInfo.FrameRate });
                InfoList.Items.Add(new ListItem { Name = "Language", Value = videoInfo.Language });

                iTrack++;
            }

            foreach (MediaAudioInfo audioInfo in mediaInfo.audioInfos)
            {
                if (InfoList.Items.Count > 0)
                {
                    InfoList.Items.Add(new ListItem { Name = string.Empty, Value = string.Empty });
                }

                InfoList.Items.Add(new ListItem { Name = "Audio Track " + iTrack.ToString(), Value = string.Empty });
                InfoList.Items.Add(new ListItem { Name = "Codec", Value = audioInfo.Codec });
                InfoList.Items.Add(new ListItem { Name = "Language", Value = audioInfo.Language });
                InfoList.Items.Add(new ListItem { Name = "Channels", Value = audioInfo.Channels });
                InfoList.Items.Add(new ListItem { Name = "Rate", Value = audioInfo.Rate });

                iTrack++;
            }

            foreach (MediaTextInfo textInfo in mediaInfo.textInfos)
            {
                if (InfoList.Items.Count > 0)
                {
                    InfoList.Items.Add(new ListItem { Name = string.Empty, Value = string.Empty });
                }

                InfoList.Items.Add(new ListItem { Name = "Subtitle Track " + iTrack.ToString(), Value = string.Empty });
                InfoList.Items.Add(new ListItem { Name = "Codec", Value = textInfo.Codec });
                InfoList.Items.Add(new ListItem { Name = "Language", Value = textInfo.Language });

                iTrack++;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        internal class MediaInfo
        {
            readonly public List<MediaVideoInfo> videoInfos;
            readonly public List<MediaAudioInfo> audioInfos;
            readonly public List<MediaTextInfo> textInfos;

            public MediaInfo(Media media)
            {
                videoInfos = new List<MediaVideoInfo>();
                audioInfos = new List<MediaAudioInfo>();
                textInfos = new List<MediaTextInfo>();

                if (media != null)
                {
                    foreach (MediaTrack track in media.Tracks)
                    {
                        switch (track.TrackType)
                        {
                            case TrackType.Video:
                                {
                                    try
                                    {
                                        string Codec = media.CodecDescription(TrackType.Video, track.Codec);
                                        string Resolution = track.Data.Video.Width.ToString() + "x" + track.Data.Video.Height.ToString();
                                        string FrameRate = string.Format((track.Data.Video.FrameRateNum / track.Data.Video.FrameRateDen).ToString(), "0.000");
                                        string Language = track.Language;

                                        videoInfos.Add(new MediaVideoInfo(Codec, Resolution, FrameRate, Language));
                                    }
                                    catch
                                    {
                                        videoInfos.Add(new MediaVideoInfo("Error", "Error", "Error", "Error"));
                                    }

                                    break;
                                }

                            case TrackType.Audio:
                                {
                                    try
                                    {
                                        string Codec = media.CodecDescription(TrackType.Audio, track.Codec);
                                        string Language = track.Language;
                                        string Channels = track.Data.Audio.Channels.ToString();
                                        string Rate = track.Data.Audio.Rate.ToString() + " Hz";

                                        audioInfos.Add(new MediaAudioInfo(Codec, Language, Channels, Rate));
                                    }
                                    catch
                                    {
                                        audioInfos.Add(new MediaAudioInfo("Error", "Error", "Error", "Error"));
                                    }

                                    break;
                                }

                            case TrackType.Text:
                                {
                                    try
                                    {
                                        string Codec = media.CodecDescription(TrackType.Text, track.Codec);
                                        string Language = track.Language;

                                        textInfos.Add(new MediaTextInfo(Codec, Language));
                                    }
                                    catch
                                    {
                                        textInfos.Add(new MediaTextInfo("Error", "Error"));
                                    }

                                    break;
                                }

                            default:
                                break;
                        }
                    }
                }
            }
        }

        internal class MediaVideoInfo
        {
            readonly public string Codec;
            readonly public string Resolution;
            readonly public string FrameRate;
            readonly public string Language;

            public MediaVideoInfo(string Codec, string Resolution, string FrameRate, string Language)
            {
                this.Codec = Codec;
                this.Resolution = Resolution;
                this.FrameRate = FrameRate;
                this.Language = Language;
            }
        }

        internal class MediaAudioInfo
        {
            readonly public string Codec;
            readonly public string Language;
            readonly public string Channels;
            readonly public string Rate;

            public MediaAudioInfo(string Codec, string Language, string Channels, string Rate)
            {
                this.Codec = Codec;
                this.Language = Language;
                this.Channels = Channels;
                this.Rate = Language;
            }
        }

        internal class MediaTextInfo
        {
            readonly public string Codec;
            readonly public string Language;

            public MediaTextInfo(string Codec, string Language)
            {
                this.Codec = Codec;
                this.Language = Language;
            }
        }
    }
}
