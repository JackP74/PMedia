using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PMedia
{
    class Settings
    {

        private MainSettings mainSettings;
        private readonly SoapFormatter SoapFormat;
        private readonly string filePath = AppDomain.CurrentDomain.BaseDirectory + @"\settings.ini";

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

        [Serializable] internal struct MainSettings
        {
            public int Jump;
            public int Volume;
            public bool IsMute;
            public int Autoplay; 
        }

        public Settings()
        {
            mainSettings = new MainSettings()
            {
                Jump = 10,
                Volume = 100,
                IsMute = false,
                Autoplay = 15
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

                Save();
            }
        }
    }
}
