CREATE TABLE IF NOT EXISTS urls
(   
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  url TEXT, 
  title TEXT,
  length INTEGER
);
CREATE TABLE IF NOT EXISTS videos
(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  urlId INTEGER,
  fileName TEXT,
  finished INTEGER
);
CREATE TABLE IF NOT EXISTS keyValues
(
  key TEXT,
  value TEXT
);
CREATE UNIQUE INDEX IF NOT EXISTS keyValuesIndex ON keyValues ( key );
CREATE UNIQUE INDEX IF NOT EXISTS urlsIndex ON urls ( url );
CREATE UNIQUE INDEX IF NOT EXISTS videosFileNameIndex ON videos ( fileName );
CREATE INDEX IF NOT EXISTS videosUrlIdIndex ON videos ( urlid );
CREATE INDEX IF NOT EXISTS videosUrlIdFileNameIndex ON videos ( urlid, fileName );
--REINDEX;
