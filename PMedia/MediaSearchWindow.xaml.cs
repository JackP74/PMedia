using System;
using System.IO;
using System.Windows;
using System.Threading;
using System.Collections.Generic;
using MessageCustomHandler;
using System.Text;
using System.Windows.Data;
using System.Linq;

namespace PMedia
{
    /// <summary>
    /// Interaction logic for MediaSearchWindow.xaml
    /// </summary>
    public partial class MediaSearchWindow : Window
    {
        private readonly string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private readonly TvShow tvShowHelper = new TvShow();
        private readonly List<MediaQuery> mediaQueries = new List<MediaQuery>();

        public MediaSearchWindow()
        {
            InitializeComponent();
        }

        internal class ListItem
        {
            public string Name { get; set; }

            public string Info { get; set; }

            public string Position { get; set; }
        }

        internal class MediaQuery
        {
            public string Name;
            public string Info;
            public string Position;

            public MediaQuery(string Name, string Info, string Position)
            {
                this.Name = Name;
                this.Info = Info;
                this.Position = Position;
            }
        }

        private void StartThread(ThreadStart newStart)
        {
            Thread newThread = new Thread(newStart);
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.IsBackground = true;
            newThread.Start();
        }

        public string GetPosition(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return "0";

                long position = File.ReadAllText(filePath, Encoding.ASCII).ToInt32();
                var positionSpan = TimeSpan.FromSeconds(position);

                return string.Format("{0:D2}h:{1:D2}m:{2:D2}s", positionSpan.Hours, positionSpan.Minutes, positionSpan.Seconds);
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't get video position, Error: " + ex.Message, MessageCustomHandler.Style.Error, Buttons.OK, ex.ToString());
                return "-1";
            }
        }

        private void RefreshItems()
        {
            InfoList.Items.Clear();

            foreach (var item in mediaQueries)
                InfoList.Items.Add(new ListItem { Name = item.Name, Info = item.Info, Position = item.Position });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ColumnName.DisplayMemberBinding = new Binding("Name");
            ColumnInfo.DisplayMemberBinding = new Binding("Info");
            ColumnPosition.DisplayMemberBinding = new Binding("Position");

            if (Directory.Exists(dataPath))
            {
                StartThread(() =>
                {
                    var files = Directory.GetFiles(dataPath, "*.ini", SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        string position = GetPosition(file);

                        int index = file.LastIndexOf(@"-");
                        string fileName = file.Substring(0, index);

                        var fileInfo = tvShowHelper.ParseFile(fileName, true);

                        mediaQueries.Add(new MediaQuery(fileInfo.Name, fileInfo.Episode, position));
                    }

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        RefreshItems();
                    }));

                });
                
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void TxtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string toSearch = TxtSearch.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(toSearch))
                RefreshItems();
            else
            {
                var searchQueries = mediaQueries.Where((x) => x.Name.ToLower().StartsWith(toSearch) || x.Name.ToLower().Contains(toSearch));

                InfoList.Items.Clear();

                foreach (var item in searchQueries)
                    InfoList.Items.Add(new ListItem { Name = item.Name, Info = item.Info, Position = item.Position });
            }
        }
    }
}
