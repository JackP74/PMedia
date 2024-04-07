using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

using MessageCustomHandler;

namespace PMedia;

[Serializable]
public class PlaylistFile
{
    public List<EpisodeInfo> files;

    public PlaylistFile()
    {

    }
}

public class Playlist
{
    public PlaylistFile currentList;
    private readonly XmlSerializer XmlFormatter = new XmlSerializer(typeof(PlaylistFile));

    public void AddList(List<EpisodeInfo> newFiles)
    {
        currentList.files.AddRange(newFiles);
    }

    public void ClearFiles()
    {
        currentList.files.Clear();
    }

    public Playlist()
    {
        currentList = new PlaylistFile() { files = new List<EpisodeInfo>() };
    }

    public bool Save(string filePath)
    {
        try
        {
            using Stream fStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            XmlFormatter.Serialize(fStream, currentList);
            return true;
        }
        catch (Exception ex)
        {
            CMBox.Show("Error", "Couldn't save playlist, Error: " + ex.Message, Style.Error, Buttons.OK, ex.ToString());
            return false;
        }
    }

    public bool Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            CMBox.Show("Error", "Couldn't load playlist, Error: File not found", Style.Error, Buttons.OK);
            return false;
        }
        else
        {
            try
            {
                using Stream fStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                currentList = (PlaylistFile)XmlFormatter.Deserialize(fStream);
                return true;
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't load playlist, Error: " + ex.Message, Style.Error, Buttons.OK, ex.ToString());
                return false;
            }
        }
    }
}
