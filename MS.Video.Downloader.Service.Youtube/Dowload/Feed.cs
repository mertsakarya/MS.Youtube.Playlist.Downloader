using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MS.Video.Downloader.Service.Youtube.Annotations;

namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public class Feed : INotifyPropertyChanged
    {
        private string _title;
        private string _description;
        private DownloadState _downloadState;
        private double _percentage;
        private ObservableCollection<Feed> _entries;

        public Feed()
        {
            Entries = new ObservableCollection<Feed>();
            Guid = Guid.NewGuid();
            DownloadState = DownloadState.Initialized;
            Percentage = 0.0;
            Title = "";
            Description = "";
            ThumbnailUrl = "";
        }

        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                if (value == _description) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public Guid Guid { get; set; }
        public string ThumbnailUrl { get; set; }
        public ObservableCollection<Feed> Entries
        {
            get { return _entries; }
            set
            {
                if (Equals(value, _entries)) return;
                _entries = value;
                OnPropertyChanged();
            }
        }

        public DownloadState DownloadState
        {
            get { return _downloadState; }
            set
            {
                if (value == _downloadState) return;
                _downloadState = value;
                OnPropertyChanged();
            }
        }

        public double Percentage
        {
            get { return _percentage; }
            set
            {
                if (value.Equals(_percentage)) return;
                _percentage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
