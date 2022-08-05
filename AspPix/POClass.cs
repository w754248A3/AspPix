using LinqToDB.Mapping;
using System;

namespace AspPix
{
    public class PixivData
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int Mark { get; set; }
        public DateTime Date { get; set; }
        public int Flags { get; set; }
    }


    public class PixImg
    {
        [PrimaryKey]
        public int Id { get; set; }

        public byte[] Img { get; set; }

    }

    public class PixLive
    {
        [PrimaryKey]
        public int Id { get; set; }

        public byte[] Img { get; set; }
    }

    public class PixivTag
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Tag { get; set; }
    }

    public class PixivTagMap
    {
        [PrimaryKey(0)]
        public int ItemId { get; set; }
        [PrimaryKey(1)]
        public int TagId { get; set; }
    }

    public class PixivHtml : PixivData
    {
        public string[] Tags { get; set; }
    }

}