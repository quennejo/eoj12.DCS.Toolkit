using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eoj12.DCS.Toolkit.Models
{
    public class WebFileInfo
    {
        public string FileName { get; private set; }
        public string FileExtension { get; private set; }
        public long FileSize { get; private set; }
        public DateTime ModificationDate { get; private set; }
        public string ContentType { get; set; }
        public Uri ResponseUri { get; set; }

        public MemoryStream Stream { get; set; }
        public string FilePath { get; set; }
        public WebFileInfo(string fileName, string fileExtension, long fileSize, DateTime modificationDate, string contentType, Uri responseUri)
        {
            FileName = fileName;
            FileExtension = fileExtension;
            FileSize = fileSize;
            ModificationDate = modificationDate;
            ModificationDate = modificationDate;
            ContentType = contentType;
            ResponseUri = responseUri;
            

        }
    }

}
