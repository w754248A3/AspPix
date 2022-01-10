using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PixivData = AspPix.Fs.PixSql.PixivData;
using PixivTag = AspPix.Fs.PixSql.PixivTag;
using PixivTagMap = AspPix.Fs.PixSql.PixivTagMap;

namespace AspPix
{
    public static class Info
    {
        public class PixImg
        {

            [PrimaryKey]
            public int Id { get; set; }


            public byte[] Img { get; set; }
        }

        public static Func<DataConnection> DbCreateFunc { get; private set; }

        public static IEnumerable<string> Tags { get; private set; }

        public static Func<string, string, Task<byte[]>> GetImg { get; private set; }
     
        public static IEnumerable<string> CreateTags()
        {
            
            var db = Info.DbCreateFunc();
            
            var data = DateTime.Now.AddDays(-7);

            var pixiv2 = db.GetTable<PixivData>()
                .Where(p => p.Date > data)
                .OrderByDescending(p => p.Mark).Take(10000);

            var hasTag = db.GetTable<PixivTagMap>()
                .InnerJoin(pixiv2, (a, b) => a.ItemId == b.Id, (a, b) => a);



            var tagId = db.GetTable<PixivTag>()
                .InnerJoin(hasTag, (a, b) => a.Id == b.TagId, (a, b) => a)
                .GroupBy(p => p.Id)
                .Select(p => new { Id = p.Key, Count = p.Count() });




            var tags = db.GetTable<PixivTag>()
                .InnerJoin(tagId, (a, b) => a.Id == b.Id, (a, b) => new { a.Tag, a.Id, b.Count })
                .OrderByDescending(p => p.Count)
                .Take(150);


            return tags.ToArray().Select(p => p.Tag).ToArray();

        }

        static void SetTags()
        {
            Tags = CreateTags();
        }

        public static void Init()
        {
            Configuration.ContinueOnCapturedContext = false;
            Configuration.Linq.GuardGrouping = false;
            DbCreateFunc = () =>
            {

                var ip = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "127.0.0.1" : "192.168.0.101";

                var db = new DataConnection(
                    ProviderName.MySql,
                    $"Host={ip};Port=3306;User=myuser;Password=mypass;Database=mysql;SslMode=none");


                db.CommandTimeout = 60 * 5;


                return db;
            };


            var func = new HttpClient();



            GetImg = async (s, s2) =>
            {

                IEnumerable<Task<byte[]>> cf()
                {
                    yield return func.GetByteArrayAsync(s);

                    if (s2 is not null)
                    {
                        yield return func.GetByteArrayAsync(s2);
                    }

                   
                }

                foreach (var item in cf())
                {
                    try
                    {
                        return await item.ConfigureAwait(false);

                    }
                    catch (HttpRequestException)
                    {

                    }
                    
                }

                throw new HttpRequestException();
            };



            SetTags();

        }

    }

    public static class ConstValue
    {


        public const string IMG_HTTPCLIENT_KEY = "s.pximg.net";

        public const string IMG_HTTPCLIENT_SNI = "s.pximg.net";

        public const string IMG_HTTPCLIENT_BASEADDRESS = "http://i.pximg.net/";

        public const string IMG_HTTPCLIENT_REFERER = "https://www.pixiv.net/";



#if DEBUG

        public const int BU_COUNT = 10;

        public const int BU_LOAD_COUNT = 10000;



        public const int TAG_LOAD_COUNT = 0;

        public const int TAG_LOAD_POOL_COUNT = 0;



        public const int TAKE_SMALL_IMAGE = 50;


        
#else

        public const int BU_COUNT = 100;

        public const int BU_LOAD_COUNT = 100;



        public const int TAG_LOAD_COUNT = 100;

        public const int TAG_LOAD_POOL_COUNT = 100;


        public const int TAKE_SMALL_IMAGE = 20;

#endif
    }



    public class Program
    {
        
        
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CancelKeyPress += (ibj, e) => Environment.Exit(0);
            TaskScheduler.UnobservedTaskException += (obj, e) => { Console.WriteLine($"{e.Exception.GetType()} {e.Exception.Message}"); Environment.Exit(0); };
            
            Info.Init();

            var http = Fs.PixHTTP.createGetHTMLFunc(new Uri("http://www.pixiv.net/artworks/"), "www.pixivision.net", 443, "www.pixivision.net", "https://www.pixivision.net");

            AspPix.Fs.PixCrawling.run(Info.DbCreateFunc, http);
          
            var host = CreateHostBuilder(args).Build();

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}