using LinqToDB;
using LinqToDB.AspNet;
using LinqToDB.AspNet.Logging;
using LinqToDB.Common;
using LinqToDB.Configuration;
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

namespace AspPix
{
    public static class Info
    {

        public static AspPixInfo GetAspPixInfo(this IConfiguration configuration)
        {
            return configuration.GetSection(AspPixInfo.Key_Name).Get<AspPixInfo>();
        }

        public static AspPixInfo GetAspPixInfo(this IServiceProvider service)
        {
            return service.GetRequiredService<IConfiguration>().GetAspPixInfo();
        }

        //public static IEnumerable<string> CreateTags()
        //{

        //    var db = Info.DbCreateFunc();

        //    var data = DateTime.Now.AddDays(-7);

        //    var pixiv2 = db.GetTable<PixivData>()
        //        .Where(p => p.Date > data)
        //        .OrderByDescending(p => p.Mark).Take(10000);

        //    var hasTag = db.GetTable<PixivTagMap>()
        //        .InnerJoin(pixiv2, (a, b) => a.ItemId == b.Id, (a, b) => a);



        //    var tagId = db.GetTable<PixivTag>()
        //        .InnerJoin(hasTag, (a, b) => a.Id == b.TagId, (a, b) => a)
        //        .GroupBy(p => p.Id)
        //        .Select(p => new { Id = p.Key, Count = p.Count() });




        //    var tags = db.GetTable<PixivTag>()
        //        .InnerJoin(tagId, (a, b) => a.Id == b.Id, (a, b) => new { a.Tag, a.Id, b.Count })
        //        .OrderByDescending(p => p.Count)
        //        .Take(150);


        //    return tags.ToArray().Select(p => p.Tag).ToArray();

        //}

    }


    public class AppDataConnection : DataConnection
    {
        public AppDataConnection(LinqToDbConnectionOptions<AppDataConnection> options)
            : base(options)
        {

        }



        public ITable<Fs.PixSql.PixImg> PixImg => GetTable<Fs.PixSql.PixImg>();
        public ITable<Fs.PixSql.PixivData> PixData => GetTable<Fs.PixSql.PixivData>();
        public ITable<Fs.PixSql.PixivHtml> PixHtml => GetTable<Fs.PixSql.PixivHtml>();
        public ITable<Fs.PixSql.PixivTag> PixTag => GetTable<Fs.PixSql.PixivTag>();
        public ITable<Fs.PixSql.PixivTagMap> PixTagMap => GetTable<Fs.PixSql.PixivTagMap>();
        public ITable<Fs.PixSql.PixLive> PixLive => GetTable<Fs.PixSql.PixLive>();


    }


    public class AspPixInfo
    {
        public const string Key_Name = nameof(AspPixInfo);

        public Uri CLOUDFLARE_HOST { get; set; }

        public Uri BASEURI { get; set; }

        public Uri REFERER { get; set; }

        public string DNS { get; set; }

        public string SNI { get; set; }

        public int PORT { get; set; }

        public int TAKE_SMALL_IMAGE { get; set; }


        public string DATA_BASE_CONNECT_STRING { get; set; }
    }


    public record PixImgGetHttp(HttpClient Http);

    public class Program
    {
        static void KillSelf()
        {
            Process.GetCurrentProcess().Kill();
        }
        
        static void Exit(Exception e)
        {
            Console.WriteLine($"Task类中有未观察到的异常 {e.GetType()}");

            KillSelf();
        }

        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.CancelKeyPress += (obj, e) => KillSelf();

            TaskScheduler.UnobservedTaskException += (obj, e) => Exit(e.Exception);

            Configuration.ContinueOnCapturedContext = false;
            Configuration.Linq.GuardGrouping = false;



            var host = CreateHostBuilder(args).Build();

            
            host.Start();

            var info = host.Services.GetAspPixInfo();

            AspPix.Fs.PixCrawling.run(() => host.Services.GetRequiredService<AppDataConnection>(), () => host.Services.GetRequiredService<Fs.PixCrawling.PixGetHtmlService>(), info.BASEURI, info.REFERER.AbsoluteUri);        
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();

                    var basePath = AppDomain.CurrentDomain.BaseDirectory;

                    var logFolderPath = Path.Combine(basePath, "logs");

                    Directory.CreateDirectory(logFolderPath);

                    var path = Path.Combine(logFolderPath, "myapp-{Date}.txt");


                    logging.AddFile(path);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class Startup
    {


        public IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            services.AddControllers();

            services.AddHttpClient<PixImgGetHttp>();

            services.AddHttpClient<Fs.PixCrawling.PixGetHtmlHttp>()
                .ConfigurePrimaryHttpMessageHandler((iser) =>
                {
                    var info = iser.GetRequiredService<IConfiguration>().GetSection(AspPixInfo.Key_Name).Get<AspPixInfo>();

                    return Fs.PixHTTP.createSocketsHttpHandler(info.DNS, info.PORT, info.SNI);
                });

            services.AddTransient<Fs.PixCrawling.PixGetHtmlService>();

           
            services.AddLinqToDbContext<AppDataConnection>((provider, options) => {
                options      
                .UseMySql(_configuration.GetConnectionString("Default"))
                .UseDefaultLogging(provider);
            });
          
        }

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