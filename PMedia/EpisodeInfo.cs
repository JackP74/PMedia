using System;

namespace PMedia
{
    [Serializable]
    public class EpisodeInfo
    {
        public bool IsTvShow;
        public string Name;
        public string Episode;
        public string SearchDir;
        public string FilePath;
        public bool Search;

        public EpisodeInfo()
        {
            this.IsTvShow = false;
            this.Name = string.Empty;
            this.Episode = string.Empty;
            this.SearchDir = string.Empty;
            this.FilePath = string.Empty;
            this.Search = true;
        }

        public EpisodeInfo(bool IsTvShow, string Name, string Episode, string SearchDir, string FilePath, bool Search = true)
        {
            this.IsTvShow = IsTvShow;
            this.Name = Name;
            this.Episode = Episode;
            this.SearchDir = SearchDir;
            this.FilePath = FilePath;
            this.Search = Search;
        }

        public override string ToString()
        {
            return $"Is Tv Show: {IsTvShow}; Name: {Name}; Episode: {Episode}; Search Dir: {SearchDir}; File Path {FilePath};";
        }

        public override bool Equals(object obj)
        {
            if (obj is EpisodeInfo cObj)
                return (IsTvShow == cObj.IsTvShow && Name == cObj.Name && Episode == cObj.Episode && SearchDir == cObj.SearchDir && FilePath == cObj.FilePath);

            return false;
        }

        public override int GetHashCode()
        {
            return $"Is Tv Show: {IsTvShow}; Name: {Name}; Episode: {Episode}; Search Dir: {SearchDir}; File Path {FilePath};".GetHashCode();
        }
    }
}