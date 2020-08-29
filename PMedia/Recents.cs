using MessageCustomHandler;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PMedia
{
    class Recents
    {
        private readonly string path;
        private readonly int recentCount = 10;

        private RecentList recents;
        private readonly BinaryFormatter BinaryFormat;

        [Serializable] internal struct RecentList
        {
            public List<string> fileList;
        }

        public Recents(string Path)
        {
            this.path = Path;

            this.recents = new RecentList();
            recents.fileList = new List<string>();

            BinaryFormat = new BinaryFormatter();
        }
        
        public void Save()
        {
            try
            {
                using Stream fStream = new FileStream(this.path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                BinaryFormat.Serialize(fStream, recents);
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't save recents, Error: " + ex.Message, Style.Error, Buttons.OK, ex.ToString());
                return;
            }
        }

        public void Load()
        {
            if (File.Exists(this.path) == false)
                return;

            try
            {
                using Stream fStream = new FileStream(this.path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                recents = (RecentList)BinaryFormat.Deserialize(fStream);
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't load recents, Error: " + ex.Message, Style.Error, Buttons.OK, ex.ToString());
                return;
            }
        }

        public void AddRecent(string filePath)
        {
            if (recents.fileList.Contains(filePath))
                return;

            if (recents.fileList.Count >= recentCount)
            {
                recents.fileList.RemoveAt(0);
            }

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
