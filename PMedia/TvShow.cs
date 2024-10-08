﻿#region "Imports"
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

using MessageCustomHandler;
#endregion

namespace PMedia;

public class TvShow
{
    #region "Variables"
    // For first letter in word
    private readonly TextInfo textInfo = new CultureInfo("en-US", false).TextInfo; 

    // For regex groups
    private const string gSeries = "series";
    private const string gSeasonNum = "seasonnum";
    private const string gEpisodeNum = "episodenum";
    private const string gEndEpisodeNum = "endepisodenum";
    private const string gEpisode = "episode";

    // For regex search
    private readonly List<Replacement> Replacements;
    private readonly List<MatchPattern> Patterns;

    // For easy access to empty episode info
    public readonly EpisodeInfo EmptyEpisode;

    // Variables
    public List<EpisodeInfo> episodeList;

    private EpisodeInfo nextEpisode;
    private EpisodeInfo currentEpisode;
    private EpisodeInfo previousEpisode;

    public bool neverSet = true;
    #endregion

    #region "Internal Classes"
    internal abstract class RegexBase
    {
        protected Regex _regex;

        public bool Enabled { get; set; }

        public string Pattern { get; set; }

        public RegexOptions? RegexOptions { get; set; }

        public override string ToString()
        {
            return string.Format($"{GetType().Name}: Enabled: {Enabled}, Pattern: {Pattern}, Option: {RegexOptions}");
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
    #endregion

    #region "Functions"
    public TvShow()
    {
        // Init default patterns.
        Patterns = new List<MatchPattern>
        {
            // Filename only pattern
        
            // Series\Season...\S01E01* or Series\Season...\1x01*
            new() { Enabled = true, Pattern = @"(?<series>[^\\]*)\\[^\\]*(?<seasonnum>\d+)[^\\]*\\S*(?<seasonnum>\d+)[EX](?<episodenum>\d+)*(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
        
            // MP1 EpisodeScanner recommendations for recordings: Series - (Episode) S1E1, also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
            new() { Enabled = true, Pattern = @"(?<series>[^\\]+) - \((?<episode>.*)\) S(?<seasonnum>[0-9]+?)[\s|\.|\-|_]{0,1}E(?<episodenum>[0-9]+?)", RegexOptions = RegexOptions.IgnoreCase },
        
            // "Series 1x1 - Episode" and multi-episodes "Series 1x1_2 - Episodes"
            new() { Enabled = true, Pattern = @"(?<series>[^\\]+)\W(?<seasonnum>\d+)x((?<episodenum>\d+)_?)+ - (?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
        
            // "Series S1E01 - Episode" and multi-episodes "Series S1E01_02 - Episodes", also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
            new() { Enabled = true, Pattern = @"(?<series>[^\\]+)\WS(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)_?)+ - (?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
        
            // "Series.Name.1x01.Episode.Or.Release.Info"
            new() { Enabled = true, Pattern = @"(?<series>[^\\]+).(?<seasonnum>\d+)x((?<episodenum>\d+)_?)+(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
        
            // "Series.Name.S01E01.Episode.Or.Release.Info", also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
            new() { Enabled = true, Pattern = @"(?<series>[^\\]+).S(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)_?)+(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
       
            // Folder + filename pattern
        
            // "Series\1\11 - Episode" "Series\Staffel 2\11 - Episode" "Series\Season 3\12 Episode" "Series\3. Season\13-Episode"
            new() { Enabled = false, Pattern = @"(?<series>[^\\]*)\\[^\\|\d]*(?<seasonnum>\d+)\D*\\(?<episodenum>\d+)\s*-\s*(?<episode>[^\\]+)\.", RegexOptions = RegexOptions.IgnoreCase },
        
            // "Series.Name.101.Episode.Or.Release.Info", attention: this expression can lead to false matches for every filename with nnn included
            new() { Enabled = false, Pattern = @"(?<series>[^\\]+).\W(?<seasonnum>\d{1})(?<episodenum>\d{2})\W(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
        };

        // Init default replacements
        Replacements = new List<Replacement>
        {
            new() { Enabled = true, Pattern = "360p", ReplaceBy = "", IsRegex = false },
            new() { Enabled = true, Pattern = "480p", ReplaceBy = "", IsRegex = false },
            new() { Enabled = true, Pattern = "720p", ReplaceBy = "", IsRegex = false },
            new() { Enabled = true, Pattern = "1080i", ReplaceBy = "", IsRegex = false },
            new() { Enabled = true, Pattern = "1080p", ReplaceBy = "", IsRegex = false },
            new() { Enabled = true, Pattern = "x264", ReplaceBy = "", IsRegex = false },
            new() { Enabled = true, Pattern = "x265", ReplaceBy = "", IsRegex = false },
            new() { Enabled = true, Pattern = @"(?<!(?:S\d+.?E\\d+\-E\d+.*|S\d+.?E\d+.*|\s\d+x\d+.*))P[ar]*t\s?(\d+)(\s?of\s\d{1,2})?", ReplaceBy = "S01E${1}", IsRegex = true },
        };

        // Init variables
        EmptyEpisode = new EpisodeInfo();
        episodeList = new List<EpisodeInfo>();
        nextEpisode = new EpisodeInfo();
        currentEpisode = new EpisodeInfo();
        previousEpisode = new EpisodeInfo();
    }

    public TvShow(TvShow tvShow)
    {
        EmptyEpisode = new EpisodeInfo();
        episodeList = tvShow.episodeList;
        nextEpisode = tvShow.NextEpisode();
        currentEpisode = tvShow.currentEpisode;
        previousEpisode = tvShow.PreviousEpisode();
    }

    public void Load(string FilePath)
    {
        if (Patterns == null || Replacements == null)
        {
            throw new Exception("TvShow not made to load from an other TvShow");
        }

        if (episodeList.Count() <= 0 || episodeList.Where(x => x.FilePath == FilePath).Count() <= 0) // Is not part of the current list
        {
            // Clear everything
            episodeList.Clear();
            nextEpisode = new EpisodeInfo();
            currentEpisode = new EpisodeInfo();
            previousEpisode = new EpisodeInfo();

            // New video info
            currentEpisode = ParseFile(FilePath);

            if (currentEpisode.IsTvShow && currentEpisode.Search && !string.IsNullOrWhiteSpace(currentEpisode.SearchDir)) // Is TvShow
            {
                episodeList.Add(currentEpisode);

                List<string> dirList = new() { currentEpisode.SearchDir }; // New List of dirs to check

                foreach (string dir in Directory.GetDirectories(currentEpisode.SearchDir))
                {
                    if (HasDirPermission(dir)) // Don't check dirs that the user doesn't have access to
                    {
                        // Only check dirs that start with the same name to limit search count
                        if (GetDirName(dir).StartsWith(currentEpisode.Name))
                        {
                            dirList.Add(dir);
                        }
                    }
                }

                List<string> fileList = new(); // File list to check

                foreach (string dir in dirList)
                {
                    // Only get files with video extensions to limit check count
                    var foundList = GetFilesByExtensions(new DirectoryInfo(dir), Extensions.Video.ToArray());
                    fileList.AddRange(foundList.ToList());
                }

                foreach (string file in fileList.Distinct()) // TvShow check
                {
                    if (file != FilePath) // Don't re-check current file
                    {
                        EpisodeInfo currentInfo = ParseFile(file);

                        if (currentInfo.IsTvShow && currentInfo.Name == currentEpisode.Name && !episodeList.Contains(currentInfo)) // Check if TvShow and names match
                        {
                            episodeList.Add(currentInfo);
                        }
                    }
                }

                episodeList = episodeList.Distinct().ToList();
                episodeList = episodeList.GroupBy(ep => ep.Episode).Select(ep => ep.First()).ToList();

                episodeList.Sort((a, b) => a.Episode.CompareTo(b.Episode));

                // Next/Previous episodes
                int currentIdx = episodeList.IndexOf(currentEpisode);

                if (currentIdx > 0)
                    previousEpisode = episodeList[currentIdx - 1];

                if (currentIdx < episodeList.Count() - 1)
                    nextEpisode = episodeList[currentIdx + 1];
            }
            else
            {
                episodeList.Add(currentEpisode); // Is not a tv show but add it empty for uniformity
            }
        }
        else // Part of current list
        {
            // Clear Next/Previous
            nextEpisode = new EpisodeInfo();
            currentEpisode = new EpisodeInfo();
            previousEpisode = new EpisodeInfo();

            // Set new current/next/previous episode
            currentEpisode = episodeList.Find(x => x.FilePath == FilePath);

            int currentIdx = episodeList.IndexOf(currentEpisode);

            if (currentIdx > 0)
                previousEpisode = episodeList[currentIdx - 1];

            if (currentIdx < episodeList.Count() - 1)
                nextEpisode = episodeList[currentIdx + 1];
        }

        neverSet = false;
    }

    public bool LoadPlaylist(Playlist newPlaylist)
    {
        if (newPlaylist.currentList.files.Count() == 0)
        {
            CMBox.Show("Warning", "Empty playlist", Style.Warning, Buttons.OK);
            return false;
        }
        else
        {
            episodeList.Clear();
            nextEpisode = new EpisodeInfo();
            currentEpisode = new EpisodeInfo();
            previousEpisode = new EpisodeInfo();

            episodeList.AddRange(newPlaylist.currentList.files);
            currentEpisode = episodeList[0];

            if (episodeList.Count() > 1)
                nextEpisode = episodeList[1];

            return true;
        }
    }

    public EpisodeInfo ParseFile(string FilePath, bool ByPassExists = false)
    {
        if (!ByPassExists && !File.Exists(FilePath)) return EmptyEpisode; // File doesn't exist

        string NameToCheck = new FileInfo(FilePath).FullName;
        DirectoryInfo DirToCheck = new FileInfo(FilePath).Directory;

        if (NameToCheck.Contains("sample"))
            return EmptyEpisode;

        if (DirToCheck.Name.Contains("sample"))
            return EmptyEpisode;

        // Main Variables
        string ShowName = string.Empty;
        string ShowEpisode = string.Empty;
        string ShowSearchDir = string.Empty;

        // Temp variables
        string EpisodeTmp = string.Empty;
        string SeasonTmp = string.Empty;
        List<int> EpisodeCntTmp = new();

        foreach (var replacement in Replacements)
        {
            replacement.Replace(ref NameToCheck);
        }

        foreach (var pattern in Patterns)
        {

            if (pattern.GetRegex(out Regex matcher))
            {
                Match match = matcher.Match(NameToCheck);

                if (!match.Success)
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
                    List<int> episodeNums = new();

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
                    ShowEpisode = "S" + SeasonTmp + "E" + string.Format("00", EpisodeTmp);
                }
                else
                {
                    ShowEpisode = "S" + SeasonTmp + "E" + string.Join("_", EpisodeCntTmp.Select(x => x.ToString("00")));
                }

                // Uniform name
                ShowName = ShowName.Replace(".", " ");
                ShowName = ShowName.Replace("'", string.Empty);
                ShowName = ShowName.Replace("-", string.Empty);
                ShowName = Regex.Replace(ShowName, @"\([^)]*\)", "");
                ShowName = Regex.Replace(ShowName, @"\s{2,}", " ");
                ShowName = textInfo.ToTitleCase(ShowName);

                if (ShowName.StartsWith("[") && ShowName.Count(x => x == ']') == 1)
                    ShowName = ShowName.Split("]".ToCharArray())[1];

                // Search Dir for all episodes
                if (DirToCheck.FullName == DirToCheck.Root.FullName)
                {
                    ShowSearchDir = string.Empty; // Don't check if root directory, can contain too many files
                }
                else
                {
                    DirectoryInfo currentDir = DirToCheck;

                    while (true)
                    {
                        if (currentDir.Parent != null && currentDir.Parent.FullName != DirToCheck.Root.FullName)
                        {
                            currentDir = currentDir.Parent;
                        }
                        else
                        {
                            break;
                        }
                    }

                    ShowSearchDir = currentDir.FullName;
                }

                return new EpisodeInfo(true, ShowName.Trim(), ShowEpisode.Trim(), ShowSearchDir.Trim(), FilePath.Trim());
            }
        }

        return ParseMovie(FilePath);
    }

    public EpisodeInfo ParseMovie(string FilePath)
    {
        string pattern = @"^(?<Name>.+?)(?!\.[12]\d\d\d\.\d{,3}[ip]\.)\.(?<Year>\d\d\d\d)\.(?<Resolution>[^.]+)\.(?<Format>[^.]+)";
        string NameToCheck = new FileInfo(FilePath).Name.Replace(" ", ".");

        foreach (var item in Regex.Matches(NameToCheck, pattern, RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace)
            .OfType<Match>().Select(mt => new {
                Movie = Regex.Replace(mt.Groups["Name"].Value, @"\.", " "),
                Year = mt.Groups["Year"].Value,
                Resolution = mt.Groups["Resolution"].Value,
                Format = mt.Groups["Format"].Value,
            }))
        {
            return new EpisodeInfo(false, item.Movie, $"{item.Year} {item.Resolution} {item.Format}", string.Empty, FilePath);
        }

        return new EpisodeInfo(false, FilePath, string.Empty, string.Empty, FilePath);
    }

    public string GetDirName(string DirPath)
    {
        if (!Directory.Exists(DirPath))
            return string.Empty;

        string DirName = new DirectoryInfo(DirPath).Name.ToLower();

        // Uniform name
        DirName = DirName.Replace(".", " ");
        DirName = DirName.Replace("'", string.Empty);
        DirName = DirName.Replace("-", string.Empty);
        DirName = textInfo.ToTitleCase(DirName);

        if (DirName.StartsWith("[") && DirName.Count(x => x == ']') == 1)
        {
            DirName = DirName.Split("]".ToCharArray())[1];
        }
        else if(DirName.StartsWith("[") && DirName.Count(x => x == ']') > 1)
        {
            string toRemove = DirName.Split("]".ToCharArray())[0] + "]";
                DirName = DirName.Substring(toRemove.Length);
        }
            
        return DirName.Trim();
    }

    private bool HasDirPermission(string folderPath)
    {
        DirectoryInfo dirInfo = new(folderPath);
        try
        {
            DirectorySecurity dirAC = dirInfo.GetAccessControl(AccessControlSections.Access);
            return true;
        }
        catch (PrivilegeNotHeldException)
        {
            return false;
        }
    }

    private IEnumerable<string> GetFilesByExtensions(DirectoryInfo dir, params string[] extensions)
    {
        if (extensions == null)
            throw new ArgumentNullException("extensions missing");

        IEnumerable<FileInfo> files = dir.EnumerateFiles("*.*", SearchOption.AllDirectories);
        return files.Where(f => extensions.Contains(f.Extension)).Select(x => x.FullName);
    }

    public EpisodeInfo NextEpisode()
    {
        return nextEpisode ?? EmptyEpisode;
    }

    public EpisodeInfo PreviousEpisode()
    {
        return previousEpisode ?? EmptyEpisode;
    }

    public bool HasNextEpisode()
    {
        return nextEpisode.IsTvShow;
    }

    public bool HasPreviousEpisode()
    {
        return previousEpisode.IsTvShow;
    }

    public EpisodeInfo GetCurrentEpisode()
    {
        return currentEpisode;
    }
    #endregion
}
