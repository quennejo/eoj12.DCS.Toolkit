using eoj12.DCS.Toolkit.Names;
using eoj12.DCS.Toolkit.Models;
using eoj12.DCS.Toolkit.Data;
using eoj12.DCS.Toolkit.Utilites;



namespace eoj12.DCS.Toolkit.Services
{
    public class ModManagerService
    {
        public string ModManagerPath { get; set; }
        public string ModManagerTempPath { get; set; }
        public string DbPath { get; set; }

        private LocalDb _localDb;
        public LocalDb LocalDb
        {
            get
            {
                if (_localDb == null)
                {
                    _localDb = new LocalDb();
                }
                return _localDb;
            }
            set { _localDb = value; }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        public ModManagerService()
        {
            DbPath = @$"{FileSystem.Current.AppDataDirectory}\\localDb.json";
            LocalDb = LocalDb.DeserializeObject(DbPath);
            if (LocalDb.Settings.DCSSaveGamesPath != null)
            {
                ModManagerPath = @$"{LocalDb.Settings.DCSSaveGamesPath}\{General.MOD_MANAGER_PATH}";
                ModManagerTempPath = @$"{LocalDb.Settings.DCSSaveGamesPath}\{General.MOD_MANAGER_TEMP_PATH}";
                EnsureDirectory(ModManagerTempPath);
            }

        }
        /// <summary>
        /// ensure directory exists, if not create it
        /// </summary>
        /// <param name="path"></param>
        private static void EnsureDirectory(string path)
        {
            // Check if directory exists
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

        }
        /// <summary>
        /// Get mods from local db
        /// </summary>
        /// <param name="excludeModDefinitions"></param>
        /// <returns></returns>
        public async Task<List<Mod>> GetMods(bool excludeModDefinitions)
        {

            List<Mod> mods = LocalDb.CopyMods(excludeModDefinitions);

            return mods.OrderBy(m => m.Title).ToList();


        }
        /// <summary>
        /// Get settings from local db
        /// </summary>
        /// <returns></returns>
        public async Task<Settings> GetSettings()
        {
            return LocalDb.Settings;
        }

        /// <summary>
        /// Save settings to local db
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="Exception"></exception>
        public async void SaveSettings(Settings settings)
        {
            if (Directory.Exists(settings.DCSSaveGamesPath))
            {
                LocalDb.Settings.DCSSaveGamesPath = settings.DCSSaveGamesPath;
                ModManagerPath = @$"{LocalDb.Settings.DCSSaveGamesPath}\{General.MOD_MANAGER_PATH}";
                ModManagerTempPath = @$"{LocalDb.Settings.DCSSaveGamesPath}\{General.MOD_MANAGER_TEMP_PATH}";
                EnsureDirectory(ModManagerTempPath);
                SaveLocalDb();
            }
            else
            {
                throw new Exception("DCS Save game path does not exist!");
            }
        }

        /// <summary>
        /// Delete local db
        /// </summary>
        public async void DeleteLocalDb()
        {
            File.Delete(DbPath);
            LocalDb = null;
        }

        /// <summary>
        /// Download file from url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<List<Mod>> DownloadFileDefinitionAsync(string url)
        {

            LocalDb.ModDefinitionUrl = url;
            var webFileInfo = await DownloadFileAsync(url);
            return await DownloadFileDefinitionAsync(webFileInfo.Stream);
        }

        /// <summary>
        /// Download file definition from stream    
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        //public async Task<List<Mod>> DownloadFileDefinitionAsync(Stream stream)
        public async Task<List<Mod>> DownloadFileDefinitionAsync(MemoryStream memoryStream)
        {
            List<Mod> mods = new List<Mod>();
            var squadronModeDefinitionList = Mod.DeserializeObject(memoryStream);
            var dbModDefinitionList = LocalDb.CopyMods(false);


            foreach (var squadronMod in squadronModeDefinitionList)
            {
                var dbMod = dbModDefinitionList.FirstOrDefault(m => squadronMod.Title.ToLower() == m.Title.ToLower() && m.IsModDefinition);
                // No Match
                if (dbMod == null)
                {
                    squadronMod.IsModDefinition = true;
                    squadronMod.IsDisable = false;
                    squadronMod.IsPreviousVersion = false;
                    squadronMod.IsDownloaded = false;
                    
                    var potentialMatch = dbModDefinitionList.FirstOrDefault(m => (m.Title.Replace("_", "").Replace("-", "").Replace(" ", "").ToLower().Contains(squadronMod.Title.Replace("_", "").Replace("-", "").Replace(" ", "").ToLower())
                                                                                || squadronMod.Title.Replace("_", "").Replace("-", "").Replace(" ", "").ToLower().Contains(m.Title.Replace("_", "").Replace("-", "").Replace(" ", "").ToLower()))
                                                                                && m.TargetFolder.ToLower() == squadronMod.TargetFolder.ToLower()
                                                                                && m.Version.ToLower() != squadronMod.Version.ToLower()
                                                                                && !m.IsModDefinition);
                    if (potentialMatch != null && potentialMatch.Title.ToLower() != "liveries")
                    {
                        squadronMod.IsPotentialMatch = true;
                        squadronMod.PotentialMatch = potentialMatch;
                    }
                    else
                    {
                        squadronMod.IsPotentialMatch = false;
                    }
                    mods.Add(squadronMod);
                }
                //Match but need update
                else if (squadronMod?.Version.ToLower() != dbMod.Version.ToLower())
                {
                    var mod = new Mod()
                    {
                        Title = squadronMod.Title,
                        Url = squadronMod.Url,
                        Version = squadronMod.Version,
                        Size = squadronMod.Size,
                        Description = squadronMod.Description,
                        TargetFolder = squadronMod.TargetFolder,
                        IsDisable = dbMod.IsDisable,
                        IsPreviousVersion = true,
                        IsDownloaded = true,
                        IsModDefinition = true,
                    };
                    mods.Add(mod);

                }
                //Match
                else
                {
                    var mod = dbMod.CopyTo(dbMod);
                    mod.Url = squadronMod.Url;
                    mod.TargetFolder = squadronMod.TargetFolder;
                    mod.Description = squadronMod.Description;
                    mod.IsModDefinition = true;
                    mod.IsPotentialMatch = false;
                    mod.PotentialMatch = null;
                    mods.Add(mod);
                }

            }
            mods = mods.OrderBy(m => m.Title).ToList();
            return mods;
        }

        ///// <summary>
        ///// Download or Update a mod from a url
        ///// </summary>
        ///// <param name="mod"></param>
        ///// <param name="Update">Fals by Default</param>
        ///// <returns></returns>
        public async Task<Mod> DownloadMod(Mod mod, bool Update = false)
        {
            var url = mod.Url.ToString();
            var webFileInfo = await DownloadFileAsync(url);
            mod.IsDownloading = false;
            if (Update)
                DeleteMod(mod);
            mod.ModEntries = ArchiveService.ExtractFileFromStream(webFileInfo, LocalDb.Settings.DCSSaveGamesPath, mod.TargetFolder,ModManagerTempPath);
            mod.IsDownloaded = true;
            mod.IsPreviousVersion = false;
            mod.IsModDefinition = true;
            mod.IsPotentialMatch = false;
            mod.PotentialMatch = null;
            LocalDb.Mods.Add(mod);
            mod.IsExtracting = false;
            SaveLocalDb();
            ScanMods();
            return mod;
        }

        /// <summary>
        /// match a mod with a potential match
        /// </summary>
        /// <param name="mod"></param>
        public async void Match(Mod mod)
        {
            var dbMod = LocalDb.Mods.FirstOrDefault(m => !m.IsModDefinition && m.Title == mod.PotentialMatch.Title && m.TargetFolder.ToLower() == mod.PotentialMatch.TargetFolder.ToLower());

            if (dbMod != null)
            {

                dbMod.Title = mod.Title;
                dbMod.Description = mod.Description;
                dbMod.Version = mod.Version;
                dbMod.IsDownloaded = true;
                dbMod.IsPreviousVersion = false;
                dbMod.IsModDefinition = true;
                dbMod.Url = mod.Url;
                mod.IsPotentialMatch = false;
                mod.PotentialMatch = null;

            }
            SaveLocalDb();
        }

        /// <summary>
        /// scan the mods folder and return a list of mods
        /// </summary>
        /// <returns></returns>
        public List<Mod> ScanMods()
        {
            List<Mod> localMods = new List<Mod>();
            localMods.AddRange(ScanModsFolder(Folders.AIRCRAFT));
            localMods.AddRange(ScanModsFolder(Folders.TECH));
            localMods.AddRange(ScanModsFolder(Folders.LIVERIES));
            return localMods.OrderBy(m => m.Title).ToList();
        }

        /// <summary>
        /// export a list of mods to a json file
        /// </summary>
        /// <param name="mods"></param>
        /// <returns></returns>
        public string ExportMods(List<Mod> mods)
        {

            string timeStamp = DateTime.Now.ToString("yyyyMMddhhmmss");
            string fileName = string.Format(@"{0}\Mods_{1}.json", ModManagerPath, timeStamp);
            Mod.SerializeObject(mods.Select(m => m.CopyTo(m, false, false,false)).ToList(), fileName);
            return fileName;
        }

        /// <summary>
        /// scan a mod folder and return a list of mods
        /// </summary>
        /// <param name="localMods"></param>
        /// <param name="modPath"></param>
        private List<Mod> ScanModsFolder(string modPath)
        {
            List<Mod> localMods = new List<Mod>();
            DirectoryInfo directoryInfo = new DirectoryInfo(LocalDb.Settings.DCSSaveGamesPath + modPath);
            if (directoryInfo.Exists)
            {
                var directories = directoryInfo.GetDirectories().Where(d => !d.FullName.ToLower().EndsWith("tacview")).ToList(); //exclude Tacview
                foreach (var modDirectory in directories)
                {
                    Mod localMod;
                    if (modPath == Folders.LIVERIES)
                    {
                        var LiveryDirectories = modDirectory.GetDirectories();
                        foreach (var liveryDirectories in LiveryDirectories)
                        {
                            localMod = CreateModEntries(modPath, liveryDirectories);
                            if (localMod != null)
                                localMods.Add(localMod);
                        }

                    }
                    else
                    {
                        localMod = CreateModEntries(modPath, modDirectory);
                        if (localMod != null)
                            localMods.Add(localMod);
                    }
                }
            }

            foreach (var localMod in localMods)
            {
                //find parent mod
                string searchKey = modPath == Names.Folders.LIVERIES ? $@"\{localMod.Title}\{localMod.Folder}\" : $@"\{localMod.Title}\";
                var dbDefinitionMod = LocalDb.Mods.FirstOrDefault(m => m.IsModDefinition && m.ModEntries.Any(e => e.Name.ToLower() == "" && e.Path.EndsWith($@"{localMod.Title}/") || e.Path.Contains(searchKey)));
                var dbMod = LocalDb.Mods.FirstOrDefault(m => !m.IsModDefinition && m.Folder.ToLower() == localMod.Folder.ToLower());
                if (dbDefinitionMod != null)
                {
                    if (dbMod == null)
                    {
                        localMod.Title = dbDefinitionMod.Title;
                        localMod.Description = dbDefinitionMod.Description;
                        localMod.Version = dbDefinitionMod.Version;
                    }
                    else
                    {
                        dbMod.Title = dbDefinitionMod.Title;
                        dbMod.Description = string.IsNullOrEmpty(dbMod.Description) ? dbDefinitionMod.Description : dbMod.Description;
                        dbMod.Version = dbDefinitionMod.Version;
                    }
                }
                if (dbMod == null)
                {
                    LocalDb.Mods.Add(localMod);
                }
                else
                {
                    dbMod.IsDisable = false;
                }

            }
            SaveLocalDb();
            return localMods;
        }

        /// <summary>
        /// create a mod entry from a directory
        /// </summary>
        /// <param name="modPath"></param>
        /// <param name="modDirectory"></param>
        /// <returns></returns>
        private Mod CreateModEntries(string modPath, DirectoryInfo modDirectory)
        {

            var localMod = new Mod(modPath == Folders.LIVERIES ? modDirectory.Parent.Name : modDirectory.Name, modDirectory.Name, "", "", "", modPath, true, false);
            var subDirectories = modDirectory.GetDirectories("*.*", SearchOption.AllDirectories);
            ModEntry modEntry = new ModEntry(modDirectory.Name, modDirectory.FullName, true);
            localMod.ModEntries.Add(modEntry);
            foreach (var subDirectory in subDirectories)
            {
                modEntry = new ModEntry(subDirectory.Name, subDirectory.FullName, true);
                localMod.ModEntries.Add(modEntry);
            }
            var files = modDirectory.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (FileInfo f in files)
            {
                modEntry = new ModEntry(f.Name, f.FullName, false, f.Length);
                localMod.ModEntries.Add(modEntry);
            }
            var size = localMod.ModEntries.Sum(modEntry => modEntry.Length);
            localMod.Size = FileHelper.GetSize(size);
            return localMod;
        }



        /// <summary>
        /// Add a mod to the database
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="file"></param>
        /// <param name="udpate"></param>
        /// <returns></returns>
        public async Task<Mod> AddMod(Mod mod, FileResult file, bool udpate)
        {
            var dbMod = LocalDb.Mods.FirstOrDefault(m => m.Title.ToLower() == mod.Title.ToLower());
            WebFileInfo fileInfo = null;
            if (file != null)
            {
                fileInfo = await DownloadService.GetWebFileInfo(file);
            }
            else
            {
                if (GoogleService.IsGoogleUrl(mod.Url)) {
                    var googleService = new GoogleService();
                    fileInfo = await googleService.DownloadFileFromGoogleDrive(mod.Url.ToString());
                }
                else
                    fileInfo = await DownloadService.GetWebFileInfoAndFixGoogleUrl(mod.Url);
            }
            if (fileInfo != null && (fileInfo.FileExtension.ToLower() == ".zip" || fileInfo.FileExtension.ToLower() == ".rar"))
            {
                if (dbMod == null)
                {
                    dbMod = new Mod(mod.Title, "", mod.Description, mod.Version, fileInfo.ResponseUri != null ? fileInfo.ResponseUri.ToString() : "", mod.TargetFolder, false, true)
                    {
                        Size = FileHelper.GetSize(fileInfo.FileSize),
                        IsModDefinition = true,
                    };
                }
                else
                {
                    dbMod.Description = mod.Description;
                    dbMod.Version = mod.Version;
                    dbMod.TargetFolder = mod.TargetFolder;
                    dbMod.IsDownloaded = true;
                    dbMod.IsPreviousVersion = dbMod.Version.ToLower() == mod.Version.ToLower() ? false : true;
                    dbMod.IsModDefinition = true;
                    dbMod.Url = mod.Url;
                    dbMod.Size = FileHelper.GetSize(fileInfo.FileSize);
                }
                if (udpate)
                {
                    dbMod.ModEntries = ArchiveService.ExtractFileFromStream(fileInfo, LocalDb.Settings.DCSSaveGamesPath, mod.TargetFolder, ModManagerTempPath);
                    LocalDb.Mods.Add(dbMod);
                    SaveLocalDb();

                }
                return mod.CopyTo(dbMod);
            }
            else { return null; };
        }
        /// <summary>
        /// Update a mod
        /// </summary>
        /// <param name="mod"></param>
        public async void UpdateMod(Mod mod)
        {
            var dbMod = LocalDb.Mods.FirstOrDefault(m => m.Folder.ToLower() == mod.Folder.ToLower() && !m.IsModDefinition);

            if (dbMod != null)
            {
                if (mod.Version != dbMod.Version)
                {
                    LocalDb.Mods.Where(m => m.Title == mod.Title).ToList().ForEach(m =>
                    {
                        m.Version = mod.Version;
                    });
                }
                dbMod.Description = mod.Description;
            }
            SaveLocalDb();
        }

      
        /// <summary>
        /// Delete a mod
        /// </summary>
        /// <param name="mod"></param>
        public void DeleteMod(Mod mod)
        {
            List<Mod> dbMods = null;
            //if (mod.IsModDefinition)
            dbMods = LocalDb.Mods.Where(m => (m.Title.ToLower() == mod.Title.ToLower() && m.IsModDefinition)
                    || m.Folder.ToLower() == mod.Folder.ToLower() && !m.IsModDefinition).ToList();
            //else
            //    dbMods = LocalDb.Mods.Where(m => m.Folder.ToLower() == mod.Folder.ToLower() && !m.IsModDefinition).ToList();

            dbMods?.ForEach(dbMod =>
                {
                    foreach (var modEntry in dbMod.ModEntries.Where(e => e.IsDirectory == false))
                    {
                        try
                        {
                            File.Delete($"{modEntry.Path}");
                        }
                        catch (Exception ex)
                        {
                            //A mod directory can be part of many mods
                        }
                    }
                    var directoryEntries = dbMod.ModEntries.Where(e => e.IsDirectory).OrderByDescending(e => e.Path).ToList();
                    foreach (var modEntry in directoryEntries)
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(modEntry.Path);
                        if (directoryInfo.Exists && directoryInfo.GetFiles().Length == 0)
                        {
                            if (directoryInfo.GetDirectories().Count() == 0)
                            {
                                try {
                                    Directory.Delete(modEntry.Path);
                                }
                                catch (Exception ex)
                                {
                                    //A directory can have system files on it   
                                }
                            }
                        }
                    }
                    LocalDb.Mods.Remove(dbMod);
                    //Remove mod definition
                    if (!LocalDb.Mods.Any(m => m.Title == mod.Title && !m.IsModDefinition))
                    {
                        var dbModDefinition = LocalDb.Mods.FirstOrDefault(m => m.Title == mod.Title && m.IsModDefinition);
                        if (dbModDefinition != null)
                        {
                            LocalDb.Mods.Remove(dbModDefinition);
                        }
                    }
                    SaveLocalDb();
                });
        }
        /// <summary>
        /// Disable a mod
        /// </summary>
        /// <param name="mod"></param>
        public void DisableMod(Mod mod)
        {

            var dbMod = LocalDb.Mods.FirstOrDefault(m => m.Folder.ToLower() == mod.Folder.ToLower() && !m.IsModDefinition);
            var directoryEntries = dbMod.ModEntries.Where(e => e.IsDirectory).OrderBy(e => e.Path).ToList();
            foreach (var modEntry in directoryEntries)
            {
                var tempPath = modEntry.Path.Replace(LocalDb.Settings.DCSSaveGamesPath, ModManagerTempPath);
                DirectoryInfo directoryInfo = new DirectoryInfo(tempPath);
                if (!directoryInfo.Exists)
                {
                    Directory.CreateDirectory(tempPath);
                }

            }
            foreach (var modEntry in dbMod.ModEntries.Where(e => e.IsDirectory == false))
            {
                var tempPath = modEntry.Path.Replace(LocalDb.Settings.DCSSaveGamesPath, ModManagerTempPath);
                try
                {
                    File.Move(modEntry.Path, tempPath, true);
                }
                catch (Exception ex)
                {
                    //A mod directory can be part of many mods
                }

            }
            directoryEntries = dbMod.ModEntries.Where(e => e.IsDirectory).OrderByDescending(e => e.Path).ToList();
            foreach (var modEntry in directoryEntries)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(modEntry.Path);
                if (directoryInfo.Exists && directoryInfo.GetFiles().Length == 0)
                {
                    if (directoryInfo.GetDirectories().Count() == 0)
                        Directory.Delete(modEntry.Path);
                }
            }

