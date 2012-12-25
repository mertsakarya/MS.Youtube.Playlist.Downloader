using System;
using System.Collections.ObjectModel;

namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public interface IFeed
    {
        string Title { get;  }
        Guid Guid { get;  }
        string ThumbnailUrl { get;  }
        ObservableCollection<IFeed> Entries { get; }
        DownloadState DownloadState { get;  }
        double Percentage{ get;  }
    }

    public abstract class Feed : IFeed {
        protected Feed()
        {
            Entries = new ObservableCollection<IFeed>();
            Guid = Guid.NewGuid();
            DownloadState = DownloadState.Initialized;
            Percentage = 0.0;
            Title = "";
            Description = "";
            ThumbnailUrl = "";
        }

        public string Title { get; protected set; }
        public string Description { get; protected set; }
        public Guid Guid { get; protected set; }
        public string ThumbnailUrl { get; protected set; }
        public ObservableCollection<IFeed> Entries { get; protected set; }
        public DownloadState DownloadState { get; protected set; }
        public double Percentage { get; protected set; }
    }
}
