using MahApps.Metro.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace ImageConvertor
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly ObservableCollection<SourceImage> sourceImages = new ObservableCollection<SourceImage>();
        private readonly CodecCollection codecs = new CodecCollection();
        private readonly Storyboard storyboard;
        public MainWindow()
        {
            InitializeComponent();
            storyboard = FindResource("TextBoxAlert") as Storyboard;
        }

        /// <summary>
        /// ロードが完了したときの処理を定義します。
        /// </summary>
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SourceListView.ItemsSource = sourceImages;
            CodecList.ItemsSource = codecs;
            CodecList.SelectedIndex = 0;
        }

        /// <summary>
        /// ファイルをドラッグしたときの処理を定義します。
        /// </summary>
        private void MetroWindow_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        /// <summary>
        /// ファイルをドロップしたときの処理を定義します。
        /// </summary>
        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var entries = (string[])e.Data.GetData(DataFormats.FileDrop);

                Task.Factory.StartNew(() =>
                {
                    foreach (var source in FileManager.GetImageFiles(entries))
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            sourceImages.Add(source);
                        }));
                    }
                });
            }
        }

        /// <summary>
        /// 出力フォルダをクリックしたときの処理を定義します。
        /// </summary>
        private void OutputDirectory_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "出力フォルダを選択してください",
                Multiselect = false,
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                OutputDirectory.Text = dialog.FileName;
            }
        }

        /// <summary>
        /// クリアボタンをクリックしたときの処理を定義します。
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            sourceImages.Clear();
        }

        /// <summary>
        /// 変換開始ボタンをクリックしたときの処理を定義します。
        /// </summary>
        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            var directory = SamePath.IsChecked == true ? null : OutputDirectory.Text;
            if (directory != null && !Directory.Exists(directory))
            {
                BeginStoryboard(storyboard);
                return;
            }

            var codec = (CodecInfo)CodecList.SelectedItem;
            var removeSource = SamePath.IsChecked == true && RemoveSource.IsChecked == true;
            var trimming = IsTrimming.IsChecked == true;
            var trimmingType = LeftTop.IsChecked == true ? TrimType.LeftTop : TrimType.RightBottom;
            var line200 = Is200Line.IsChecked == true;
            var color8 = false;// Is8Color.IsChecked == true;

            Task.Factory.StartNew(() =>
            {
                foreach (var sourceImage in sourceImages)
                {
                    // 既にファイルが存在する場合の処理をどうするか後ほど決定
                    var result = sourceImage.Save(codec, directory, removeSource, trimming, trimmingType, line200, color8);
                    var log = $"{sourceImage.Filename} => ";

                    switch (result)
                    {
                        case SaveResult.Processed:
                            log += $"{codec.Name}";
                            break;
                        case SaveResult.Skipped:
                            log += "skipped.";
                            break;
                        default:
                            log += "undefined.";
                            break;
                    }

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LogView.Items.Add(log);
                        LogView.ScrollIntoView(LogView.Items[LogView.Items.Count - 1]);
                    }));
                }
            });
        }
    }
}
