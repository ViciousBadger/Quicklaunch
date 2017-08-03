using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace Quicklaunch
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Entry : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [JsonProperty]
        public Guid ID { get; set; } = Guid.NewGuid();

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(Entry), new PropertyMetadata(string.Empty));
        [JsonProperty]
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty PathProperty =
            DependencyProperty.Register("Path", typeof(string), typeof(Entry), new PropertyMetadata(string.Empty));
        [JsonProperty]
        public string Path
        {
            get { return (string)GetValue(PathProperty); }
            set { SetValue(PathProperty, value); }
        }

        public static readonly DependencyProperty ArgsProperty =
            DependencyProperty.Register("Args", typeof(string), typeof(Entry), new PropertyMetadata(string.Empty));
        [JsonProperty]
        public string Args
        {
            get { return (string)GetValue(ArgsProperty); }
            set { SetValue(ArgsProperty, value); }
        }

        public static readonly DependencyProperty MinutesUsedProperty =
            DependencyProperty.Register("MinutesUsed", typeof(int), typeof(Entry), new PropertyMetadata(0));
        [JsonProperty]
        public int MinutesUsed
        {
            get { return (int)GetValue(MinutesUsedProperty); }
            set { SetValue(MinutesUsedProperty, value); }
        }

        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register("IconSource", typeof(BitmapImage), typeof(Entry));
        public BitmapImage IconSource
        {
            get { return (BitmapImage)GetValue(IconSourceProperty); }
            set { SetValue(IconSourceProperty, value); }
        }

        [JsonProperty]
        public List<string> Tags { get; set; } = new List<string>();

        public string TagList
        {
            get
            {
                List<string> orderedTags = Tags.OrderBy(a => a).ToList();

                StringBuilder result = new StringBuilder();
                for (int i = 0; i < orderedTags.Count; i++)
                {
                    result.Append(orderedTags[i]);
                    //Add comma for every entry but the last one
                    if (i != orderedTags.Count - 1)
                        result.Append(", ");
                }
                return result.ToString();
            }
        }

        public string UseTime
        {
            get
            {
                return string.Format("{0}h {1}m", MinutesUsed / 60, MinutesUsed % 60);
            }
        }

        public string PathToImage
        {
            get
            {
                const string iconFolder = "Icons/";
                Directory.CreateDirectory(iconFolder);
                return iconFolder + ID + ".png";
            }
        }

        public string AbsolutePathToImage
        {
            get
            {
                return System.IO.Path.GetFullPath(PathToImage);
            }
        }

        public Entry()
        {
            UpdateBitmap();
        }

        public void UpdateBitmap()
        {
            IconSource = new BitmapImage();
            if (File.Exists(AbsolutePathToImage))
            {
                IconSource.BeginInit();
                IconSource.CacheOption = BitmapCacheOption.None;
                IconSource.UriCachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);
                IconSource.CacheOption = BitmapCacheOption.OnLoad;
                IconSource.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                IconSource.UriSource = new Uri(AbsolutePathToImage);
                IconSource.EndInit();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IconSource"));
        }
    }
}