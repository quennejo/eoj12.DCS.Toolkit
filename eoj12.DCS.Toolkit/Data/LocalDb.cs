using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace eoj12.DCS.Toolkit.Data
{
    public class LocalDb
    {
        public string ModDefinitionUrl { get; set; }
        List<Mod> _mods;
        public List<Mod> Mods 
        {
            get
            {
                if (_mods == null)
                {
                    _mods = new List<Mod>();                 
                };
                return _mods;//.Where(m=>m.IsDownloaded).ToList();   
            }
            set { _mods = value; }
        }

        


        // Serialization method for a list of objects
        public static void SerializeObject(LocalDb localDb, string filePath)
        {
            string jsonString = JsonSerializer.Serialize(localDb);
            File.WriteAllText(filePath, jsonString);
        }
        public static LocalDb DeserializeObject(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<LocalDb>(jsonString);
            }
            catch (Exception)
            {
                return new LocalDb();
            }

        }

        public  List<Mod> CopyMods()
        {
            List<Mod> mods = new List<Mod>();
            Mods.ForEach(mod => mods.Add(mod.CopyTo(mod)));
            return mods;
        }

}
}
