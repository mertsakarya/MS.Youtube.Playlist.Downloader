using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace MS.Video.Downloader.Service
{
    public class LocalService
    {
        public string CompanyFolder { get; private set; }
        public string AppFolder { get; private set; }
        public string AppVersionFolder { get; private set; }
        public string Version { get; private set; }
        public Guid Guid { get; private set; }
        public bool FirstTime { get; private set; }

        private ApplicationConfiguration Configuration { get; set; }

        public LocalService(Assembly assembly = null)
        {
            
            FirstTime = false;
            var assm = assembly ?? Assembly.GetEntryAssembly();
            //var at = typeof(AssemblyCompanyAttribute);
            //var r = assm.GetCustomAttributes(at, false);
            //var ct = ((AssemblyCompanyAttribute)(r[0]));
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            CompanyFolder = path + @"\MS";
            if (!Directory.Exists(CompanyFolder)) Directory.CreateDirectory(CompanyFolder);
            AppFolder = CompanyFolder + @"\MS.Youtube.Downloader";
            if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
            Version = assm.GetName().Version.ToString();
            AppVersionFolder = AppFolder + @"\" +Version;
            if (!Directory.Exists(AppVersionFolder)) Directory.CreateDirectory(AppVersionFolder);
            Configuration = GetConfiguration();
            Guid = Configuration.Guid;
        }

        private ApplicationConfiguration GetConfiguration()
        {
            var configFile = AppFolder + "\\applicationConfiguration.json";
            if (!File.Exists(configFile))
            {
                FirstTime = true;
                var config = new ApplicationConfiguration {Guid = Guid.NewGuid()};
                using (var file = new StreamWriter(configFile)) {
                    file.Write(JsonConvert.SerializeObject(config));
                }
            }
            using (var file = new StreamReader(configFile)){
                return JsonConvert.DeserializeObject<ApplicationConfiguration>(file.ReadToEnd());
            }
        }
    }
}
