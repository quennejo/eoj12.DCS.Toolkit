using System.IO;
using System.IO.Compression;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using SharpCompress.Archives;
using eoj12.DCS.Toolkit.Names;
using System.Net;
using System;

namespace eoj12.DCS.Toolkit.Data
{
    public class ModManagerService
    {
        public  string DCSSaveGamesPath { get; set; }
        public  string ModManagerPath { get; set; }
        public  string ModManagerTempPath { get; set; }
        public string DbPath { get; set; }
        public LocalDb LocalDb { get; internal set; }

        public ModManagerService()
        {
            DCSSaveGamesPath = @$"C:\Users\joequ\Saved Games\DCS.openbetaModManager";
            ModManagerPath = @$"{DCSSaveGamesPath}\{Names.General.MOD_MANAGER_PATH}";
            ModManagerTempPath = @$"{ModManagerPath}\Temp";
            DbPath = @$"{ModManagerPath}\localDb.json";
            // Check if directory exists
            if (!Directory.Exists(ModManagerPath))
            {
                // Create the directory
                Directory.CreateDirectory(ModManagerPath);
                if (!Directory.Exists(ModManagerTempPath))
                    Directory.CreateDirectory(ModManagerTempPath);
            }
            LocalDb = LocalDb.DeserializeObject(DbPath);
        }

        public async Task DownloadFileDefinitionAsync(List<Mod> modDefinitionList, string url) {
            var contentStream = await DownloadFileAsync(url);
            var squadronModeDefinitionList = Mod.DeserializeObject(contentStream);
            var dbModDefinitionList = LocalDb.Mods;
            LocalDb.ModDefinitionUrl = url;
            SaveLocalDb();

            foreach (var squadronMod in squadronModeDefinitionList)
            {
                var modDefinition = modDefinitionList.FirstOrDefault(m => squadronMod.Title == m.Title);
                if (modDefinition == null)
                {
                    modDefinitionList.Add(squadronMod);
                }
                else if (squadronMod.Version.ToLower() != modDefinition.Version.ToLower())
                {
                    modDefinition.IsPreviousVersion = true;
                    modDefinition.IsDownloaded = true;
                    modDefinition.Url = squadronMod.Url;
                    modDefinition.Version = squadronMod.Version;
                    modDefinition.Size = squadronMod.Size;

                }
            }
            modDefinitionList.OrderBy(m => m.Title).ToList();
        } 

        public List<Mod> ScanMods()
        {
            List<Mod> localMods = new List<Mod>();


            ScanModsFolder(localMods, Folders.AIRCRAFT);
            ScanModsFolder(localMods, Folders.TECH);
            ScanModsFolder(localMods, Folders.LIVERIES);

            return localMods;
        }

        private void ScanModsFolder(List<Mod> localMods, string modPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(DCSSaveGamesPath +modPath);
            if (directoryInfo.Exists)
            {
                var directories = directoryInfo.GetDirectories();
                foreach (var modDirectory in directories)
                {
                    var localMod = new Mod(modDirectory.Name, "", "", "", @$"{modPath}\" + modDirectory.Name, true);
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
                        modEntry = new ModEntry(f.Name, f.FullName, false);
                        localMod.ModEntries.Add(modEntry);
                    }
                    localMods.Add(localMod);
                }
            }
            foreach (var mod in localMods)
            {
                if (!LocalDb.Mods.Any(m => m.Title == mod.Title))
                    LocalDb.Mods.Add(mod);

            }
            SaveLocalDb();
        }


