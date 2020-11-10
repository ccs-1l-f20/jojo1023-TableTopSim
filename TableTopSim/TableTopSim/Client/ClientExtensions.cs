using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlazorInputFile;

namespace TableTopSim.Client
{
    public static class ClientExtensions
    {
        public static async Task<(byte[] bytes, string format)> GetImageBytes(this IFileListEntry rawFile)
        {
            if (rawFile == null)
            {
                return (null, null);
            }
            // Load as an image file in memory
            string format = "image/jpeg";
            if (rawFile.Type != null && rawFile.Type.Length > 0)
            {
                format = rawFile.Type;
            }
            IFileListEntry imageFile;
            if (format == "image/svg+xml")
            {
                imageFile = rawFile;
            }
            else
            {
                imageFile = await rawFile.ToImageFileAsync(format, 100000, 100000);
            }
            var ms = new MemoryStream();
            await imageFile.Data.CopyToAsync(ms);

            return (ms.ToArray(), format);

        }
    }
}
