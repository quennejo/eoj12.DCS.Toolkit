using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;


namespace eoj12.DCS.Toolkit.Services
{
   
    public class AppConfigService
    {
        public static AppSettings GetAppSettings()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<AppSettings>(json);
        }
    }

}
