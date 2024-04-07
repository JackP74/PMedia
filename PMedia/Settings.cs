using System;
using System.IO;
using System.Xml.Serialization;

using MessageCustomHandler;

namespace PMedia;

[Serializable]
public class MainSettings
{
    public int Jump;
    public int Volume;
    public bool IsMute;
    public bool AutoPlay;
    public int AutoPlayTime;
    public int Rate;
    public bool AutoAudio;
    public bool AutoSubtitle;
    public bool HardwareAcceleration;
    public bool SubtitleDisable;
}

class Settings
{
    private MainSettings mainSettings;
    private readonly XmlSerializer XmlFormatter = new XmlSerializer(typeof(MainSettings));
    private readonly string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");

    public bool NeedsSaving = false;

    public int Jump
    {
        set
        {
            mainSettings.Jump = value;
            NeedsSaving = true;
        }

        get
        {
            return mainSettings.Jump;
        }
    }

    public int Volume
    {
        set
        {
            mainSettings.Volume = value;
            NeedsSaving = true;
        }

        get
        {
            return mainSettings.Volume;
        }
    }

    public bool IsMute
    {
        set
        {
            mainSettings.IsMute = value;
            NeedsSaving = true;
        }

        get
        {
            return mainSettings.IsMute;
        }
    }

    public bool AutoPlay
    {
        set
        {
            mainSettings.AutoPlay = value;
            NeedsSaving = true;
        }

        get
        {
            return mainSettings.AutoPlay;
        }
    }

    public int AutoPlayTime
    {
        set
        {
            mainSettings.AutoPlayTime = value;
            NeedsSaving = true;
        }

        get
        {
            return mainSettings.AutoPlayTime;
        }
    }

    public int Rate
    {
        set
        {
            mainSettings.Rate = value;
            NeedsSaving = true;
        }

        get
        {
            return mainSettings.Rate;
        }
    }

    public bool AutoAudio
    {
        set
        {
            mainSettings.AutoAudio = value;
            NeedsSaving = true;
        }

        get
        {
            return mainSettings.AutoAudio;
        }
    }

    public bool AutoSubtitle
    {
        set
        {
            mainSettings.AutoSubtitle = value;
            NeedsSaving = true;
        }

        get
        {
            return mainSettings.AutoSubtitle;
        }
    }

    public bool Acceleration
    {
        set
        {
            mainSettings.HardwareAcceleration = value;
            NeedsSaving = true;
        }

        get
        {
            return mainSettings.HardwareAcceleration;
        }
    }

    public bool SubtitleDisable
    {
        set
        {
            mainSettings.SubtitleDisable = value;
            NeedsSaving = true;
        }

        get
        {
            return mainSettings.SubtitleDisable;
        }
    }

    public Settings()
    {
        mainSettings = new MainSettings()
        {
            Jump = 10,
            Volume = 100,
            IsMute = false,
            AutoPlay = false,
            AutoPlayTime = 15,
            Rate = 1,
            AutoAudio = true,
            AutoSubtitle = true,
            HardwareAcceleration = true,
            SubtitleDisable = false
        };
    }

    public void Save()
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            using Stream fStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            XmlFormatter.Serialize(fStream, mainSettings);

            NeedsSaving = false;
        }
        catch (Exception ex)
        {
            CMBox.Show("Error", "Couldn't save settings, Error: " + ex.Message, Style.Error, Buttons.OK, ex.ToString());
        }
    }

    public void Load()
    {
        if (!File.Exists(filePath))
            return;

        try
        {
            using Stream fStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            mainSettings = (MainSettings)XmlFormatter.Deserialize(fStream);
        }
        catch (Exception ex)
        {
            try
            {
                File.Delete(filePath);

                Jump = 10;
                Volume = 100;
                IsMute = false;
                AutoPlay = false;
                AutoPlayTime = 15;
                Rate = 1;
                AutoAudio = true;
                AutoSubtitle = true;
                Acceleration = true;
                SubtitleDisable = false;

                using Stream fStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                XmlFormatter.Serialize(fStream, mainSettings);

                NeedsSaving = false;

                CMBox.Show("Error", "Couldn't load settings and file was reset, Error: " + ex.Message, Style.Error, Buttons.OK, ex.ToString());
            }
            catch (Exception e)
            {
                CMBox.Show("Error", "Couldn't settings or reset recents, Error: " + e.Message, Style.Error, Buttons.OK, ex.ToString());
            }
        }
    }
}