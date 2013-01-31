CREATE TABLE IF NOT EXISTS urls
(   
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  url TEXT, 
  length INTEGER
);
CREATE TABLE IF NOT EXISTS videos
(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  urlId INTEGER,
  fileName TEXT,
  finished INTEGER
);
CREATE TABLE IF NOT EXISTS audios
(
  id INTEGER PRIMARY KEY,
  urlId INTEGER,
  fileName TEXT,
  finished INTEGER
);
CREATE UNIQUE INDEX IF NOT EXISTS urlsIndex ON urls ( url );
CREATE INDEX IF NOT EXISTS audiosUrlIdIndex ON audios ( urlid );
CREATE INDEX IF NOT EXISTS videosUrlIdIndex ON videos ( urlid );
CREATE INDEX IF NOT EXISTS audiosFileNameIndex ON audios ( urlid, fileName );
CREATE INDEX IF NOT EXISTS videosFileNameIndex ON videos ( urlid, fileName );
--REINDEX;
