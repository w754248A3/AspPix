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
using System.Collections.Generic;

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

    public class InsertImgService
    {
        readonly AppDataConnection _db;

        readonly ILogger _logger;

        public InsertImgService(AppDataConnection db, ILogger<IntoSqliteService> logger)
        {
            _db = db;
            _logger = logger;
        }


        void Inser(System.Threading.Channels.ChannelReader<PixImg> reader)
        {

            var vs = new List<PixImg>();
            while (true)
            {
                if (reader.TryRead(out var pixImg))
                {
                   
                    vs.Add(pixImg);

                    if (vs.Count >= 10)
                    {
                        

                        using var tc = _db.BeginTransaction();

                        Array.ForEach(vs.ToArray(), (p) => _db.InsertOrReplace(p));

                        tc.Commit();

                        return;
                    }
                }
                else
                {
                    Thread.Sleep(new TimeSpan(0, 0, 3));
                }

               
            }


            
        }

        public static System.Threading.Channels.ChannelWriter<PixImg> Writer { get; private set; }

        public static void Init(IHost host)
        {



            var inserImg = host.Services.GetRequiredService<InsertImgService>();

            var chann = System.Threading.Channels.Channel.CreateBounded<PixImg>(100);
            var read = chann.Reader;

            Writer = chann.Writer;

            var th = new Thread(() => inserImg.Run(read));

            th.Start();

        }

        public void Run(System.Threading.Channels.ChannelReader<PixImg> reader)
        {
            LogExit.OnErrorExit(nameof(InsertImgService), _logger, () =>
            {
                
                while (true)
                {
                    Inser(reader);
                    _logger.LogError("insetimgrunone");
                }

            });
        }
    }

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
            //Xcopy /y /E $(ProjectDir)wwwroot $(OutDir)wwwroot

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

            InsertImgService.Init(host);

            FreeConsole();

            into.Run(1000, reader);

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
                    logging.AddFile(path, minimumLevel: LogLevel.Error);
                });
                
    }
}