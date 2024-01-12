using eoj12.DCS.Toolkit.Data;
using eoj12.DCS.Toolkit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace eoj12.DCS.Toolkit.Services
{
    public class DownloadService
    {
        public LocalDb LocalDb { get; internal set; }
        public DownloadService(LocalDb localDb) {
            LocalDb=localDb;
        }


        /// <summary>
        /// Download a file from a url
        /// </summary>
        /// <param name = "url" ></ param >
        /// < returns ></ returns >
        public async Task<WebFileInfo> DownloadFileAsync(string url)
        {
            WebFileInfo modInfo = null;


            Uri uri = new Uri(url);
            if (GoogleService.IsGoogleUrl(uri) && LocalDb.Settings.UseGoogleApi)
            {
                var googleService = new GoogleService();
                modInfo = await googleService.DownloadFileFromGoogleDrive(url);
            }
            else
            {
                modInfo = await GetWebFileInfoAndFixGoogleUrl(url);
                HttpClient client = new HttpClient();
                client.Timeout = new TimeSpan(1, 0, 0);
                var response = await client.GetAsync(modInfo.ResponseUri);
                response.EnsureSuccessStatusCode();
                modInfo.Stream = (MemoryStream)await response.Content.ReadAsStreamAsync();
            }
            return modInfo;
        }
        /// <summary>
        /// Get the web content
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static async Task<string> GetWebContent(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetStringAsync(url);
            }
        }
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<WebFileInfo> GetWebFileInfoAndFixGoogleUrl(string url)
        {
            var webFileInfo = await GetWebFileInfo(url);
            var googleUrl = await FormatGoogleUrlWithToken(webFileInfo);
            if (googleUrl != webFileInfo.ResponseUri.ToString())
                webFileInfo = await GetWebFileInfo(googleUrl);
            return webFileInfo;
        }

        public static async Task<WebFileInfo> GetWebFileInfo(string url)
        {
            //Uri retUri = null;
            WebFileInfo modInfo = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 3600000;//60 minutes
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

                modInfo = new WebFileInfo(fileName, fileExtension, fileSize, modificationDate, response.ContentType, new Uri(url));
            }
            response.Close();
            return modInfo;
        }
        public static async Task<WebFileInfo> GetWebFileInfo(WebResponse response)
        {

            WebFileInfo modInfo = null;
            string fileName = "";
            string fileExtension = "";

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

            modInfo = new WebFileInfo(fileName, fileExtension, fileSize, modificationDate, response.ContentType, response.ResponseUri);
            return modInfo;
        }
        public static async Task<WebFileInfo> GetWebFileInfo(FileResult file)
        {
            WebFileInfo modInfo = null;
            //Get file size from FileResult

            modInfo = new WebFileInfo(file.FileName, Path.GetExtension(file.FileName), 0, DateTime.Now, file.ContentType, null);
            var memoryStream = new MemoryStream();
            //await file.OpenReadStream(maxAllowedSize: 3221225472, cancellationToken: default).CopyToAsync(memoryStream);
            var bufferStream = await file.OpenReadAsync();
            await bufferStream.CopyToAsync(memoryStream);
            modInfo.Stream = memoryStream;
            return modInfo;
        }



        ///// <summary>
        ///// Download or Update a mod from a url
        ///// </summary>
        ///// <param name="mod"></param>
        ///// <param name="Update">Fals by Default</param>
        ///// <returns></returns>
        //public async Task<Mod> DownloadMod(Mod mod, bool Update = false)
        //{


        //    var url = mod.Url.ToString();
        //    var webFileInfo = await DownloadFileAsync(url);
        //    mod.IsDownloading = false;
        //    if (Update)
        //        DeleteMod(mod);
        //    mod.ModEntries = ExtractFileFromStream(webFileInfo, LocalDb.Settings.DCSSaveGamesPath, mod.TargetFolder);
        //    mod.IsDownloaded = true;
        //    mod.IsPreviousVersion = false;
        //    mod.IsModDefinition = true;
        //    mod.IsPotentialMatch = false;
        //    mod.PotentialMatch = null;
        //    LocalDb.Mods.Add(mod);
        //    mod.IsExtracting = false;
        //    SaveLocalDb();
        //    ScanMods();
        //    return mod;
        //}
        /// <summary>
        /// Fix the google url
        /// </summary>
        /// <param name="webFileInfo"></param>
        /// <returns></returns>
        public static async Task<string> FormatGoogleUrlWithToken(WebFileInfo webFileInfo)
        {

            string returnUrl = webFileInfo.ResponseUri.ToString();
            if (webFileInfo.ContentType == "text/html; charset=utf-8" && GoogleService.IsGoogleUrl(webFileInfo.ResponseUri))
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

    }
}
