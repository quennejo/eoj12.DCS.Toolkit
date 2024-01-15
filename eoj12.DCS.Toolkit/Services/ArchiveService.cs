using eoj12.DCS.Toolkit.Names;
using System.Net;
using eoj12.DCS.Toolkit.Models;
using System.Web;
using eoj12.DCS.Toolkit.Data;
using System.IO;
using System.IO.Compression;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using SharpCompress.Archives;
using eoj12.DCS.Toolkit.Utilites;
using Radzen;
namespace eoj12.DCS.Toolkit.Services
{
    public static class ArchiveService
    {
        /// <summary>
        /// Extract a zip file from a stream
        /// </summary>
        /// <param name="webFileInfo"></param>
        /// <param name="saveGamePath"></param>
        /// <param name="targetFolder"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<ModEntry> ExtractFileFromStream(WebFileInfo webFileInfo, string saveGamePath, string targetFolder,string modManagerTempPath)
        {
            List<ModEntry> entries = null;
            var tempFilePath= @$"{modManagerTempPath}\{Guid.NewGuid()}_{webFileInfo.FileName}";
            if (webFileInfo.FileExtension == ".zip")
            {
                //StreamHelper.ConvertMemoryStreamToFileStream(webFileInfo.Stream,tempFilePath);
                entries = ExtractZipFromStream(webFileInfo.Stream, saveGamePath, targetFolder);
            }
            else if (webFileInfo.FileExtension == ".rar")
            { //(RarArchive.IsRarFile(stream)){

                StreamHelper.ConvertMemoryFile(webFileInfo.Stream, tempFilePath);
                entries = ExtractRarFromFileInfo(new System.IO.FileInfo(tempFilePath), saveGamePath, targetFolder);
            }
            else
            {
                throw new Exception("File format not supported");
            }
            return entries;

        }

        /// <summary>
        /// Extract a zip file from a stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="outputPath"></param>
        /// <param name="targetFolder"></param>
        /// <returns></returns>
        //public static List<ModEntry> ExtractZipFromStream(string archiveFilePath, string outputPath, string targetFolder)
        //{
        //    using (ZipArchive archive = ZipFile.Open(archiveFilePath, ZipArchiveMode.Read))
        public static List<ModEntry> ExtractZipFromStream(Stream stream, string outputPath, string targetFolder)
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                List<ModEntry> entries = new List<ModEntry>();
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!entry.Name.ToLower().Contains("desktop.ini") && !entry.Name.ToLower().Contains("thumbs.db"))
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
                            string fileName = Path.GetFileName(modEntry.Path);
                            string folder = Path.GetFullPath(modEntry.Path).Replace(fileName, "");
                            EnsureDirectory(folder);
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
                //var fileInfo = new System.IO.FileInfo(archiveFilePath);
                //fileInfo.Delete();
                return entries;
            }
        }

        /// <summary>
        /// Extract a rar file from a fileinfo
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="outputPath"></param>
        /// <param name="targetFolder"></param>
        /// <returns></returns>
        public static List<ModEntry> ExtractRarFromFileInfo(System.IO.FileInfo fileInfo, string outputPath, string targetFolder)
        {

            EnsureDirectory(outputPath);
            var modFolder = outputPath.ToLower().Split("\\").LastOrDefault();
            List<ModEntry> entries = new List<ModEntry>();
            using (var archive = RarArchive.Open(fileInfo))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.Key.ToLower().Contains("desktop.ini") && !entry.Key.ToLower().Contains("thumbs.db"))
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

                            EnsureDirectory(folder);

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
    }
}
