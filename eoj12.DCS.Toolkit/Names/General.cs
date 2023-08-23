using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eoj12.DCS.Toolkit.Names
{
    public static class General
    {
        public const string MOD_MANAGER_PATH = @"DCS.Toolkit";
        public const string MOD_MANAGER_TEMP_PATH = @"DCS.Toolkit\Temp";
        public const string DB_Name = "localDb.json";
    }
    public static class Folders
    {
        public const string ROOT = $@"\";
        public const string MODS = $@"\mods";
        public const string AIRCRAFT = $@"\mods\aircraft";
        public const string TECH = $@"\mods\tech";
        public const string LIVERIES = $@"\liveries";

        public static List<string> FoldersList { get; internal set; } = new List<string>() {AIRCRAFT, TECH, LIVERIES };

    }
   
}
