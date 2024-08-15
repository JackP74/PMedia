using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using MessageCustomHandler;

namespace PMedia
{
    [Serializable]
    public class RecentList
    {
        public List<string> fileList;
    }

    class Recents
    {
        private readonly string path;
        private readonly int recentCount = 10;

        private RecentList recents;
        private readonly XmlSerializer XmlFormatter = new XmlSerializer(typeof(RecentList));
        
        public Recents(string Path)
        {
            this.path = Path;
            recents = new RecentList { fileList = new List<string>() };
        }
        
        public void Save()
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                using Stream fStream = new FileStream(this.path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                XmlFormatter.Serialize(fStream, recents);
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't save recents, Error: " + ex.Message, Style.Error, Buttons.OK, ex.ToString());
                return;
            }
        }

        public void Load()
        {
            if (!File.Exists(this.path))
                return;

            try
            {
                using Stream fStream = new FileStream(this.path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                recents = (RecentList)XmlFormatter.Deserialize(fStream);
            }
            catch (Exception ex)
            {
                try
                {
                    File.Delete(this.path);

                    recents = new RecentList { fileList = new List<string>() };

                    using Stream fStream = new FileStream(this.path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    XmlFormatter.Serialize(fStream, recents);

                    CMBox.Show("Error", "Couldn't load recents and file was reset, Error: " + ex.Message, Style.Error, Buttons.OK, ex.ToString());
                }
                catch(Exception e)
                {
                    CMBox.Show("Error", "Couldn't load or reset recents, Error: " + e.Message, Style.Error, Buttons.OK, ex.ToString());
                }
            }
        }

        public void AddRecent(string filePath)
        {
            if (recents.fileList.Contains(filePath))
                return;

            if (recents.fileList.Count >= recentCount)
                recents.fileList.RemoveAt(0);

            recents.fileList.Add(filePath);

            Save();
        }

        public void ClearRecent()
        {
            recents.fileList.Clear();
        }

        public List<string> GetList()
        {
            return recents.fileList;
        }
    }
}
