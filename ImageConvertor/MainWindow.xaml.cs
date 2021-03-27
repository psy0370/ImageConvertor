﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace ImageConvertor
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly ObservableCollection<SourceImage> sourceImages = new ObservableCollection<SourceImage>();
        private readonly CodecCollection codecs = new CodecCollection();

        public MainWindow()
        {
            InitializeComponent();
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
                foreach (var source in FileManager.GetImageFiles(entries))
                {
                    sourceImages.Add(source);
                }
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
                // UI上でエラーを表現する処理を後ほど追加する
                return;
            }

            var encoder = (BitmapEncoder)CodecList.SelectedItem;

            foreach (var sourceImage in sourceImages)
            {
                // 既にファイルが存在する場合の処理をどうするか後ほど決定
                sourceImage.Save(encoder, directory, 
                    SamePath.IsChecked == true && RemoveSource.IsChecked == true, 
                    IsTrimming.IsChecked == true, 
                    LeftTop.IsChecked == true ? TrimType.LeftTop : TrimType.RightBottom,
                    false, false);// Is200Line.IsChecked, Is8Color.IsChecked);
            }
        }
    }
}