            dbMod.IsDisable = true;
            SaveLocalDb();

        }

        /// <summary>
        /// Enable a mod
        /// </summary>
        /// <param name="mod"></param>
        public void EnableMod(Mod mod)
        {

            var dbMod = LocalDb.Mods.FirstOrDefault(m => m.Folder.ToLower() == mod.Folder.ToLower() && !m.IsModDefinition);
            var directoryEntries = dbMod.ModEntries.Where(e => e.IsDirectory).OrderBy(e => e.Path).ToList();
            foreach (var modEntry in directoryEntries)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(modEntry.Path);
                if (!directoryInfo.Exists)
                {
                    Directory.CreateDirectory(modEntry.Path);
                }
            }
            foreach (var modEntry in dbMod.ModEntries.Where(e => e.IsDirectory == false))
            {
                var tempPath = modEntry.Path.Replace(LocalDb.Settings.DCSSaveGamesPath, ModManagerTempPath);
                try
                {
                    File.Move(tempPath, modEntry.Path, true);
                }
                catch (Exception ex)
                {
                    //A mod directory can be part of many mods
                }
            }
            directoryEntries = dbMod.ModEntries.Where(e => e.IsDirectory).OrderByDescending(e => e.Path).ToList();
            foreach (var modEntry in directoryEntries)
            {
                var tempPath = modEntry.Path.Replace(LocalDb.Settings.DCSSaveGamesPath, ModManagerTempPath);
                DirectoryInfo directoryInfo = new DirectoryInfo(tempPath);
                if (directoryInfo.Exists && directoryInfo.GetFiles().Length == 0)
                {
                    if (directoryInfo.GetDirectories().Count() == 0)
                        Directory.Delete(tempPath);
                }
            }

            dbMod.IsDisable = false;
            SaveLocalDb();

        }

        /// <summary>
        /// Download a file from a url
        /// </summary>
        /// <param name = "url" ></ param >
        /// < returns ></ returns >
        public async Task<WebFileInfo> DownloadFileAsync(string url)
        {
            var downloadService = new DownloadService(LocalDb);
            return await downloadService.DownloadFileAsync(url);
        }


        public void SaveLocalDb()
        {
            LocalDb.SerializeObject(LocalDb, DbPath);
        }
    }
}

