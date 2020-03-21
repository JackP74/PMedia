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
            public int Autoplay;
            public int Rate;
            public bool AutoAudio;
            public bool AutoSubtitle;
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

        public int Autoplay
        {
            set
            {
                mainSettings.Autoplay = value;
                NeedsSaving = true;
            }

            get
            {
                return mainSettings.Autoplay;
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
            }

            get
            {
                return mainSettings.AutoSubtitle;
            }
        }

        public Settings()
        {
            mainSettings = new MainSettings()
            {
                Jump = 10,
                Volume = 100,
                IsMute = false,
                Autoplay = 15,
                Rate = 1,
                AutoAudio = true,
                AutoSubtitle = true
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
                using (Stream fStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    mainSettings = (MainSettings)SoapFormat.Deserialize(fStream);
                }
            }
            catch (Exception e)
            {
                System.IO.File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + @"\log.txt", Environment.NewLine + Environment.NewLine + e.ToString() + Environment.NewLine + Environment.NewLine, System.Text.Encoding.UTF8);

                Jump = 10;
                Volume = 100;
                IsMute = false;
                Autoplay = 15;
                Rate = 1;

                Save();
            }
        }
    }
}
