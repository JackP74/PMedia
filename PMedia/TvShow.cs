using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PMedia
{
    public class TvShow
    {
        private const string gSeries = "series";
        private const string gSeasonNum = "seasonnum";
        private const string gEpisodeNum = "episodenum";
        private const string gEndEpisodeNum = "endepisodenum";
        private const string gEpisode = "episode";

        private readonly List<Replacement> Replacements;
        private readonly List<MatchPattern> Patterns;

        internal abstract class RegexBase
        {
            protected Regex _regex;

            public bool Enabled { get; set; }

            public string Pattern { get; set; }

            public RegexOptions? RegexOptions { get; set; }

            public override string ToString()
            {
                return string.Format("{0}: Enabled: {1}, Pattern: {2}, Option: {3}", GetType().Name, Enabled, Pattern, RegexOptions);
            }
        }

        internal class Replacement : RegexBase
        {
            public bool IsRegex { get; set; }

            public string ReplaceBy { get; set; }

            public bool Replace(ref string textToReplace)
            {
                if (!Enabled)
                    return false;

                if (IsRegex)
                {
                    if (!string.IsNullOrEmpty(Pattern) && RegexOptions.HasValue)
                        _regex = new Regex(Pattern, RegexOptions.Value);
                    else if (!string.IsNullOrEmpty(Pattern))
                        _regex = new Regex(Pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    var regex = _regex;
                    if (regex != null)
                    {
                        textToReplace = regex.Replace(textToReplace, ReplaceBy);
                    }
                }
                else
                {
                    if (RegexOptions.HasValue)
                        textToReplace = Regex.Replace(textToReplace, Pattern, ReplaceBy, RegexOptions.Value);
                    else
                        textToReplace = Regex.Replace(textToReplace, Pattern, ReplaceBy, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                return true;
            }
        }

        internal class MatchPattern : RegexBase
        {
            public bool GetRegex(out Regex regex)
            {
                if (!Enabled)
                {
                    regex = null;
                    return false;
                }

                if (!string.IsNullOrEmpty(Pattern) && RegexOptions.HasValue)
                    _regex = new Regex(Pattern, RegexOptions.Value);

                regex = _regex;
                return regex != null;
            }
        }

        public EpisodeInfo ParseFile(string FilePath)
        {
            string NameToCheck = FilePath.ToLower();

            // Main Variables
            bool IsTvShow = false;
            string ShowName = string.Empty;
            string ShowEpisode = string.Empty;
            string ShowSearchDir = string.Empty;

            // Temp variables
            string EpisodeTmp = string.Empty;
            string SeasonTmp = string.Empty;
            List<int> EpisodeCntTmp = new List<int>();

            foreach (var replacement in Replacements)
            {
                replacement.Replace(ref NameToCheck);
            }

            foreach (var pattern in Patterns)
            {
                if (IsTvShow == true)
                    continue;

                if (pattern.GetRegex(out Regex matcher)) 
                {

                    Match match = matcher.Match(NameToCheck);

                    if (match.Success == false)
                        continue;

                    // Show Name
                    Group group = match.Groups[gSeries];
                    if (group.Length > 0)
                        ShowName = group.Value;

                    // Episode
                    group = match.Groups[gEpisode];
                    if (group.Length > 0)
                        EpisodeTmp = group.Value;

                    // Season
                    group = match.Groups[gSeasonNum];
                    if (group.Length > 0 && int.TryParse(group.Value, out _))
                        SeasonTmp = group.Value;

                    // Multiple Episode Count
                    group = match.Groups[gEpisodeNum];
                    if (group.Length > 0)
                    {
                        List<int> episodeNums = new List<int>();

                        if (group.Captures.Count > 1)
                        {
                            foreach (Capture capture in group.Captures)
                            {
                                if (int.TryParse(capture.Value, out int episodeNum))
                                    episodeNums.Add(episodeNum);
                            }
                        }

                        else if (match.Groups[gEndEpisodeNum].Length > 0)
                        {
                            if (int.TryParse(group.Value, out int start))
                            {
                                group = match.Groups[gEndEpisodeNum];
                                if (group.Length > 0 && int.TryParse(group.Value, out int end))
                                {
                                    for (int episode = start; episode <= end; episode++)
                                    {
                                        episodeNums.Add(episode);
                                    }
                                }
                            }
                        }

                        else
                        {
                            foreach (Capture capture in group.Captures)
                            {
                                if (int.TryParse(capture.Value, out int episodeNum))
                                    episodeNums.Add(episodeNum);
                            }
                        }

                        if (episodeNums.Count > 0 && !EpisodeCntTmp.SequenceEqual(episodeNums))
                        {
                            EpisodeCntTmp = new List<int>(episodeNums);
                        }
                    }

                    // Final settings
                    if (EpisodeCntTmp.Count() <= 0)
                    {
                        ShowEpisode = "S" +SeasonTmp + "E" + string.Format("00", EpisodeTmp);
                    }
                    else
                    {
                        ShowEpisode = "S" + SeasonTmp + "E" + string.Join("_", EpisodeCntTmp.Select(x => x.ToString("00")));
                    }

                    if (ShowName.StartsWith("[") && ShowName.Count(x => x == ']') == 1)
                    {
                        ShowName = ShowName.Split("]".ToCharArray())[1];
                    }

                    TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

                    ShowName = ShowName.Replace(".", " ");
                    ShowName = textInfo.ToTitleCase(ShowName);

                    return new EpisodeInfo(true, ShowName, ShowEpisode, string.Empty);

                }
            }

            return new EpisodeInfo(false, string.Empty, string.Empty, string.Empty);

        }

        public TvShow()
        {
            // Init default patterns.
            Patterns = new List<MatchPattern>
            {
                // Filename only pattern
            
                // Series\Season...\S01E01* or Series\Season...\1x01*
                new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]*)\\[^\\]*(?<seasonnum>\d+)[^\\]*\\S*(?<seasonnum>\d+)[EX](?<episodenum>\d+)*(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
            
                // MP1 EpisodeScanner recommendations for recordings: Series - (Episode) S1E1, also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
                new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+) - \((?<episode>.*)\) S(?<seasonnum>[0-9]+?)[\s|\.|\-|_]{0,1}E(?<episodenum>[0-9]+?)", RegexOptions = RegexOptions.IgnoreCase },
            
                // "Series 1x1 - Episode" and multi-episodes "Series 1x1_2 - Episodes"
                new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+)\W(?<seasonnum>\d+)x((?<episodenum>\d+)_?)+ - (?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
            
                // "Series S1E01 - Episode" and multi-episodes "Series S1E01_02 - Episodes", also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
                new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+)\WS(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)_?)+ - (?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
            
                // "Series.Name.1x01.Episode.Or.Release.Info"
                new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+).(?<seasonnum>\d+)x((?<episodenum>\d+)_?)+(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
            
                // "Series.Name.S01E01.Episode.Or.Release.Info", also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
                new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+).S(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)_?)+(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
           
                // Folder + filename pattern
            
                // "Series\1\11 - Episode" "Series\Staffel 2\11 - Episode" "Series\Season 3\12 Episode" "Series\3. Season\13-Episode"
                new MatchPattern { Enabled = false, Pattern = @"(?<series>[^\\]*)\\[^\\|\d]*(?<seasonnum>\d+)\D*\\(?<episodenum>\d+)\s*-\s*(?<episode>[^\\]+)\.", RegexOptions = RegexOptions.IgnoreCase },
            
                // "Series.Name.101.Episode.Or.Release.Info", attention: this expression can lead to false matches for every filename with nnn included
                new MatchPattern { Enabled = false, Pattern = @"(?<series>[^\\]+).\W(?<seasonnum>\d{1})(?<episodenum>\d{2})\W(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
            };

            // Init default replacements
            Replacements = new List<Replacement>
            { 
                new Replacement { Enabled = false, Pattern = "720p", ReplaceBy = "", IsRegex = false },
                new Replacement { Enabled = false, Pattern = "1080i", ReplaceBy = "", IsRegex = false },
                new Replacement { Enabled = false, Pattern = "1080p", ReplaceBy = "", IsRegex = false },
                new Replacement { Enabled = false, Pattern = "x264", ReplaceBy = "", IsRegex = false },
                new Replacement { Enabled = false, Pattern = @"(?<!(?:S\d+.?E\\d+\-E\d+.*|S\d+.?E\d+.*|\s\d+x\d+.*))P[ar]*t\s?(\d+)(\s?of\s\d{1,2})?", ReplaceBy = "S01E${1}", IsRegex = true },
            };
        }
    }

    public class EpisodeInfo
    {
        public readonly bool IsTvShow;
        public readonly string Name;
        public readonly string Episode;
        public readonly string SearchDir;

        public EpisodeInfo(bool IsTvShow, string Name, string Episode, string SearchDir)
        {
            this.IsTvShow = IsTvShow;
            this.Name = Name;
            this.Episode = Episode;
            this.SearchDir = SearchDir;
        }

        public override string ToString()
        {
            return $"Is Tv Show: {IsTvShow.ToString()}  Name: {Name}  Episode: {Episode}  Search Dir: {SearchDir}";
        }
    }
}
