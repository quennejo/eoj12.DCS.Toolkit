﻿using eoj12.DCS.Toolkit.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System.Web;

namespace eoj12.DCS.Toolkit.Services
{
    public class GoogleService
    {
        public DriveService DriveService { get; set; }
        public GoogleService()
        {
            DriveService = GetGoogleDriveService();
        }
        private static UserCredential Login()
        {
            var appSettings = AppConfigService.GetAppSettings();
            ClientSecrets secrets = new ClientSecrets
            {
                ClientId = appSettings.ClientId,
                ClientSecret = appSettings.ClientSecret
            };
            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, new[] { "https://www.googleapis.com/auth/drive.file" }, "user", CancellationToken.None).Result;
            return credential;
        }
        private static GoogleCredential LoginFromFile()
        {
            string PathToServiceAccountKeyFile = string.Format($@"{ FileSystem.Current.AppDataDirectory}\service_account.json");
            // Load the Service account credentials and define the scope of its access.
            var credential = GoogleCredential.FromFile(PathToServiceAccountKeyFile)
                            .CreateScoped(DriveService.ScopeConstants.Drive);

            return credential;
        }

        private static GoogleCredential LoginFromJson()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "service_account.json");
            string json = System.IO.File.ReadAllText(filePath);
            // Load the Service account credentials and define the scope of its access.
             var credential = GoogleCredential.FromJson(json)
                            .CreateScoped(DriveService.ScopeConstants.Drive);

            return credential;
        }

        public DriveService GetGoogleDriveService()
        {
            var credential = LoginFromJson();
            var driveService = new DriveService(new BaseClientService.Initializer() { HttpClientInitializer = credential });
            return driveService;
        }


        /// <summary>
        /// Download a Document file in PDF format.
        /// </summary>
        /// <param name = "fileId" > file ID of any workspace document format file.</param>
        /// <returns>byte array stream if successful, null otherwise.</returns>
        public async Task<WebFileInfo> DownloadFileFromGoogleDrive(string url)
        {
            try
            {
                WebFileInfo webFileInfo = null;
                var uri = new Uri(FormatGoogleUrl(url));
                var fileId = HttpUtility.ParseQueryString(uri.Query).Get("id");
                var request = DriveService.Files.Get(fileId);
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
                    await request.DownloadAsync(stream);
                    webFileInfo = new WebFileInfo(file.Name, Path.GetExtension(file.Name), stream.Length, DateTime.Now, file.MimeType, new Uri(url))
                    {
                        Stream = stream
                    };
                }
                else
                {
                    Console.WriteLine("File not found!");
                }
                return webFileInfo;
            }
            catch (Exception e)
            {
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
        public static bool IsGoogleUrl(string url)
        {
            Uri uri = new Uri(url);
            return IsGoogleUrl(uri);
        }
        public static bool IsGoogleUrl(Uri uri)
        {
            return uri.Host == "drive.google.com";
        }

    }
}
