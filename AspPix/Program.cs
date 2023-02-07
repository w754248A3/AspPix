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
using Microsoft.Win32.TaskScheduler;
using System.Linq;
using System.CommandLine;

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
        

        static System.Threading.Channels.ChannelWriter<PixImg> Writer { get; set; }

        public static System.Threading.Channels.ChannelReader<PixImg> Init()
        {

            var chann = System.Threading.Channels.Channel.CreateBounded<PixImg>(100);
            Writer = chann.Writer;


            return chann.Reader;
        }


        public void Post(PixImg pixImg)
        {
            
            Writer.TryWrite(pixImg);
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


        public static class Cla任务计划程序帮助类
        {

            public static string GetAppPath()
            {

                string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

                string basePath = AppDomain.CurrentDomain.BaseDirectory;

                string appNameWithExe = appName + ".exe";

                return Path.Combine(basePath, appNameWithExe);

            }

            public static void Remove任务计划程序(string taskName)
            {
                var ts = TaskService.Instance.RootFolder.GetTasks().Where(p => p.Name == taskName);


                foreach (var item in ts)
                {
                    item.Stop();
                }

                TaskService.Instance.RootFolder.DeleteTask(taskName, false);




            }
            public static void Start任务计划程序(string taskName, string args)
            {
                var ts = TaskService.Instance.RootFolder.GetTasks().Where(p => p.Name == taskName);


                foreach (var item in ts)
                {
                    item.Run(args);
                }
            }
            public static void Stop任务计划程序(string taskName)
            {
                var ts = TaskService.Instance.RootFolder.GetTasks().Where(p => p.Name == taskName);


                foreach (var item in ts)
                {
                    item.Stop();
                }
            }
            
            public static void Register任务计划程序并立即运行(string appPath, string args, string taskName, string v任务表述)
            {
                var td = TaskService.Instance.NewTask();


                td.RegistrationInfo.Description = v任务表述;

                td.Triggers.AddNew(TaskTriggerType.Logon);

                td.Principal.RunLevel = TaskRunLevel.LUA;

                td.Settings.DisallowStartIfOnBatteries = false;

                td.Settings.ExecutionTimeLimit = TimeSpan.Zero;

                td.Settings.MultipleInstances = TaskInstancesPolicy.StopExisting;

                td.Settings.StopIfGoingOnBatteries = false;

                td.Settings.RunOnlyIfIdle = false;

                td.Settings.IdleSettings.StopOnIdleEnd = false;

                td.Actions.Add(appPath, args);

                TaskService.Instance.RootFolder.RegisterTaskDefinition(taskName, td).Run(args);


            }

        }

        public enum CommandLineFlags
        {
            OnlyRun,
            Install,
            RunAndFree,
            Uninstall,
            Stop,
            Start
        }

        public static int Main(string[] args)
        {

            var option = new System.CommandLine.Option<CommandLineFlags>(
            name: "--args",
            description: "执行选项");

            var rootCommand = new RootCommand("AspPix");
            rootCommand.AddOption(option);



            rootCommand.SetHandler((flags) =>
            {
                const string TASKNAME = "AspPix_2be61d52-3276-4279-9a15-1879e65b73d2";
                const string START_ARGS = "--args RunAndFree";
                if (flags == CommandLineFlags.Install)
                {

                    Cla任务计划程序帮助类.Register任务计划程序并立即运行(
                        Cla任务计划程序帮助类.GetAppPath(),
                        START_ARGS,
                        TASKNAME,
                        "AspPix");


                }
                else if (flags == CommandLineFlags.OnlyRun)
                {

                    F启动(false);

                }
                else if (flags == CommandLineFlags.RunAndFree)
                {


                    F启动(true);

                }
                else if (flags == CommandLineFlags.Uninstall)
                {

                    Cla任务计划程序帮助类.Remove任务计划程序(TASKNAME);
                }
                else if (flags == CommandLineFlags.Stop)
                {

                    Cla任务计划程序帮助类.Stop任务计划程序(TASKNAME);
                }
                else if (flags == CommandLineFlags.Start)
                {

                    Cla任务计划程序帮助类.Start任务计划程序(TASKNAME, START_ARGS);
                }
            },
            option);

            return rootCommand.Invoke(args);
        }



        static void F启动(bool isFreeConsole)
        {
            //Xcopy /y /E $(ProjectDir)wwwroot $(OutDir)wwwroot

            var args = new string[] { "--urls=http://127.0.0.1:80/" };


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

            if(isFreeConsole){
                FreeConsole();
            }

            into.Run(1000, reader, InsertImgService.Init());

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