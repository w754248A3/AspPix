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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Pixiv2 = AspPix.Fs.PixSql.Pixiv2;
using PixivTag = AspPix.Fs.PixSql.PixivTag;
using PixivTagHas = AspPix.Fs.PixSql.PixivTagHas;

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

        public class ReloadTag
        {

            [PrimaryKey, Identity]
            public int Index { get; set; }

            [Column(Length = 250, CanBeNull = false)]
            public string Tag { get; set; }
        }

        public static void LogLine(string s)
        {
            Console.WriteLine($"{DateTime.Now}:{s}");
        }

        public static Func<DataConnection> DbCreateFunc { get; private set; }

        public static IEnumerable<string> Tags { get; private set; }

        public static Func<string, string, Task<byte[]>> GetImg { get; private set; }
     
        public static IEnumerable<string> CreateTags()
        {
            using var db = Info.DbCreateFunc();

            //var v = db.GetTable<Pixiv2>()
            //.OrderByDescending(item => item.Mark)
            //.Take(ConstValue.TAG_LOAD_POOL_COUNT);




            //var b = db.GetTable<PixivTagHas>()
            //.InnerJoin(v, (left, right) => left.ItemId == right.Id, (left, right) => left)
            //.Select(item => item.TagId)
            //.Distinct();



            //var c = db.GetTable<PixivTag>()
            //.InnerJoin(b, (left, right) => left.Id == right, (left, right) => left.Tag);




            //c.Take(ConstValue.TAG_LOAD_COUNT).Insert(db.GetTable<ReloadTag>(), (p) => new ReloadTag { Tag = p });


            return db.GetTable<ReloadTag>().Select(p => p.Tag).ToArray();
        }

        public static void Init()
        {
            Configuration.ContinueOnCapturedContext = false;

            DbCreateFunc = () =>
            {

                var db = new DataConnection(
                    ProviderName.MySql,
                    $"Host=192.168.0.101;Port=3306;User=myuser;Password=mypass;Database=mysql;SslMode=none");


                db.CommandTimeout = 3 * 60;


                return db;
            };

            var func = Fs.PixHTTP.createGetByteFunc(new Uri("https://morning-bird-d5a7.sparkling-night-bc75.workers.dev/"), "https://www.pixiv.net");

            GetImg = async (s, s2) =>
            {

                IEnumerable<Task<byte[]>> cf()
                {
                    yield return func.Invoke(s);
                    yield return func.Invoke(s2);
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



            Tags = CreateTags();

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
            TaskScheduler.UnobservedTaskException += (obj, e) => { Console.WriteLine(e.Exception); Environment.Exit(0); };
            
            Info.Init();

            AspPix.Fs.PixCrawling.run();
          
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