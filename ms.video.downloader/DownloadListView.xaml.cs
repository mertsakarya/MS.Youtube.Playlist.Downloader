using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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

namespace ms.video.downloader
{
    /// <summary>
    /// Interaction logic for DownloadListView.xaml
    /// </summary>
    public partial class DownloadListView : UserControl
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
    }
}
