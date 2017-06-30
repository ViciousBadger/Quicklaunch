using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IWshRuntimeLibrary;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Quicklaunch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Library lib = new Library();

        public ObservableCollection<Tag> UsedTags { get; set; } = new ObservableCollection<Tag>();

        public static readonly DependencyProperty CurrentlyEditingProperty =
            DependencyProperty.Register("CurrentlyEditing", typeof(Entry), typeof(MainWindow));
        public Entry CurrentlyEditing
        {
            get { return (Entry)GetValue(CurrentlyEditingProperty); }
            set { SetValue(CurrentlyEditingProperty, value); }
        }

        public MainWindow()
        {
            InitializeComponent();
            Load();
        }

        private const string savefile = "Library.json";

        private void Load()
        {
            if (System.IO.File.Exists(savefile))
            {
                using (StreamReader sr = new StreamReader(savefile))
                {
                    lib = JsonConvert.DeserializeObject<Library>(sr.ReadToEnd());
                    UpdateList();
                }
                //Load icons and add tags for each loaded game
                foreach (Entry e in lib.Entries)
                {
                    e.UpdateBitmap();
                    foreach (string tag in e.Tags)
                    {
                        if (!UsedTags.Any(a => a.Name == tag))
                        {
                            UsedTags.Add(new Tag { Name = tag });
                        }
                    }
                }
                Width = lib.WindowWidth;
                Height = lib.WindowHeight;
                Left = lib.WindowLeft;
                Top = lib.WindowTop;
            }
        }

        private void Save()
        {
            lib.WindowWidth = Width;
            lib.WindowHeight = Height;
            lib.WindowLeft = Left;
            lib.WindowTop = Top;
            using (StreamWriter sw = new StreamWriter(savefile))
            {
                sw.Write(JsonConvert.SerializeObject(lib));
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Save();
        }

        private void QuickAdd(string targetFile, string args, string name = null)
        {
            if (!System.IO.File.Exists(targetFile))
            {
                MessageBox.Show("File " + targetFile + " not found - game not added!");
                return;
            }
            if (System.IO.Path.GetExtension(targetFile) != ".exe")
            {
                MessageBoxResult res = MessageBox.Show(System.IO.Path.GetFileName(targetFile) + " is not an executable. Do you still want to add it?", "Notice", MessageBoxButton.YesNo);
                if (res != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            Entry entry = new Entry
            {
                Title = name ?? System.IO.Path.GetFileNameWithoutExtension(targetFile),
                Path = targetFile,
                Args = args
            };

            //Extract .exe icon and save it as the game entry image
            ExtractExeIcon(targetFile, entry.AbsolutePathToImage);
            entry.UpdateBitmap();

            lib.Entries.Add(entry);

            UpdateList();
        }

        private void UpdateList()
        {
            entryListView.Items.Clear();
            foreach (Entry e in lib.Entries.OrderBy(a => a.Title))
            {
                if (string.IsNullOrEmpty(searchBox.Text)
                    //Seach in titles
                    || e.Title.ToLower().Contains(searchBox.Text.ToLower())
                    //Search in tags
                    || e.Tags.Any(a => a.ToLower().Contains(searchBox.Text.ToLower())))
                {
                    entryListView.Items.Add(e);
                }
            }
        }

        private void Edit(Entry game)
        {
            tabControl.SelectedIndex = 1;
            CurrentlyEditing = game;
            //Set tag list to have this game's tags checked
            foreach (Tag item in UsedTags)
            {
                item.Checked = game.Tags.Contains(item.Name);
            }
        }

        private bool ExtractExeIcon(string targetFile, string savePath)
        {
            try
            {
                Bitmap icon = System.Drawing.Icon.ExtractAssociatedIcon(targetFile).ToBitmap();
                icon.Save(savePath);
                return true;
            }
            catch (Exception)
            {
                MessageBox.Show("Could not fetch executable icon from " + targetFile + ". You're gonna have to set the icon manually.");
                return false;
            }
        }

        /* Game list events */

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    if (System.IO.Path.GetExtension(file) == ".lnk")
                    {
                        //Extract target of shortcut
                        WshShell shell = new WshShell();
                        IWshShortcut link = (IWshShortcut)shell.CreateShortcut(file);

                        QuickAdd(link.TargetPath, link.Arguments, System.IO.Path.GetFileNameWithoutExtension(file));
                    }
                    else
                    {
                        QuickAdd(file, string.Empty);
                    }
                }
            }
            addGameOverlay.Visibility = Visibility.Hidden;
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            addGameOverlay.Visibility = Visibility.Visible;
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            addGameOverlay.Visibility = Visibility.Hidden;
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //filteredGameEntries = new ObservableCollection<GameEntry>(gameEntries.Where(a => a.Title.Contains(textBox.Text)));
            UpdateList();
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            Entry entry = (sender as ListViewItem)?.Content as Entry;
            if (entry != null)
            {
                if (System.IO.File.Exists(entry.Path))
                {
                    try
                    {
                        Process.Start(entry.Path, entry.Args);
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    MessageBox.Show("Game executable at " + entry.Path + " not found!");
                }
            }
        }

        private void EntryList_EditItem(object sender, RoutedEventArgs e)
        {
            Entry entry = (sender as MenuItem)?.DataContext as Entry;
            if (entry != null)
            {
                Edit(entry);
            }
        }

        private void EntryList_RemoveItem(object sender, RoutedEventArgs e)

        {
            Entry entry = (sender as MenuItem)?.DataContext as Entry;
            if (entry != null)
            {
                lib.Entries.Remove(entry);
                if (System.IO.File.Exists(entry.AbsolutePathToImage))
                {
                    System.IO.File.Delete(entry.AbsolutePathToImage);
                }
                UpdateList();
            }
        }

        private void addGameButton_Click(object sender, RoutedEventArgs e)
        {
            Entry newgame = new Entry();
            lib.Entries.Add(newgame);
            Edit(newgame);
        }

        /* Entry editing events */

        private void browseExeBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            bool? result = dialog.ShowDialog();
            switch (result)
            {
                case null:
                    break;

                case false:
                    break;

                case true:
                    CurrentlyEditing.Path = dialog.FileName;
                    if (string.IsNullOrEmpty(CurrentlyEditing.Title))
                    {
                        CurrentlyEditing.Title = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                    }
                    break;
            }
        }

        private void browseIconBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png)|*.jpg;*.jpeg;*.jpe;*.jfif;*.png;*.bmp;*.dib;*.rle;";
            bool? result = dialog.ShowDialog();
            switch (result)
            {
                case null:
                    break;

                case false:
                    break;

                case true:
                    try
                    {
                        System.IO.File.Copy(dialog.FileName, CurrentlyEditing.AbsolutePathToImage, true);
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show("Failed to copy image - " + ex.Message);
                    }

                    break;
            }
            CurrentlyEditing.UpdateBitmap();
        }

        private void resetIconBtn_Click(object sender, RoutedEventArgs e)
        {
            ExtractExeIcon(CurrentlyEditing.Path, CurrentlyEditing.AbsolutePathToImage);
            CurrentlyEditing.UpdateBitmap();
        }

        private void doneBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();
            tabControl.SelectedIndex = 0;
            CurrentlyEditing = null;
        }

        private void tagCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            CheckBox senderCb = sender as CheckBox;
            if (senderCb != null)
            {
                string content = senderCb.Content as string;
                if (content != null)
                {
                    switch (senderCb.IsChecked)
                    {
                        case null:
                        case false:
                            CurrentlyEditing.Tags.Remove(content);
                            break;

                        case true:
                            if (!CurrentlyEditing.Tags.Contains(content))
                                CurrentlyEditing.Tags.Add(content);
                            break;
                    }
                }
            }
        }

        private void newTagButton_Click(object sender, RoutedEventArgs e)
        {
            string name = newTagField.Text;
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (!UsedTags.Any(a => a.Name == name))
                {
                    Tag newTag = new Tag { Name = name };
                    UsedTags.Add(newTag);
                    newTagField.Text = string.Empty;
                }
                else
                {
                    MessageBox.Show("Tag already exists!");
                }
            }
        }

        private void RemoveTagMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Tag tag = (sender as MenuItem)?.DataContext as Tag;
            if (tag != null)
            {
                //Look for entries with this tag and display a warning if any games have it
                int entriesWithThisTag = 0;
                foreach (Entry entry in lib.Entries)
                {
                    if (entry.Tags.Contains(tag.Name))
                    {
                        entriesWithThisTag++;
                    }
                }

                if (entriesWithThisTag > 0)
                {
                    MessageBoxResult response = MessageBox.Show(entriesWithThisTag + " entries are currently using this tag. Are you sure you want to remove it?", "Hang on", MessageBoxButton.YesNo);
                    switch (response)
                    {
                        case MessageBoxResult.No:
                            //Return without removing tag
                            return;
                    }
                }

                //Tag is removed here
                foreach (Entry entry in lib.Entries)
                {
                    entry.Tags.Remove(tag.Name);
                }
                UsedTags.Remove(tag);
            }
        }
    }
}