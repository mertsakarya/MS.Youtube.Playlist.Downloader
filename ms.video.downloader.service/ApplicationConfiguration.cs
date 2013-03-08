using System;
using System.ComponentModel;
using ms.video.downloader.service.Annotations;

namespace ms.video.downloader.service
{
    public class ApplicationConfiguration : INotifyPropertyChanged
    {
        private Guid _guid;
        private string _s3AccessKey;
        private bool _s3IsActive;
        private string _s3BucketName;
        private string _s3RegionHost;
        private string _s3SecretAccessKey;

        public Guid Guid
        {
            get { return _guid; }
            set
            {
                if (value.Equals(_guid)) return;
                _guid = value;
                OnPropertyChanged("Guid");
            }
        }
        public string S3AccessKey
        {
            get { return _s3AccessKey ?? ""; }
            set
            {
                if (value == _s3AccessKey) return;
                _s3AccessKey = value;
                OnPropertyChanged("S3AccessKey");
            }
        }
        public string S3SecretAccessKey
        {
            get { return _s3SecretAccessKey ?? ""; }
            set
            {
                if (value == _s3SecretAccessKey) return;
                _s3SecretAccessKey = value;
                OnPropertyChanged("S3SecretAccessKey");
            }
        }

        public string S3RegionHost
        {
            get { return _s3RegionHost ?? ""; }
            set
            {
                if (value == _s3RegionHost) return;
                _s3RegionHost = value;
                OnPropertyChanged("S3RegionHost");
            }
        }

        public string S3BucketName
        {
            get { return _s3BucketName ?? ""; }
            set
            {
                if (value == _s3BucketName) return;
                _s3BucketName = value;
                OnPropertyChanged("S3BucketName");
            }
        }

        public bool S3CanBeActive { get { return S3CanSelectBucket && !String.IsNullOrWhiteSpace(S3BucketName); } }

        public bool S3CanSelectBucket { 
            get {
                    return S3CanSelectRegion && !String.IsNullOrWhiteSpace(S3RegionHost);
                } 
        }

        public bool S3CanSelectRegion { get { return !(String.IsNullOrWhiteSpace(S3AccessKey) || String.IsNullOrWhiteSpace(S3SecretAccessKey) ); } }

        public bool S3IsActive
        {
            get { return _s3IsActive; }
            set
            {
                if (value.Equals(_s3IsActive)) return;
                if (!S3CanBeActive) return;
                _s3IsActive = value;
                OnPropertyChanged("S3IsActive");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}