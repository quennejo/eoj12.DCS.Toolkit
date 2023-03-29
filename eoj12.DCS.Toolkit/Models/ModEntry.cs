using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eoj12.DCS.Toolkit.Models
{
    public class ModEntry
    {
        public ModEntry()
        {

        }

        public ModEntry(string name, string path, bool isDirectory)
        {
            Name = name;
            Path = path;
            IsDirectory = isDirectory;

        }
        public ModEntry(string name, string path, bool isDirectory, long length)
        {
            Name = name;
            Path = path;
            IsDirectory = isDirectory;
            Length = length;
            
        }

        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public long Length { get; set; }
        public long CompressedLength { get; set; }
        public DateTimeOffset LastWriteTime { get; set; }
    }
}
