using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

namespace ms.video.downloader.service.Dowload
{
    public class Feed : INotifyPropertyChanged
    {
        private string _title;
        private string _description;
        private DownloadState _downloadState;
        private double _percentage;
        private ObservableCollection<Feed> _entries;
        private ExecutionStatus _executionStatus;


        public Feed()
        {
            Entries = new ObservableCollection<Feed>();
            Guid = Guid.NewGuid();
            DownloadState = DownloadState.Initialized;
            Percentage = 0.0;
            Title = "";
            Description = "";
            ThumbnailUrl = "";
            _executionStatus = ExecutionStatus.Normal;
        }

        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged("Title");
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                if (value == _description) return;
                _description = value;
                OnPropertyChanged("Description");
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
                OnPropertyChanged("Entries");
            }
        }

        [JsonIgnore]
        public DownloadState DownloadState
        {
            get { return _downloadState; }
            set
            {
                if (value == _downloadState) return;
                _downloadState = value;
                OnPropertyChanged("DownloadState");
            }
        }

        public ExecutionStatus ExecutionStatus
        {
            get { return _executionStatus; }
            set
            {
                if (value == _executionStatus) return;
                _executionStatus = value;
                OnPropertyChanged("ExecutionSTatus");
            }
        }

        [JsonIgnore]
        public double Percentage
        {
            get { return _percentage; }
            set
            {
                if (value.Equals(_percentage)) return;
                _percentage = value;
                OnPropertyChanged("Percentage");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void Delete()
        {
            ExecutionStatus = ExecutionStatus.Deleted;
        }

        public virtual void Pause()
        {
            ExecutionStatus = ExecutionStatus.Pause;
        }

        public virtual void Continue()
        {
            ExecutionStatus = ExecutionStatus.Normal;
        }

    }
}
