using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eoj12.DCS.Toolkit.Data
{
    class ModInfo
    {
        public string FileName { get; private set; }
        public string FileExtension { get; private set; }
        public long FileSize { get; private set; }
        public DateTime ModificationDate { get; private set; }
        public string ContentType { get; set; }
        public Uri ResponseUri { get; set; }


        public ModInfo(string fileName, string fileExtension, long fileSize, DateTime modificationDate, string contentType, Uri responseUri)
        {
            this.FileName = fileName;
            this.FileExtension = fileExtension;
            this.FileSize = fileSize;
            this.ModificationDate = modificationDate;
            ModificationDate = modificationDate;
            ContentType = contentType;
            ResponseUri = responseUri;  
        }
    }

}