        public async void AddMod(Mod mod)
        {

            var fileInfo =GetFileInfo(mod.Url);
            var url = "";
            if (fileInfo != null && fileInfo.FileExtension.ToLower() == ".zip" || fileInfo.FileExtension.ToLower() == ".rar")
            {
                var dbMod = new Mod(mod.Title, mod.Description, mod.Version, url, mod.TargetFolder, false)
                {
                    Size = fileInfo.FileSize.ToString(),
                };
            }
            else if (fileInfo.ContentType == "text/html; charset=utf-8" && fileInfo.ResponseUri.Host == "drive.google.com")
            {

                //https://drive.google.com/file/d/1JM8nofJk0VH5U6uHxK6-vrMvtrzhlu0I/view?usp=sharing
                var ids =mod.Url.Split('/');
                var documentId = "";
                if (ids.Length > 0)
                { 
                    documentId = ids[5].Trim();
                    
                }
                //"https://drive.google.com/uc?export=download&id=1i88vAEulF-VeS8jfCnOJXqtU53LbuqVA&confirm=t&uuid=8edf8164-8e55-4859-8a52-85dd92aeb58f&at=ALgDtsxOg-eDFuyptLfy6UnwwqeT:1677165836536",
                string newUrl = string.Format("https://drive.google.com/uc?export=download&id={0}",documentId );
                var content = await GetWebContent(mod.Url);

                string confirmTocken = "";
                newUrl = string.Format("https://drive.google.com/uc?export=download&id={0}&confirm=t&uuid={1}",documentId,confirmTocken);
                //var content = await GetWebContent(mod.Url);
                ;
            }
            ;
            ////LocalDb.Mods.Remove(dbMod);
            //SaveLocalDb();
        }

        static ModInfo GetFileInfo(string url)
        {
            ModInfo modInfo = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string fileName = "";
            string fileExtension = "";
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
                
                modInfo = new ModInfo(fileName, fileExtension, fileSize, modificationDate,response.ContentType,response.ResponseUri);
            }
            response.Close();
            return modInfo;
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
            var dbMod = LocalDb.Mods.FirstOrDefault(m=>m.Title == mod.Title && m.Version ==m.Version);  
            foreach (var modEntry in dbMod.ModEntries.Where(e => e.IsDirectory == false))
            {
                File.Delete($"{modEntry.Path}");
            }
            var directoryEntries = dbMod.ModEntries.Where(e => e.IsDirectory).OrderByDescending(e => e.Path).ToList();
            foreach (var modEntry in directoryEntries) { 
                DirectoryInfo directoryInfo = new DirectoryInfo(modEntry.Path);
                if (directoryInfo.Exists && directoryInfo.GetFiles().Length == 0)
                {
                    Directory.Delete(modEntry.Path);
                }
            }
            LocalDb.Mods.Remove(dbMod);
            SaveLocalDb();
        }

        public void DisableMod(Mod mod) {

            var dbMod = LocalDb.Mods.FirstOrDefault(m => m.Title == mod.Title && m.Version == m.Version);
            var directoryEntries = dbMod.ModEntries.Where(e => e.IsDirectory).OrderBy(e => e.Path).ToList();
            foreach (var modEntry in directoryEntries)
            {
                var tempPath = modEntry.Path.Replace(DCSSaveGamesPath, ModManagerTempPath);
                DirectoryInfo directoryInfo = new DirectoryInfo(tempPath);
                if (!directoryInfo.Exists)
                {
                    Directory.CreateDirectory(tempPath);
                }

            }
            foreach (var modEntry in dbMod.ModEntries.Where(e => e.IsDirectory == false))
            {
                var tempPath = modEntry.Path.Replace(DCSSaveGamesPath, ModManagerTempPath);
                File.Move(modEntry.Path, tempPath,true);
            }
            directoryEntries = dbMod.ModEntries.Where(e => e.IsDirectory).OrderByDescending(e => e.Path).ToList();
            foreach (var modEntry in directoryEntries)
            {
                //var tempPath = modEntry.Path.Replace(DCSSaveGamesPath, ModManagerTempPath);
                DirectoryInfo directoryInfo = new DirectoryInfo(modEntry.Path);
                if (directoryInfo.Exists && directoryInfo.GetFiles().Length == 0)
                {
                    Directory.Delete(modEntry.Path);
                }
            }

            dbMod.IsDisable = true;
            SaveLocalDb();

        }
        public void EnableMod(Mod mod)
        {

            var dbMod = LocalDb.Mods.FirstOrDefault(m => m.Title == mod.Title && m.Version == m.Version);
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
                var tempPath = modEntry.Path.Replace(DCSSaveGamesPath, ModManagerTempPath);
                File.Move(tempPath,modEntry.Path, true);
            }
            directoryEntries = dbMod.ModEntries.Where(e => e.IsDirectory).OrderByDescending(e => e.Path).ToList();
            foreach (var modEntry in directoryEntries)
            {
                var tempPath = modEntry.Path.Replace(DCSSaveGamesPath, ModManagerTempPath);
                DirectoryInfo directoryInfo = new DirectoryInfo(tempPath);
                if (directoryInfo.Exists && directoryInfo.GetFiles().Length == 0)
                {
                    Directory.Delete(tempPath);
                }             
            }

