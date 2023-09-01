using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eoj12.DCS.Toolkit.Utilites
{
    public static class ImageHelper
    {

        public static string ImageToBase64(string imagePath)
        {
            try
            {
                // Read the image file into a byte array
                byte[] imageBytes = File.ReadAllBytes(imagePath);

                // Convert the byte array to a Base64 string
                string base64String = Convert.ToBase64String(imageBytes);

                return base64String;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting image to Base64: {ex.Message}");
                return null;
            }
        }
    }
}
