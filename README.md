MS.Youtube.Playlist.Downloader
==============================

Youtube Downloader and audio converter
--------------------------------------
	
This application is for downloading multiple videos from Youtube. You can download videos and extract MP3 audio files from them. You can download all video on playlists and extract them to MP3 with few clicks.

You can use this application to download individual videos and playlists, extract MP3 from videos, and browse Youtube.

The binaries can be found at http://www.mertsakarya.com/MS.Youtube.Downloader/

Notes about installation
------------------------
Setup size is about 20MBs and most of it goes to FFMPEG converter.
If you don't have .Net 4.5 Framework installed, the setup installs the framework first (which is another 20MBs), and installation of .Net Framework 4.5 might take some time.

Features
--------
* Browse on Youtube for each video,
* Browse on Youtube for a  playlist
* Browse on Youtube for user's playlists and favorites.
* Download a single video [and extract MP3 audio]
* Download a playlist [and extract MP3 audio]
* If you are on a "Playlist" page, you can download all (or selected) videos (and audios)
* If you are on a user's page, All users' playlists are listed and can be downloaded individually.
* Playlists are downloaded as seperate folder
* You can select a folder for all downloads (By default a "YouTubeDownloads" folder will be created on your Desktop)
   A "Video" folder is generated for downloading videos.
   If you download from a play list a folder will be created for that Playlist
   If the video downloaded does not belong to any Playlist, it will be downloaded under "Video" folder.
   All videos are downloaded as 360 bit resolution MP4 files
   An "Audio" folder created for all MP3 files.
   MP3 files will be created after each video file (.MP4) is downloaded
* Ability to understand dragging and dropping links from other browsers, if it is from youtube.com domain.

Technology
----------

* Written with Visual Studio 2012 in C#
* .Net Framework 4.5 is required.
* Tested on Windows 8 Professional 64bit with 8GB RAM and tons of harddisk space with very large bandwidth.
* I didn't have time to test on other systems yet.
* This application won't run on Apples, Oranges, Potatoes or on Linux boxes.
* Built for pure high-end Windows boxes.
* Here is the download link again, in case you missed it above.

Contact me if necessary,
mertsakarya@hotmail.com

Planned improvements:

* Ability to understand other feeds (playlists, channels, histories)
* Youtube user authentication.
* Better management, search and play on downloaded content.
* Support for multiple bitrates and qualities and file formats.
* Usability improvements and tests.
* Performance tests.
* Support for older Windows.
* Silverlight support.
* Porting to Metro Application.
* Windows Mobile support.
* Amazon S3 support.

