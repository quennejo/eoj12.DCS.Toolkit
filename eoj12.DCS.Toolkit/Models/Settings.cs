using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eoj12.DCS.Toolkit.Models
{
    public class Settings
    {
        public string DCSSaveGamesPath { get; set; }
        public string SquadronUrl{ get; set; }
        public string LogoBase64 { get; set; }
        public string LogoName { get; set; }
        public bool IsAdmin { get; set; }
    }
}
