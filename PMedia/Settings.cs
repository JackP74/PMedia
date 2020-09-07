using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Soap;

namespace PMedia
{
    class Settings
    {
        private MainSettings mainSettings;
        private readonly SoapFormatter SoapFormat;
        private readonly string filePath = AppDomain.CurrentDomain.BaseDirectory + @"\settings.ini";

        public bool NeedsSaving = false;

        [Serializable] internal struct MainSettings
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

            SoapFormat = new SoapFormatter();
        }

        public void Save()
        {
            using(Stream fStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                SoapFormat.Serialize(fStream, mainSettings);
            }

            NeedsSaving = false;
        }

        public void Load()
        {
            if (File.Exists(filePath) == false)
                return;

            try
            {
                using Stream fStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                mainSettings = (MainSettings)SoapFormat.Deserialize(fStream);
            }
            catch (Exception e)
            {
                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + @"\log.txt", Environment.NewLine + Environment.NewLine + e.ToString() + Environment.NewLine + Environment.NewLine, System.Text.Encoding.UTF8);

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

                Save();
            }
        }
    }
}