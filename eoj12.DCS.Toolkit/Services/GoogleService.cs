using eoj12.DCS.Toolkit.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static Google.Apis.Drive.v3.DriveService;
using static System.Formats.Asn1.AsnWriter;

namespace eoj12.DCS.Toolkit.Services
{
    public static class GoogleService
    {


        private static UserCredential Login()
        {
            var appSettings = AppConfigService.GetAppSettings();
            ClientSecrets secrets = new ClientSecrets
            {
                ClientId = appSettings.ClientId,
                ClientSecret = appSettings.ClientSecret
            };
            var service = GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, new[] { "https://www.googleapis.com/auth/drive.readonly" }, "user", CancellationToken.None).Result;
            return service;
        }
      

        private static DriveService GetGoogleDriveService()
        {
            UserCredential credential = Login();
            var driveService = new DriveService(new BaseClientService.Initializer() { HttpClientInitializer = credential });
            return driveService;
        }


        /// <summary>
        /// Download a Document file in PDF format.
        /// </summary>
        /// <param name = "fileId" > file ID of any workspace document format file.</param>
        /// <returns>byte array stream if successful, null otherwise.</returns>
        public static async Task<WebFileInfo> DownloadFileFromGoogleDrive(string url)
        {
            try
            {

                WebFileInfo webFileInfo = null;
                var uri = new Uri(FormatGoogleUrl(url));
                var fileId = HttpUtility.ParseQueryString(uri.Query).Get("id");
                var service = GetGoogleDriveService();
                //var service = GetDriveService();
                var request = service.Files.Get(fileId);
                var stream = new MemoryStream();


                // Add a handler which will be notified on progress changes.
                // It will notify on each chunk download and when the
                // download is completed or failed.
                request.MediaDownloader.ProgressChanged +=
                    progress =>
                    {
                        switch (progress.Status)
                        {
                            case DownloadStatus.Downloading:
                                {
                                    Console.WriteLine(progress.BytesDownloaded);
                                    break;
                                }
                            case DownloadStatus.Completed:
                                {
                                    Console.WriteLine("Download complete.");
                                    break;
                                }
                            case DownloadStatus.Failed:
                                {
                                    Console.WriteLine("Download failed.");
                                    break;
                                }
                        }
                    };

                var file = request.Execute();

                if (file != null)
                {
                    webFileInfo = new WebFileInfo(file.Name, Path.GetExtension(file.Name), 111, DateTime.Now, file.MimeType, new Uri(url));
                    await request.DownloadAsync(stream);
                    webFileInfo.Stream = stream;
                }
                else
                {
                    Console.WriteLine("File not found!");
                }
                return webFileInfo;
            }
            catch (Exception e)
            {
                // TODO(developer) - handle error appropriately
                if (e is AggregateException)
                {
                    Console.WriteLine("Credential Not found");
                }
                else
                {
                    throw;
                }
            }
            return null;
        }

        /// <summary>
        /// Format Google Url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string FormatGoogleUrl(string url)
        {

            var uri = new Uri(url);
            var ids = uri.ToString().Split('/');
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
                var qs = uri.Query; //"userID=16555&gameID=60&score=4542.122&time=343114";
                var parsed = HttpUtility.ParseQueryString(qs);
                documentId = parsed["id"];
            }
            string retUrl = string.Format("https://drive.google.com/uc?export=download&id={0}", documentId);
            return retUrl;
        }
    }
}
