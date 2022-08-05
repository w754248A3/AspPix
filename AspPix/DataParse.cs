using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AspPix
{
    public static class DataParse
    {
        public static DateTime GetDateTimeFromUri(Uri uri)
        {
            static int getNumber(string s) => int.Parse(s.Trim('/'));

            string[] args = uri.Segments;

            int year = getNumber(args[3]);
            int month = getNumber(args[4]);
            int day = getNumber(args[5]);
            int hour = getNumber(args[6]);
            int minute = getNumber(args[7]);
            int second = getNumber(args[8]);
            return new DateTime(year, month, day, hour, minute, second);
        }


        public static string GetPathFromDateTime(DateTime dt)
        {
            static string GetString(int n) => n.ToString() switch
            {
                string { Length: 1 } s => "0" + s,
                string s => s 
            };


            return string.Join('/',
                GetString(dt.Year),
                GetString(dt.Month),
                GetString(dt.Day),
                GetString(dt.Hour),
                GetString(dt.Minute),
                GetString(dt.Second));
        }


        public static IEnumerable<string> GetImgUri(PixivData pd, int count)
        {
            string ex = pd.Flags == 0 ? "jpg" : "png";

            string path = GetPathFromDateTime(pd.Date);

            return Enumerable.Range(0, count)
                .Select(n => $"/img-original/img/{path}/{pd.Id}_p{n}.{ex}");
        }

        public static string GetSmallImgUri(PixivData pd, bool b)
        {
            string ex = b ? "_master1200.jpg" : "_p0_master1200.jpg";

            string path = GetPathFromDateTime(pd.Date);

            return $"/c/540x540_70/img-master/img/{path}/{pd.Id}{ex}";
        }


        static Regex _original;
        static Regex _mark;
        static Regex _tags;
        public static void Init()
        {
            _original = new(@"""original"":""([^""]+)""", RegexOptions.Compiled);

            _mark = new(@"""bookmarkCount"":(\d+),", RegexOptions.Compiled);
         
            _tags = new(@"""tags"":(\[\{[^\]]+\}\]),", RegexOptions.Compiled);

        }

        public static int GetMarkCount(string html)
        {
            return int.Parse(_mark.Match(html).Groups[1].Value);

        }

        public static Uri GetUri(string html)
        {
            return new Uri(_original.Match(html).Groups[1].Value);
        }

        public static int GetIsJpg(Uri uri)
        {
            return uri.AbsolutePath.EndsWith(".jpg") ? 0 : 1;
        }


        public static string[] GetTag(string html)
        {
            static string[] GetArray(string json)
            {
                using var js = JsonDocument.Parse(json);
                return js.RootElement.EnumerateArray()
                    .Select(p => p.GetProperty("tag").GetString())
                    .ToArray();
            }

            Match m = _tags.Match(html);

            if (m.Success)
            {
                return GetArray(m.Groups[1].Value);
            }
            else
            {
                return Array.Empty<string>();
            }
        }

        public static PixivHtml CreatePD(string html, int id)
        {
            int mark = GetMarkCount(html);

            Uri uri = GetUri(html);

            DateTime dt = GetDateTimeFromUri(uri);

            int isjpg = GetIsJpg(uri);

            string[] tags = GetTag(html);
            return new PixivHtml
            {
                Id = id,

                Mark = mark,

                Date = dt,

                Flags = isjpg,

                Tags = tags

            };
        }
    }
}