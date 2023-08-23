using System.IO;
using System.IO.Compression;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using SharpCompress.Archives;
using eoj12.DCS.Toolkit.Names;
using System.Net;
using System;
using eoj12.DCS.Toolkit.Models;
using Microsoft.Maui.Media;
using eoj12.DCS.Toolkit.Pages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Maui.Controls;
using System.Web;
using System.Net.WebSockets;
using SharpCompress;
using eoj12.DCS.Toolkit.Data;

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

        private void EnsureDirectory(string path)
        {
            // Check if directory exists
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

        }

        public async Task<List<Mod>> GetMods(bool excludeModDefinitions)
        {

            List<Mod> mods = LocalDb.CopyMods(excludeModDefinitions);

            return mods.OrderBy(m => m.Title).ToList();


        }
        public async Task<Settings> GetSettings()
        {
            return LocalDb.Settings;
        }
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
        public async void DeleteLocalDb()
        {
            File.Delete(DbPath);
            LocalDb = null;
        }

        public async Task<List<Mod>> DownloadFileDefinitionAsync(string url)
        {

            LocalDb.ModDefinitionUrl = url;
            var webFileInfo = await DownloadFileAsync(url);
            return await DownloadFileDefinitionAsync(webFileInfo.Stream);
        }

        public async Task<List<Mod>> DownloadFileDefinitionAsync(Stream stream)
        {
            List<Mod> mods = new List<Mod>();
            var squadronModeDefinitionList = await Mod.DeserializeObject(stream);
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
                    var potentialMatch =dbModDefinitionList.FirstOrDefault(m =>( m.Title.ToLower().Contains(squadronMod.Title.ToLower()) || squadronMod.Title.ToLower().Contains(m.Title.ToLower())) && m.TargetFolder.ToLower() == squadronMod.TargetFolder.ToLower() && !m.IsModDefinition);
                    if (potentialMatch != null)
                    {
                        squadronMod.IsPotentialMatch = true;
                        squadronMod.PotentialMatch = potentialMatch;
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
                    mods.Add(mod);
                }

            }
            mods.OrderBy(m => m.Title).ToList();
            return mods;
        }

        public async void Match(Mod mod)
        {
            var dbMod = LocalDb.Mods.FirstOrDefault(m => m.Title == mod.PotentialMatch.Title && m.TargetFolder.ToLower() == mod.PotentialMatch.TargetFolder.ToLower());

            if (dbMod != null)
            {
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

        public List<Mod> ScanMods()
        {
            List<Mod> localMods = new List<Mod>();
            ScanModsFolder(localMods, Folders.AIRCRAFT);
            ScanModsFolder(localMods, Folders.TECH);
            ScanModsFolder(localMods, Folders.LIVERIES);
            return localMods.OrderBy(m => m.Title).ToList();
        }
        public string ExportMods(List<Mod> mods)
        {

            string timeStamp = DateTime.Now.ToString("yyyyMMddhhmmss");
            string fileName = string.Format(@"{0}\Mods_{1}.json", ModManagerPath, timeStamp);
            Mod.SerializeObject(mods.Select(m => m.CopyTo(m, false, false)).ToList(), fileName);
            return fileName;
        }

        private void ScanModsFolder(List<Mod> localMods, string modPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(LocalDb.Settings.DCSSaveGamesPath + modPath);
            if (directoryInfo.Exists)
            {
                var directories = directoryInfo.GetDirectories();
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
                string searchKey = $@"\{localMod.Title}\";
                var dbParentMod = LocalDb.Mods.FirstOrDefault(m => m.IsModDefinition && m.ModEntries.Any(e => e.Name.ToLower() == "" && e.Path.EndsWith($@"{localMod.Title}/") || e.Path.Contains(searchKey)));
                var dbMod = LocalDb.Mods.FirstOrDefault(m => !m.IsModDefinition && m.Folder.ToLower() == localMod.Folder.ToLower());
                if (dbParentMod != null)
                {
                    if (dbMod == null)
                    {
                        localMod.Title = dbParentMod.Title;
                        localMod.Description = dbParentMod.Description;
                        localMod.Version = dbParentMod.Version;
                    }
                    else
                    {
                        dbMod.Title = dbParentMod.Title;
                        dbMod.Description = dbMod != null && string.IsNullOrEmpty(dbMod.Description) ? dbParentMod.Description : dbMod.Description;
                        dbMod.Version = dbParentMod.Version;
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
        }

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
            localMod.Size = GetSize(size);
            return localMod;
        }

        public string GetSize(long size)
        {
            string sizeString = "";
            if (size > 0)
            {
                float mb = size / 1024 / 1024;
                sizeString = Math.Round(mb, 2) + " MB";
            }
            return sizeString;
        }


        public async Task<Mod> AddMod(Mod mod, IBrowserFile file, bool udpate)
        {
            var dbMod = LocalDb.Mods.FirstOrDefault(m => m.Title.ToLower() == mod.Title.ToLower());
            WebFileInfo fileInfo = null;
            if (file != null)
            {
                fileInfo = await GetWebFileInfo(file);
            }
            else
            {
                fileInfo = await GetWebFileInfoAndFixGoogleUrl(mod.Url);
            }
            if (fileInfo != null && (fileInfo.FileExtension.ToLower() == ".zip" || fileInfo.FileExtension.ToLower() == ".rar"))
            {
                if (dbMod == null)
                {
                    dbMod = new Mod(mod.Title, "", mod.Description, mod.Version, fileInfo.ResponseUri != null ? fileInfo.ResponseUri.ToString() : "", mod.TargetFolder, false, true)
                    {
                        Size = GetSize(fileInfo.FileSize),
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
                }
                if (udpate)
                {
                    dbMod.ModEntries = ExtractFileFromStream(fileInfo, LocalDb.Settings.DCSSaveGamesPath, mod.TargetFolder);
                    LocalDb.Mods.Add(dbMod);
                    SaveLocalDb();

                }
                return mod.CopyTo(dbMod);
            }
            else { return null; };
        }
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

        public static async Task<WebFileInfo> GetWebFileInfoAndFixGoogleUrl(string url)
        {
            var webFileInfo = await GetWebFileInfo(url);
            var googleUrl = await FixGoogleUrl(webFileInfo);
            if (googleUrl != webFileInfo.ResponseUri.ToString())
                webFileInfo = await GetWebFileInfo(googleUrl);
            return webFileInfo;
        }

        public static async Task<WebFileInfo> GetWebFileInfo(IBrowserFile file)
        {
            WebFileInfo modInfo = null;
            //var reader = await new StreamReader(file.OpenReadStream()).ReadToEndAsync();
            //maxAllowedSize : 3Gb

            modInfo = new WebFileInfo(file.Name, Path.GetExtension(file.Name), file.Size, file.LastModified.LocalDateTime, file.ContentType, null);
            var memoryStream = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 3221225472, cancellationToken: default).CopyToAsync(memoryStream);
            modInfo.Stream = memoryStream;
            return modInfo;
        }


        public static async Task<WebFileInfo> GetWebFileInfo(string url)
        {
            //Uri retUri = null;
            WebFileInfo modInfo = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string fileName = "";
            string fileExtension = "";
            //retUri = response.ResponseUri;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Headers["Content-Disposition"] != null)
                {
                    fileName = response.Headers["Content-Disposition"];
                    fileName = fileName.Replace("attachment; filename=", "");
                    fileName = fileName.Replace("\"", "");
                    fileExtension = Path.GetExtension(fileName);
                }
                long fileSize = response.ContentLength;
                DateTime modificationDate;
                DateTime.TryParse(response.Headers["Last-Modified"], out modificationDate);

                modInfo = new WebFileInfo(fileName, fileExtension, fileSize, modificationDate, response.ContentType, new Uri(url));
            }
            response.Close();
            return modInfo;
        }

        public static async Task<string> FixGoogleUrl(WebFileInfo webFileInfo)
        {

            string returnUrl = webFileInfo.ResponseUri.ToString();
            if (webFileInfo.ContentType == "text/html; charset=utf-8" && webFileInfo.ResponseUri.Host == "drive.google.com")// && webFileInfo.ResponseUri.ToString().Contains("sharing"))
            {

                var ids = webFileInfo.ResponseUri.ToString().Split('/');
                var documentId = "";
                if (ids.Length > 0 && ids.Length > 5)
                {
                    documentId = ids[5].Trim();///.Replace("uc?id=","");
                    if (documentId.IndexOf("?") > -1)
                    {
                        var parsed = HttpUtility.ParseQueryString(documentId);
                        documentId = parsed["id"];
                    }


                }
                if (string.IsNullOrEmpty(documentId))
                {
                    var qs = webFileInfo.ResponseUri.Query; //"userID=16555&gameID=60&score=4542.122&time=343114";
                    var parsed = HttpUtility.ParseQueryString(qs);
                    documentId = parsed["id"];
                }
                string newUrl = string.Format("https://drive.google.com/uc?export=download&id={0}", documentId);
                var content = await GetWebContent(newUrl);
                string confirmToken = "";
                int intTokenStart = content.LastIndexOf("confirm=t&amp;uuid=");
                int intTokenEnd = content.IndexOf("\" method=\"post\"");
                if (intTokenStart != -1 && intTokenEnd != -1)
                {
                    confirmToken = content.Substring(intTokenStart, intTokenEnd - intTokenStart).Replace("confirm=t&amp;uuid=", "");
                }
                returnUrl = string.Format("https://drive.google.com/uc?export=download&id={0}&confirm=t&uuid={1}", documentId, confirmToken);
            }
            return returnUrl;
        }

        static async Task<string> GetWebContent(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetStringAsync(url);
            }
        }


        public void DeleteMod(Mod mod)
        {
            List<Mod> dbMods = null;
            if (mod.IsModDefinition)
                dbMods = LocalDb.Mods.Where(m => m.Title.ToLower() == mod.Title.ToLower()).ToList();
            else
                dbMods = LocalDb.Mods.Where(m => m.Folder.ToLower() == mod.Folder.ToLower() && !m.IsModDefinition).ToList();

            if (dbMods != null)
            {
                dbMods.ForEach(dbMod =>
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
                                Directory.Delete(modEntry.Path);
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
        }

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


        public async Task<WebFileInfo> DownloadFileAsync(string url)
        {
            var modInfo = await GetWebFileInfoAndFixGoogleUrl(url);// GetWebFileInfo(url);//
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(modInfo.ResponseUri);//,HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            modInfo.Stream = await response.Content.ReadAsStreamAsync();
            return modInfo;

        }

        public List<ModEntry> ExtractFileFromStream(WebFileInfo webFileInfo, string saveGamePath, string targetFolder)
        {
            List<ModEntry> entries = null;
            if (IsValidZipFile(webFileInfo.Stream))
            {
                entries = ExtractZipFromStream(webFileInfo.Stream, saveGamePath, targetFolder);
            }
            else if (webFileInfo.FileExtension == ".rar")
            { //(RarArchive.IsRarFile(stream)){
                Guid guid = Guid.NewGuid();
                var fileStream = ConvertMemoryStreamToFileStream((MemoryStream)webFileInfo.Stream, @$"{ModManagerTempPath}\{guid}");
                entries = ExtractRarFromFileInfo(new FileInfo(@$"{ModManagerTempPath}\{guid}"), saveGamePath, targetFolder);
            }
            else
            {
                throw new Exception("File format not supported");
            }
            return entries;

        }


        public static List<ModEntry> ExtractZipFromStream(Stream stream, string outputPath, string targetFolder)
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                List<ModEntry> entries = new List<ModEntry>();
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!entry.Name.ToLower().Contains("desktop.ini"))
                    {
                        ModEntry modEntry = new ModEntry()
                        {
                            Name = entry.Name,
                            IsDirectory = string.IsNullOrEmpty(entry.Name) ? true : false,
                            CompressedLength = entry.CompressedLength,
                            //Path = $@"{outputPath}\{entry.FullName}",
                            Path = TrimPath(outputPath, targetFolder, entry.FullName),
                            LastWriteTime = entry.LastWriteTime,
                            Length = entry.Length
                        };
                        entries.Add(modEntry);
                        //string entryOutputPath = Path.Combine(outputPath, entry.FullName);
                        string entryOutputPath = modEntry.Path;
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            // This is a directory, create it if it doesn't exist
                            Directory.CreateDirectory(entryOutputPath);
                        }
                        else
                        {
                            // This is a file, extract it to disk
                            using (Stream entryStream = entry.Open())
                            {
                                using (FileStream output = new FileStream(entryOutputPath, FileMode.Create))
                                {
                                    entryStream.CopyTo(output);
                                }
                            }
                        }
                    }
                }
                return entries;
            }
        }

        public static List<ModEntry> ExtractRarFromFileInfo(FileInfo fileInfo, string outputPath, string targetFolder)
        {

            EnsureFolder(outputPath);
            var modFolder = outputPath.ToLower().Split("\\").LastOrDefault();
            List<ModEntry> entries = new List<ModEntry>();
            using (var archive = RarArchive.Open(fileInfo))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.Key.ToLower().Contains("desktop.ini"))
                    {
                        ModEntry modEntry = new ModEntry()
                        {
                            Name = entry.IsDirectory ? "" : Path.GetFileName(entry.Key),
                            IsDirectory = entry.IsDirectory,
                            CompressedLength = entry.CompressedSize,
                            Path = TrimPath(outputPath, targetFolder, entry.Key),
                            //LastWriteTime = entry.LastModifiedTime,
                            Length = entry.Size
                        };
                        entries.Add(modEntry);

                        if (!entry.IsDirectory)
                        {
                            string fileName = Path.GetFileName(modEntry.Path);
                            string folder = Path.GetFullPath(modEntry.Path).Replace(fileName, "");

                            EnsureFolder(folder);

                            entry.WriteToFile(modEntry.Path, new ExtractionOptions
                            {
                                ExtractFullPath = true,
                                Overwrite = true,

                            });
                        }
                    }
                }
            }
            fileInfo.Delete();
            return entries;
        }

        //C:\Users\xx\Saved Games\DCS.openbetaModManager2\Liveries\  \Liveries\T-45\CT-155201
        //C:\Users\xx\Saved Games\DCS.openbetaModManager3\           \425 Warthog Liveries Pack_02\Liveries\A-10CII\RCAF_425_LowVis_DarkGreenCamo
        //C:\Users\xx\Saved Games\DCS.openbetaModManager3\           \425 Warthog Liveries Pack_02\Mods\aircraft\A-10CII\RCAF_425_LowVis_DarkGreenCamo
        private static string TrimPath(string outputPath, string targetFolder, string modEntryPath)
        {


            var modEntryPathFix = modEntryPath.Replace("/", "\\");
            if (modEntryPathFix.IndexOf("\\") != 0)
                modEntryPathFix = modEntryPathFix.Insert(0, "\\");
            var retVal = $@"{outputPath}{targetFolder}\{modEntryPathFix}";

            if (targetFolder != Folders.ROOT)
            {
                var targetFolderIndex = modEntryPathFix.ToLower().IndexOf(targetFolder);
                var modFolderIndex = modEntryPathFix.ToLower().IndexOf(Folders.MODS);
                if (targetFolderIndex >= 0)
                    retVal = outputPath + targetFolder + modEntryPathFix.Substring(targetFolderIndex + targetFolder.Length);
                else if (modFolderIndex >= 0)
                    retVal = outputPath + targetFolder + modEntryPathFix.Substring(modFolderIndex + Folders.MODS.Length);
            }

            return retVal;

        }

        private static void EnsureFolder(string outputPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(outputPath);
            if (!directoryInfo.Exists)
            {
                Directory.CreateDirectory(outputPath);
            }
        }

        public FileStream ConvertMemoryStreamToFileStream(MemoryStream memoryStream, string outputFilePath)
        {
            // Create a new file stream and copy the contents of the memory stream to it.
            using (var fileStream = new FileStream(outputFilePath, FileMode.Create))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.CopyTo(fileStream);
                return fileStream;
            }
        }
        private static bool IsValidZipFile(Stream stream)
        {
            if (!stream.CanSeek)
            {
                return false;
            }

            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, buffer.Length);

            bool retVal = BitConverter.ToUInt32(buffer, 0) == 0x04034b50;
            return retVal;
        }

        public void SaveLocalDb()
        {
            LocalDb.SerializeObject(LocalDb, DbPath);
        }
    }
}
