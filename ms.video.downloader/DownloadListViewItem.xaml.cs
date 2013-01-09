using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ms.video.downloader.service;
using ms.video.downloader.service.Dowload;

namespace ms.video.downloader
{
    /// <summary>
    /// Interaction logic for DownloadListViewItem.xaml
    /// </summary>
    public partial class DownloadListViewItem : UserControl
    {
        public DownloadListViewItem()
        {
            InitializeComponent();
            var feed = Item.Tag as Feed;
            if (feed != null) {
                if (feed.ExecutionStatus == ExecutionStatus.Paused)
                    Pause.Content = "Cont.";
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            var feed = Item.Tag as Feed;
            if (feed != null) {
                if ((string) Pause.Content == "Pause") {
                    feed.Pause();
                    Pause.Content = "Cont.";
                }
                else {
                    feed.Continue();
                    Pause.Content = "Pause";
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var feed = Item.Tag as Feed;
            if (feed != null)
                feed.Delete();
        }
    }
}
