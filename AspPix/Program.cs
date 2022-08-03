using LinqToDB;
using LinqToDB.AspNet;
using LinqToDB.AspNet.Logging;
using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Expressions;
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
        public AppDataConnection(LinqToDBConnectionOptions<AppDataConnection> options)
            : base(options)
        {
            
        }



        public ITable<Fs.PixSql.PixImg> PixImg => this.GetTable<Fs.PixSql.PixImg>();
        public ITable<Fs.PixSql.PixivData> PixData => this.GetTable<Fs.PixSql.PixivData>();
        public ITable<Fs.PixSql.PixivHtml> PixHtml => this.GetTable<Fs.PixSql.PixivHtml>();
        public ITable<Fs.PixSql.PixivTag> PixTag => this.GetTable<Fs.PixSql.PixivTag>();
        public ITable<Fs.PixSql.PixivTagMap> PixTagMap => this.GetTable<Fs.PixSql.PixivTagMap>();
        public ITable<Fs.PixSql.PixLive> PixLive => this.GetTable<Fs.PixSql.PixLive>();


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

    public static class IntoSqlite
    {
        static TempTable<T> CreateTemp<T>(DataConnection db, string name) where T: class
        {

            db.DropTable<T>(tableName:name, tableOptions: TableOptions.DropIfExists);

            return db.CreateTempTable<T>(tableName: name);
        }

        static void Into<T>(IQueryable<T> query, DataConnection db, Action<TempTable<T>> action) where T : class
        {
            using var temp = CreateTemp<T>(db, "3828A8BD-85C0-4BFB-B41E-F9934D4013C4");


            temp.BulkCopy(query);


            action(temp);

        }

        static void InsertData(List<Fs.PixSql.PixivHtml> vs, DataConnection db)
        {
          
           
            using var temp = CreateTemp<Fs.PixSql.PixivData>(db, "A004C88C-0518-4A21-B8CC-6316C8523439");

            temp.BulkCopy(vs.Select(p => p.pix));

            var query = temp
                .LeftJoin(db.GetTable<Fs.PixSql.PixivData>(), (a, b) => a.Id == b.Id, (a, b) => new { a, b });

            Into(
                query.Where(p => p.b == null).Select(p => p.a),
                db,
                p => p.Insert(db.GetTable<Fs.PixSql.PixivData>(), p => p));

            Into(
                query.Where(p => p.b != null).Select(p => p.a),
                db,
                p =>
                {
                    p.InnerJoin(db.GetTable<Fs.PixSql.PixivData>(), (a, b) => a.Id == b.Id, (a, b) => a)
                    .Update(db.GetTable<Fs.PixSql.PixivData>(), p => p);


                });

            

        }
      
        static void InsertTag(List<Fs.PixSql.PixivHtml> vs, DataConnection db)
        {
            var dic = new Dictionary<string, int>();

            var ie = vs.SelectMany(p =>
            {
                return p.tag.Select(tag =>
                {
                    if (!dic.TryGetValue(tag, out var tagID))
                    {
                        tagID = StaticFunction.GetTagHash(tag);

                        dic[tag] = tagID;

                    }

                    return new Fs.PixSql.PixivTagMap(p.pix.Id, tagID);

                });
            });


            void InMap()
            {
                using var temp = CreateTemp<Fs.PixSql.PixivTagMap>(db, "18A7579E-A10A-4BC2-A698-F61BAEA36F7F");


                temp.BulkCopy(ie);

                var query = temp.LeftJoin(
                    db.GetTable<Fs.PixSql.PixivTagMap>(),
                    (a, b) => a.ItemId == b.ItemId && a.TagId == b.TagId,
                    (a, b) => new { a, b })
                    .Where(p => p.b == null)
                    .Select(p => p.a);



                Into(query, db, p => p.Insert(db.GetTable<Fs.PixSql.PixivTagMap>(), p => p));


            }

            void InTag()
            {
                using var temp = CreateTemp<Fs.PixSql.PixivTag>(db, "18A7579E-A10A-4BC2-A698-F61BAEA36F7F");


                temp.BulkCopy(dic.Select(p => new Fs.PixSql.PixivTag(p.Value, p.Key)));

                var query = temp.LeftJoin(
                    db.GetTable<Fs.PixSql.PixivTag>(),
                    (a, b) => a.Id == b.Id,
                    (a, b) => new { a, b })
                    .Where(p => p.b == null)
                    .Select(p => p.a);



                Into(query, db, p => p.Insert(db.GetTable<Fs.PixSql.PixivTag>(), p => p));


            }

            InMap();

            InTag();

        }

        static void Insert(List<Fs.PixSql.PixivHtml> vs, DataConnection db)
        {
            using var ts = db.BeginTransaction();

            InsertData(vs, db);

            InsertTag(vs, db);

            ts.Commit();
        }

        public static void Run(int count, Func<DataConnection> func, ChannelReader<Fs.PixSql.PixivHtml> reader)
        {
            var vs = new List<Fs.PixSql.PixivHtml>();

            while (true)
            {
                if (reader.TryRead(out var v))
                {
                    vs.Add(v);


                    if (vs.Count >= count)
                    {
                        Insert(vs, func());

                        vs = new List<Fs.PixSql.PixivHtml>();
                    }

                }
                else
                {
                    Thread.Sleep(new TimeSpan(0, 0, 5));
                }
            }
        }

       

    }

    public class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

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
            
            args = new string[] { "--urls=http://127.0.0.1:80/" };


            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.CancelKeyPress += (obj, e) => KillSelf();

            TaskScheduler.UnobservedTaskException += (obj, e) => Exit(e.Exception);

            Configuration.ContinueOnCapturedContext = false;
            Configuration.Linq.GuardGrouping = false;



            var host = CreateHostBuilder(args).Build();
            var db = host.Services.GetRequiredService<AppDataConnection>();

            db.CreateTable<Fs.PixSql.PixImg>(tableOptions:TableOptions.CreateIfNotExists);
            db.CreateTable<Fs.PixSql.PixivData>(tableOptions: TableOptions.CreateIfNotExists);
            db.CreateTable<Fs.PixSql.PixivTagMap>(tableOptions: TableOptions.CreateIfNotExists);
            db.CreateTable<Fs.PixSql.PixLive>(tableOptions: TableOptions.CreateIfNotExists);
            db.CreateTable<Fs.PixSql.PixivTag>(tableOptions: TableOptions.CreateIfNotExists);

            host.Start();

            var info = host.Services.GetAspPixInfo();

            var reader = AspPix.Fs.PixCrawling.run(() => host.Services.GetRequiredService<Fs.PixCrawling.PixGetHtmlService>(), info.BASEURI, info.REFERER.AbsoluteUri);


            FreeConsole();

            IntoSqlite.Run(1000, () => host.Services.GetRequiredService<AppDataConnection>(), reader);
            


        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();

                    

                    var path = Path.Combine(
                        hostingContext.Configuration.GetAspPixInfo().LOGS_PATH,
                        "myapp-{Date}.txt");


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

           
            services.AddLinqToDBContext<AppDataConnection>((provider, options) => {
                options
                .UseSQLiteMicrosoft(_configuration.GetAspPixInfo().CONNECT_STRING)
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


    public static class StaticFunction
    {
        public static string Base64Encode(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        public static string Base64Decode(string text)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(text));
        }

        public static int GetTagHash(string tag)
        {
            byte[] buff = Encoding.UTF8.GetBytes(tag);

            byte[] hash = SHA256.HashData(buff);

            return BitConverter.ToInt32(hash, 0);

        }
    }
}