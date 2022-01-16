using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static Func<HttpClient, string, string, Task<byte[]>> GetImg { get; private set; }
     
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
            //Tags = CreateTags();
            Tags = Array.Empty<string>();
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


            GetImg = async (func, s, s2) =>
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



        public const string DNS = "www.pixivision.net";

        public const string SNI = "www.pixivision.net";

        public const int PORT = 443;

        public const string REFERER = "https://www.pixivision.net";

        public const string BASEURI = "http://www.pixiv.net/artworks/";


#endif
    }


    public record PixImgGetHttp(HttpClient Http);

    public class Program
    {
        static void KillSelf()
        {
            Process.GetCurrentProcess().Kill();
        }
        
        static void Exit(object obj)
        {
            Debug.WriteLine(obj);

            Debug.Flush();

            KillSelf();
        }

        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.CancelKeyPress += (obj, e) => KillSelf();

            TaskScheduler.UnobservedTaskException += (obj, e) => Exit(e.Exception);

            AppDomain.CurrentDomain.UnhandledException += (obj, e) => Exit(e.ExceptionObject);


            Info.Init();

            
            var host = CreateHostBuilder(args).Build();


            host.Start();


            AspPix.Fs.PixCrawling.run(Info.DbCreateFunc, () => host.Services.GetRequiredService<Fs.PixCrawling.PixGetHtmlService>(), new Uri(ConstValue.BASEURI), ConstValue.REFERER);


            
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((log) => {

                    log.ClearProviders();

                    log.AddConsole();
                    log.AddDebug();

                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            services.AddControllers();

            services.AddHttpClient<PixImgGetHttp>();

            services.AddHttpClient<Fs.PixCrawling.PixGetHtmlHttp>()
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return Fs.PixHTTP.createSocketsHttpHandler(ConstValue.DNS, ConstValue.PORT, ConstValue.SNI);
                });

            services.AddTransient<Fs.PixCrawling.PixGetHtmlService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();

            app.UseRouting();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();


                endpoints.MapControllers();
            });
        }
    }
}