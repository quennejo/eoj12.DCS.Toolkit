using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace eoj12.DCS.Toolkit.Models
{
    public class Mod
    {
        public string Title { get; set; } = "";
        public string Folder { get; set; } = "";
        public string Description { get; set; }
        public string Version { get; set; } = "";
        public string Size { get; set; }
        public string Url { get; set; }
        public string FolderPath { get; set; }
        public string TargetFolder { get; set; }
        public bool IsDownloaded { get; set; }
        public bool IsDownloading { get; set; }
        public bool IsExtracting { get; set; }
        public bool IsPreviousVersion { get; set; }
        public bool IsDisable { get; set; }
        public bool IsModDefinition { get; set; }
        public bool IsPotentialMatch { get; set; }
        public Mod  PotentialMatch { get; set; }

        private List<ModEntry> _modEntries;
        public List<ModEntry> ModEntries
        {
            get
            {
                _modEntries ??= new List<ModEntry>();
                return _modEntries;
            }
            set { _modEntries = value; }
        }

        public Mod()
        {

        }
        public Mod(string title,string folder, string description, string version, string url, string targetFolder, bool isDownloaded,bool isModDefinition)
        {
            Title = title;
            Folder = folder;
            Description = description;
            Version = version;
            Url = url;
            TargetFolder = targetFolder;
            IsDownloaded = isDownloaded;
            IsDownloading = isModDefinition;
        }


        // Serialization method for a list of objects
        public static void SerializeObject(List<Mod> list, string filePath)
        {
            string jsonString = JsonSerializer.Serialize(list);
            File.WriteAllText(filePath, jsonString);
        }

        // Deserialization method for a list of objects
        public static List<Mod> DeserializeObject(string filePath)
        {
            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<Mod>>(jsonString);
        }
         
        public static List<Mod> DeserializeObject(FileStream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonString = reader.ReadToEnd();
                return JsonSerializer.Deserialize<List<Mod>>(jsonString);
            }
        }
        public static List<Mod> DeserializeObject(MemoryStream stream)
        {
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonString = reader.ReadToEnd();
                return JsonSerializer.Deserialize<List<Mod>>(jsonString);
            }
        }

        //public static async Task<List<Mod>> DeserializeObject(Stream stream)
        //{
        //    using (StreamReader reader = new StreamReader(stream))
        //    {
        //        string jsonString = await reader.ReadToEndAsync();
        //        return JsonSerializer.Deserialize<List<Mod>>(jsonString);
        //    }
        //}

        public Mod CopyTo(Mod mod, bool includeEntries = true,bool includeStatusProperties=true,bool includePotentialMatch=true)
        {
            Mod modCopy = GetModCopy(mod, includeEntries, includeStatusProperties, includePotentialMatch);
            return modCopy;
        }

        private static Mod GetModCopy(Mod mod, bool includeEntries, bool includeStatusProperties, bool includePotentialMatch)
        {
            var retVal = new Mod()
            {
                Description = mod.Description,
                ModEntries = includeEntries ? mod.ModEntries : null,
                Size = mod.Size,
                TargetFolder = mod.TargetFolder,
                Title = mod.Title,
                Url = mod.Url,
                FolderPath = mod.FolderPath,
                Version = mod.Version,
                Folder = mod.Folder,
                IsModDefinition = mod.IsModDefinition,

            };
            if (includeStatusProperties)
            {
                retVal.IsDisable = mod.IsDisable;
                retVal.IsDownloaded = mod.IsDownloaded;
                retVal.IsPreviousVersion = mod.IsPreviousVersion;
            }
            if (includePotentialMatch)
            {
                retVal.IsPotentialMatch = mod.IsPotentialMatch;
                retVal.PotentialMatch = mod.IsPotentialMatch ? GetModCopy(mod.PotentialMatch, false, false, true) : null;
            }
            return retVal;
        }
    }
}

