using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer
{
    public class ImageDto
    {
        public int Id { get; set; }
        public string Format { get; set; }
        public byte[] Image { get; set; }
        public string Url { get; private set; }
        ImageDto() { }
        public ImageDto(int id, string format, byte[] image)
        {
            Id = id;
            Format = format;
            Image = image;
        }
        public void UpdateUrl()
        {
            Url = $"data:{Format};base64,{Convert.ToBase64String(Image)}";
        }
    }
}
