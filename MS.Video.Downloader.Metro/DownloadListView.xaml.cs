using System.Collections;
using System.Collections.Specialized;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MS.Video.Downloader.Metro
{
    public sealed partial class DownloadListView : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(DownloadListView), new PropertyMetadata(null, OnItemsSourcePropertyChanged));
        
        public DownloadListView()
        {
            this.InitializeComponent();
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

        private void StackPanel_Tapped_1(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var control = sender as FrameworkElement;
            if (control == null) return;
            var listObject = control.FindName("SubDownloadListGrid");
            var listControl = listObject as Control;
            if (listControl == null) return;
            listControl.Visibility = (listControl.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DownloadListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
