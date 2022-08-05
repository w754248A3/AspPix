using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Expressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;

namespace AspPix
{
    public static class AspPixExtensions
    {

        public static AspPixInfo GetAspPixInfo(this IConfiguration configuration)
        {
            return configuration.GetSection(AspPixInfo.Key_Name).Get<AspPixInfo>();
        }

        public static AspPixInfo GetAspPixInfo(this IServiceProvider service)
        {
            return service.GetRequiredService<IConfiguration>().GetAspPixInfo();
        }
    }


    public class AppDataConnection : DataConnection
    {
        public AppDataConnection(LinqToDBConnectionOptions<AppDataConnection> options)
            : base(options)
        {
            
        }



        public ITable<PixImg> PixImg => this.GetTable<PixImg>();
        public ITable<PixivData> PixData => this.GetTable<PixivData>();
        public ITable<PixivTag> PixTag => this.GetTable<PixivTag>();
        public ITable<PixivTagMap> PixTagMap => this.GetTable<PixivTagMap>();
        public ITable<PixLive> PixLive => this.GetTable<PixLive>();


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

        public string CONNECT_STRING { get; set; }


        public string LOGS_PATH { get; set; }
    }


    public record PixImgGetHttp(HttpClient Http);

    public record PixGetHtmlHttp(HttpClient Http);

    public record PixGetHtmlService(PixGetHtmlHttp Http);

    public class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        static void InitDataBaseTable(IHost host)
        {
            var db = host.Services.GetRequiredService<AppDataConnection>();



            db.CreateTable<PixImg>(tableOptions: TableOptions.CreateIfNotExists);
            db.CreateTable<PixivData>(tableOptions: TableOptions.CreateIfNotExists);
            db.CreateTable<PixivTagMap>(tableOptions: TableOptions.CreateIfNotExists);
            db.CreateTable<PixLive>(tableOptions: TableOptions.CreateIfNotExists);
            db.CreateTable<PixivTag>(tableOptions: TableOptions.CreateIfNotExists);

        }

      

        public static void Main(string[] args)
        {
            
            args = new string[] { "--urls=http://127.0.0.1:80/" };


            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.CancelKeyPress += (obj, e) => LogExit.KillSelf();

            Configuration.ContinueOnCapturedContext = false;
            Configuration.Linq.GuardGrouping = false;



            var host = CreateHostBuilder(args).Build();


            DataParse.Init();
            InitDataBaseTable(host);

            host.Start();

            var http = host.Services.GetRequiredService<PixCrawlingService>();
            var info = host.Services.GetAspPixInfo();
            var reader = http.Run(info.BASEURI, info.REFERER.AbsoluteUri);

            var into = host.Services.GetRequiredService<IntoSqliteService>();

            into.Run(10, reader);

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();



                    var path = Path.Combine(
                        hostingContext.Configuration.GetAspPixInfo().LOGS_PATH,
                        "myapp-{Date}.txt");

                    logging.AddConsole();
                    logging.AddFile(path);
                });
                
    }
}