            dbMod.IsDisable = false;
            SaveLocalDb();

        }


        public async Task<Stream> DownloadFileAsync( string url)
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var contentStream = await response.Content.ReadAsStreamAsync();
            return contentStream;

        }

        public   List<ModEntry> ExtractFileFromStream(Stream stream, string outputPath)
        {
            List<ModEntry> entries = null ;
            if (IsValidZipFile(stream)) {
                entries =ExtractZipFromStream(stream,outputPath);
            }
            else { //(RarArchive.IsRarFile(stream)){
                Guid guid = Guid.NewGuid();
                var fileStream =ConvertMemoryStreamToFileStream((MemoryStream)stream, @$"{ModManagerTempPath}\{guid}");
                //entries =ExtractRarFromStream(stream, outputPath);
                entries =ExtractRarFromFileInfo(new FileInfo(@$"{ModManagerTempPath}\{guid}"), outputPath);
            }
            //else
            //{
            //    throw new Exception("File format not supported");
            //}
            return entries;

        }

        public static List<ModEntry> ExtractZipFromStream(Stream stream, string outputPath)
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                List<ModEntry> entries = new List<ModEntry>();
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    ModEntry modEntry = new ModEntry() {
                        Name=entry.Name,
                        IsDirectory = string.IsNullOrEmpty(entry.Name)?true:false,
                        CompressedLength = entry.CompressedLength,
                        Path= $@"{outputPath}\{entry.FullName}",
                        LastWriteTime=entry.LastWriteTime,
                        Length=entry.Length 
                    };
                    entries.Add(modEntry);
                    string entryOutputPath = Path.Combine(outputPath, entry.FullName);

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
                return entries;
            }
        }
  

        //public static List<ModEntry> ExtractRarFromStream(Stream stream, string outputPath)
        //{
        //    using (var archive = RarArchive.Open(stream))
        //    {
        //        List<ModEntry> entries = new List<ModEntry>();
        //        foreach (var entry in archive.Entries)
        //        {
        //            ModEntry modEntry = new ModEntry()
        //            {
        //                //Name = entry.,
        //                //CompressedLength = entry.CompressedLength,
        //                //FullName = entry.FullName,
        //                //LastWriteTime = entry.LastWriteTime,
        //                //Length = entry.Length
        //            };
        //            entries.Add(modEntry);
        //            if (!entry.IsDirectory)
        //            {
        //                entry.WriteToDirectory(outputPath, new ExtractionOptions
        //                {
        //                    ExtractFullPath = true,
        //                    Overwrite = true
        //                });
        //            }
        //        }
        //    }
        //    return new List<ModEntry>();
        //}
        public static List<ModEntry> ExtractRarFromFileInfo(FileInfo fileInfo, string outputPath)
        {
            List<ModEntry> entries = new List<ModEntry>();
            using (var archive = RarArchive.Open(fileInfo))
            {               
                foreach (var entry in archive.Entries)
                {
                    ModEntry modEntry = new ModEntry()
                    {
                        Name = entry.IsDirectory ? "" : Path.GetFileName(entry.Key),
                        IsDirectory =entry.IsDirectory,
                        CompressedLength = entry.CompressedSize,
                        Path = $@"{outputPath}\{entry.Key}" ,
                        //LastWriteTime = entry.LastModifiedTime,
                        Length = entry.Size
                    };
                    entries.Add(modEntry);
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(outputPath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
            fileInfo.Delete();
            return entries;
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

            bool retVal= BitConverter.ToUInt32(buffer, 0) == 0x04034b50;
            return retVal;
        }

        public void SaveLocalDb()
        {
            eoj12.DCS.Toolkit.Data.LocalDb.SerializeObject(LocalDb, DbPath);
        }
    }
}
