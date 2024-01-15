using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eoj12.DCS.Toolkit.Utilites
{
    public static class StreamHelper
    {
        public static FileStream ConvertMemoryStreamToFileStream(MemoryStream memoryStream, string outputFilePath)
        {
            // Create a new file stream and copy the contents of the memory stream to it.
            using (var fileStream = new FileStream(outputFilePath, FileMode.Create))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.CopyTo(fileStream);
                return fileStream;
            }
        }
        public static void ConvertMemoryFile(MemoryStream memoryStream, string outputFilePath)
        {
            // Create a new file stream and copy the contents of the memory stream to it.
            using (var fileStream = new FileStream(outputFilePath, FileMode.Create))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.CopyTo(fileStream);
            }
        }
    }
}
