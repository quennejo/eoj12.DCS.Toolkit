using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eoj12.DCS.Toolkit.Utilites
{
    public static class FileHelper
    {
        public static string GetSize(long size)
        {
            string sizeString = "";
            if (size > 0)
            {
                float mb = size / 1024 / 1024;
                sizeString = Math.Round(mb, 2) + " MB";
            }
            return sizeString;
        }
        public static long GetSizeMB(long size)
        {
            long retVal = 0;
            if (size > 0)
            {
                float mb = size / 1024 / 1024;
                retVal = (long)Math.Round(mb, 2) ;
            }
            return retVal;
        }


    }
}
