using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
using System.Windows.Threading;
using ms.video.downloader.service;
using ms.video.downloader.service.Annotations;
using ms.video.downloader.service.S3;

namespace ms.video.downloader
{
    /// <summary>
    /// Interaction logic for DownloadListView.xaml
    /// </summary>
    /// 
    /// Region	Endpoint	Location Constraint	Protocol
    //US Standard *	s3.amazonaws.com	(none required)	HTTP and HTTPS
    //US West (Oregon) Region	s3-us-west-2.amazonaws.com	us-west-2	HTTP and HTTPS
    //US West (Northern California) Region	s3-us-west-1.amazonaws.com	us-west-1	HTTP and HTTPS
    //EU (Ireland) Region	s3-eu-west-1.amazonaws.com	EU	HTTP and HTTPS
    //Asia Pacific (Singapore) Region	s3-ap-southeast-1.amazonaws.com	ap-southeast-1	HTTP and HTTPS
    //Asia Pacific (Sydney) Region	s3-ap-southeast-2.amazonaws.com	ap-southeast-2	HTTP and HTTPS
    //Asia Pacific (Tokyo) Region	s3-ap-northeast-1.amazonaws.com	ap-northeast-1	HTTP and HTTPS
    //South America (Sao Paulo) Region	s3-sa-east-1.amazonaws.com	sa-east-1	HTTP and HTTPS

    public partial class DownloadListView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(DownloadListView), new PropertyMetadata(null, OnItemsSourcePropertyChanged));
        private ApplicationConfiguration _appConfig;

        public DownloadListView()
        {
            this.InitializeComponent();
            _appConfig = Settings.Instance.ApplicationConfiguration;
            active.IsChecked = _appConfig.S3CanBeActive && _appConfig.S3IsActive;
            accessKey.Text = _appConfig.S3AccessKey;
            secretKey.Text = _appConfig.S3SecretAccessKey;
            region.ItemsSource = S3FileSystem.S3EndPoints;
            if (!String.IsNullOrWhiteSpace(_appConfig.S3RegionHost)) {
                foreach (var item in S3FileSystem.S3EndPoints) {
                    if (item.EndPoint == _appConfig.S3RegionHost) {
                        region.SelectedItem = item;
                    }
                }
            }
            if (!String.IsNullOrWhiteSpace(_appConfig.S3BucketName)) {
                FillBuckets(_appConfig.S3BucketName);
            }
            region.IsEnabled = _appConfig.S3CanSelectRegion;
            bucketName.IsEnabled = _appConfig.S3CanSelectBucket;
            if (Settings.Instance.ApplicationConfiguration.S3IsActive) {
                Settings.Instance.FileSystem.Sync(S3StatusChanged);
            }
        }

        private void S3StatusChanged(S3Status s3Status)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                cloudTotal.Content = String.Format("{0} files on cloud (Bucket: {1})", s3Status.CloudTotal, s3Status.BucketName);
                localTotal.Content = String.Format("{0} files on local", s3Status.LocalTotal);
                uploading.Content = String.Format("{0}/{1} uploading. {2} errors.", s3Status.Uploading, s3Status.UploadingTotal, s3Status.UploadingException);
                downloading.Content = String.Format("{0}/{1} downloading. {2} errors.", s3Status.Downloading, s3Status.DownloadingTotal, s3Status.DownloadingException);
                matching.Content = String.Format("{0} matching files", s3Status.MatchingFiles);
                notProcessed.Content = String.Format("{0} files not processed", s3Status.NotProcessedFiles);
            }));
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            _appConfig.S3AccessKey = accessKey.Text ?? "";
            _appConfig.S3BucketName = bucketName.Text ?? "";
            _appConfig.S3RegionHost = region.SelectedItem != null ? (region.SelectedItem as S3EndPoint).EndPoint : "";
            _appConfig.S3SecretAccessKey = secretKey.Text ?? "";
            if (_appConfig.S3IsActive) Settings.Instance.FileSystem.StopSync();
            _appConfig.S3IsActive = active.IsChecked ?? false;
            region.IsEnabled = _appConfig.S3CanSelectRegion;
            bucketName.IsEnabled = _appConfig.S3CanSelectBucket;
            Settings.Instance.UpdateConfiguration();
            if(_appConfig.S3IsActive)
                Settings.Instance.FileSystem.Sync(S3StatusChanged);
        }

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set
            {
                SetValue(ItemsSourceProperty, value);
                DownloadListGrid.ItemsSource = value;
            }
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as DownloadListView;
            if (control != null) control.OnItemsSourceChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
        }

        private void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            var oldValueINotifyCollectionChanged = oldValue as INotifyCollectionChanged;
            if (null != oldValueINotifyCollectionChanged) oldValueINotifyCollectionChanged.CollectionChanged -= newValueINotifyCollectionChanged_CollectionChanged;

            var newValueINotifyCollectionChanged = newValue as INotifyCollectionChanged;
            if (null != newValueINotifyCollectionChanged) newValueINotifyCollectionChanged.CollectionChanged += newValueINotifyCollectionChanged_CollectionChanged;
        }

        void newValueINotifyCollectionChanged_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

        }

        private void DownloadListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Grid_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            var control = sender as FrameworkElement;
            if (control == null) return;
            var listObject = control.FindName("SubDownloadListGrid");
            var listControl = listObject as Control;
            if (listControl == null) return;
            listControl.Visibility = (listControl.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void region_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FillBuckets(bucketName.Text);
        }

        private void FillBuckets(string bucket)
        {
            try {
                var list = Settings.Instance.FileSystem.GetBuckets();
                if (list.Count > 0) {
                    bucketName.ItemsSource = list;
                    bucketName.IsEnabled = _appConfig.S3CanSelectBucket;
                    if (bucket != null)
                        bucketName.SelectedItem = bucket;
                } else bucketName.IsEnabled = false;
            } catch (Exception) {}
        }
    }
}
