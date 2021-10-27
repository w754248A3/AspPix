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
using Pixiv2 = AspPix.PixCaling.Pixiv2;
using PixivTag = AspPix.PixCaling.PixivTag;
using PixivTagHas = AspPix.PixCaling.PixivTagHas;

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

           var a = PixCaling.CreatePixGetFunc("s.pximg.net", "http://i.pximg.net/", "https://www.pixiv.net/");

            //"https://morning-bird-d5a7.sparkling-night-bc75.workers.dev/"


            var b  = PixCaling.CreatePixGetFunc("morning-bird-d5a7.sparkling-night-bc75.workers.dev",
                "http://morning-bird-d5a7.sparkling-night-bc75.workers.dev/",
                null);



            GetImg = async (s, s2) =>
            {

                IEnumerable<Task<HttpResponseMessage>> cf()
                {
                    yield return b(s);
                    yield return b(s2);
                    yield return a(s);
                    yield return a(s2);
                }

                foreach (var item in cf())
                {
                    var res = await item.ConfigureAwait(false);
                    
                    if (res.IsSuccessStatusCode)
                    {
                        
                        return await res.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    }
                    else
                    {
                       
                    }
                }

                throw new HttpRequestException();
            };



            Tags = CreateTags();

        }



        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }



    public static class PixCaling
    {


        public class Pixiv2
        {
            [PrimaryKey]
            public int Id { get; set; }

            public int Mark { get; set; }

            public DateTime Date { get; set; }

            public byte ImgEN { get; set; }
        }

        public class PixivTag
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string Tag { get; set; }
        }

        public class PixivTagHas
        {
            [PrimaryKey]
            public int ItemId { get; set; }

            [PrimaryKey]
            public int TagId { get; set; }
        }


        public class PixivOffset
        {
            [PrimaryKey]
            public int Index { get; set; }


            public int Offset { get; set; }
        }

        public static string AsUriFromDateTimeIdSmall(Pixiv2 p, bool other)
        {
            //_ugoira0.jpg
           
            static string As(int y, int m, int d, int h, int mi, int se)
            {
                static string Add(int n)
                {
                    string s = n.ToString();

                    if (s.Length == 1)
                    {
                        return "0" + s;
                    }
                    else
                    {
                        return s;
                    }
                }


                return string.Join(
                        "/",
                        Add(y),
                        Add(m),
                        Add(d),
                        Add(h),
                        Add(mi),
                        Add(se));

            }

            var d = p.Date;

            var id = p.Id;

            string ds = As(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second);
            ///img-original/img/2015/02/07/19/16/23/48609048_ugoira0.jpg
            ///c/540x540_70/img-master/img/2015/02/07/19/16/23/48609048_master1200.jpg
            ///c/540x540_70/img-master/img/2020/03/30/08/40/00/80446655_p0_master1200.jpg
            if (other)
            {
                return $"/c/540x540_70/img-master/img/{ds}/{id}_master1200.jpg";
            }
            else
            {
                return $"/c/540x540_70/img-master/img/{ds}/{id}_p0_master1200.jpg";
                
            }       
        }


        public static string AsUriFromDateTimeId(Pixiv2 p)
        {
            ///c/540x540_70/img-master/img/2020/03/30/08/40/00/80446655_p0_master1200.jpg
            static string As(int y, int m, int d, int h, int mi, int se)
            {
                static string Add(int n)
                {
                    string s = n.ToString();

                    if (s.Length == 1)
                    {
                        return "0" + s;
                    }
                    else
                    {
                        return s;
                    }
                }


                return string.Join(
                        "/",
                        Add(y),
                        Add(m),
                        Add(d),
                        Add(h),
                        Add(mi),
                        Add(se));

            }


            DateTime d = p.Date;
            int id = p.Id;
            int imgEn = p.ImgEN;

            string ds = As(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second);

            return $"/img-original/img/{ds}/{id}_p0" + (imgEn == 0 ? ".jpg" : ".png");
        }


        static DateTime AsDateTimeFromUri(string s)
        {
            static int As(string s)
            {
                return int.Parse(s.Trim('/'));
            }

            var vs = new Uri(s).Segments;

            var y = As(vs[3]);

            var m = As(vs[4]);

            var d = As(vs[5]);


            var h = As(vs[6]);

            var mi = As(vs[7]);

            var se = As(vs[8]);

            return new DateTime(y, m, d, h, mi, se);
        }


        public static HttpMessageHandler GetHttpMessageHandler(string dns_sni)
        {
            const int TCP_PORT = 443;

            var handler = new SocketsHttpHandler()
            {

                ConnectTimeout = new TimeSpan(0, 0, 5),
                
                KeepAlivePingTimeout = new TimeSpan(0, 0, 5),
                
                KeepAlivePingDelay = new TimeSpan(0, 0, 5),
                
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                
                ResponseDrainTimeout = new TimeSpan(0, 0, 5),
                
                PooledConnectionLifetime = new TimeSpan(0, 1, 0),
                
                AutomaticDecompression = DecompressionMethods.All,
                
                EnableMultipleHttp2Connections = true,
                
                MaxConnectionsPerServer = 6,
                
                UseProxy = false,


                ConnectCallback = async (info, token) =>
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    await socket.ConnectAsync(dns_sni, TCP_PORT, token).ConfigureAwait(false);

                    var stream = new NetworkStream(socket, true);

                    var sslstream = new SslStream(stream, false);



                    await sslstream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        //sni字符串
                        TargetHost = dns_sni,

                        //tls指定后续的应用层协议
                        ApplicationProtocols = new List<SslApplicationProtocol>(new SslApplicationProtocol[] { SslApplicationProtocol.Http2 })

                    }, token).ConfigureAwait(false);


                    return sslstream;
                }
            };

            return handler;
        }

        public static Func<string, HttpRequestMessage> CreateHttpRequestMessage(Uri baseUri, string referrer)
        {
            static Action<System.Net.Http.Headers.HttpHeaders> CreateAddHeader(string referer)
            {
                static void AddHeadersWithReferer(System.Net.Http.Headers.HttpHeaders headers, string referer)
                {
                    headers.Add("Referer", referer);

                    AddHeader(headers);
                }

                static void AddHeader(System.Net.Http.Headers.HttpHeaders headers)
                {
                    headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");

                    headers.Add("Accept-Language", "zh-CN,zh;q=0.9");

                    headers.Add("Accept-Charset", "utf-8");

                    headers.Add("Accept-Encoding", "gzip, deflate, br");

                    headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.75 Safari/537.36");
                }


                if (referer is not null)
                {
                    return (header) => AddHeadersWithReferer(header, referer);
                }
                else
                {
                    return AddHeader;
                }
            }

            var headersadd = CreateAddHeader(referrer);


            return (s) => {

                var request = new HttpRequestMessage();


                request.Method = HttpMethod.Get;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                request.Version = HttpVersion.Version20;

                request.RequestUri = new Uri(baseUri, s);

                headersadd(request.Headers);

                return request;
            };
        

        }


        public static Func<string, Task<HttpResponseMessage>> CreatePixGetFunc(string dns_sni, string base_uri, string referer)
        {
            var handler = GetHttpMessageHandler(dns_sni);
            

            var http = new HttpMessageInvoker(handler);

            var request = CreateHttpRequestMessage(new Uri(base_uri), referer);

            return (s) => http.SendAsync(request(s), default);
        }

      


        static readonly Regex re_original = new Regex(@"""original"":""([^""]+)""");


        static readonly Regex re_mark = new Regex(@"""bookmarkCount"":(\d+),");


        static readonly Regex re_tags = new Regex(@"""tags"":(\[\{[^\]]+\}\]),");


        static int GetMarkFromHtml(string s)
        {
            return int.Parse(re_mark.Match(s).Groups[1].Value);
        }

        static string[] GetTagsFromHtml(string s)
        {
            var m = re_tags.Match(s);

            if (!m.Success)
            {
                return new string[] { };
            }
            else
            {
                var t = m.Groups[1].Value;

                try
                {

                    using var js = JsonDocument.Parse(t);

                    return js.RootElement.EnumerateArray().Select(p => p.GetProperty("tag").GetString()).ToArray();
                }
                catch (JsonException)
                {
                    Info.LogLine(t);

                    throw;
                }
            }
        }



        static void GetDateTimeAndENFromHtml(string s, out DateTime d, out byte b)
        {
            var uri = re_original.Match(s).Groups[1].Value;

            d = AsDateTimeFromUri(uri);

            b = (byte)(uri.EndsWith(".jpg") ? 0 : 1);

        }


        public static Func<string, int> CreateGetHashCode()
        {
            byte[] input = new byte[2048];

            return (s) => {

                int n = Encoding.UTF8.GetBytes(s, input);

                Span<byte> output = stackalloc byte[32];

                SHA256.TryHashData(input.AsSpan(0, n), output, out int size);

                return BitConverter.ToInt32(output);
            };
        }

        public static string WriteDBMessage { get; set; }

        static void WriteDB(Func<DataConnection> func, ChannelReader<CalingTmep> reader)
        {
            static void WritePixTagHas(DataConnection db, IEnumerable<IEnumerable<PixivTagHas>> pixivs)
            {
                foreach (var ie in pixivs)
                {
                    foreach (var item in ie)
                    {
                        try
                        {
                            db.Insert(item);
                        }
                        catch (MySql.Data.MySqlClient.MySqlException e)
                        {
                            if (e.Message.StartsWith("Duplicate entry"))
                            {
                                //Console.WriteLine(e.Message);
                            }
                            else
                            {
                                throw;
                            }

                        }
                    }                            
                }
            }

            static void WritePixiv2(DataConnection db, List<Pixiv2> pixivs)
            {
                try
                {

                    db.BulkCopy(pixivs);

                    return;
                }
                catch(MySql.Data.MySqlClient.MySqlException e)
                {
                    if (e.Message.StartsWith("Duplicate entry"))
                    {
                        //Console.WriteLine(e.Message);
                    }
                    else
                    {
                        throw;
                    }
                }


                foreach (var item in pixivs)
                {
                    db.InsertOrReplace(item);
                }
            }

            static void WritePixTag(DataConnection db, IEnumerable<PixivTag> tags)
            {
                foreach (var item in tags)
                {
                    try
                    {
                        db.Insert(item);
                    }
                    catch (MySql.Data.MySqlClient.MySqlException e)
                    {
                        if (e.Message.StartsWith("Duplicate entry"))
                        {
                            //Console.WriteLine(e.Message);
                        }
                        else
                        {
                            throw;
                        }

                    }
                }
            }

            const int COUNT = ConstValue.BU_COUNT;

            var hash = CreateGetHashCode();

            var pixs = new List<Pixiv2>(COUNT);

            var id_tags_s = new List<(int, string[])>(COUNT);

            var dic = new Dictionary<string, int>();

            foreach (var _ in Enumerable.Range(0, COUNT))
            {
                CalingTmep item;
              
                while (!reader.TryRead(out item))
                {
                    Thread.Sleep(new TimeSpan(0, 0, 3));
                }
                
                pixs.Add(new Pixiv2 { Id = item.id, Mark = item.mark, Date = item.d, ImgEN = item.b });

                id_tags_s.Add((item.id, item.tags));

                Array.ForEach(item.tags, p => dic[p] = hash(p));
            }

            WriteDBMessage = "开始写入数据库";

            var map = id_tags_s.Select(item => item.Item2.Select(p => new PixivTagHas { ItemId = item.Item1, TagId = dic[p] }));

            using var db = func();


            WritePixTag(db, dic.Select(p => new PixivTag { Tag = p.Key, Id = p.Value }));

            WritePixTagHas(db, map);
          
            WritePixiv2(db, pixs);


            //db.InsertOrReplace(new PixivOffset { Index = 0, Offset = pixs.Last().Id + 1 });


            WriteDBMessage = $"写入数据库完成{pixs.Last().Id}";
        }

        static void StartWriteDB(Func<DataConnection> func, ChannelReader<CalingTmep> reader)
        {

            var th = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        WriteDB(func, reader);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);

                    Environment.Exit(0);
                }

            });

            th.Start();     
        }

        static async Task Catch(Func<Task> func)
        {
            try
            {
                await func().ConfigureAwait(false);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);

                Environment.Exit(0);
            }
        }


        static Task<int> GetDateTimeId(DateTime desDT)
        {


            static Func<int, Task<DateTime>> CreateGetDateTimeFunc()
            {
                var http = CreatePixGetFunc("www.pixivision.net", "http://www.pixiv.net/artworks/", null);

                return async (n) =>
                {

                    foreach (var item in Enumerable.Range(0, 6))
                    {
                        try
                        {
                            var id = n + item;

                            //Console.WriteLine(id);


                            var res = await http(id.ToString()).ConfigureAwait(false);

                            if (res.IsSuccessStatusCode)
                            {
                                var s = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                                GetDateTimeAndENFromHtml(s, out var d, out _);

                                //Console.WriteLine($"{d} {id}");

                                return d;
                            }
                        }
                        catch (TaskCanceledException)
                        {

                        }
                        catch (HttpRequestException)
                        {

                        }
                    }

                    return DateTime.Now.AddDays(1);
                };
            }

            static async Task<int> GetDateTimeId(Func<int, Task<DateTime>> func, int min, int max, DateTime desDT)
            {
                if (min >= max)
                {
                    return -1;
                }

                var n = min + ((max - min) / 2);

                var vd = await func(n).ConfigureAwait(false);

                vd = new DateTime(vd.Year, vd.Month, vd.Day);

                if (vd < desDT)
                {
                    min = n;

                    return await GetDateTimeId(func, min, max, desDT).ConfigureAwait(false);
                }
                else if (vd > desDT)
                {
                    max = n;

                    return await GetDateTimeId(func, min, max, desDT).ConfigureAwait(false);
                }
                else
                {
                    return n;
                }
            }


            static async Task<(int min, int max)> GetMaxId(Func<int, Task<DateTime>> func, int min, int span, DateTime desDT)
            {
                var vd = await func(min).ConfigureAwait(false);

                vd = new DateTime(vd.Year, vd.Month, vd.Day);

                if (vd < desDT)
                {
                    span = span * 2;

                    return await GetMaxId(func, min + span, span, desDT).ConfigureAwait(false);
                }
                else
                {
                    return (min - span, min);
                }
            }

            const int MIN = 80000000;
            const int SPAN = 1000;



            static async Task<int> Get(DateTime desDT)
            {
                desDT = new DateTime(desDT.Year, desDT.Month, desDT.Day);

                var func = CreateGetDateTimeFunc();

                var (min, max) = await GetMaxId(func, MIN, SPAN, desDT).ConfigureAwait(false);


                return await GetDateTimeId(CreateGetDateTimeFunc(), min, max, desDT);
            }

            return Get(desDT);
        }



        static async Task CalingHtml(ChannelWriter<CalingTmep> writer, int id, Action<string> message, Func<int, bool> func)
        {

            var http = CreatePixGetFunc("www.pixivision.net", "http://www.pixiv.net/artworks/", null);

           
            int err = 0;

            while (func(id))
            {
                try
                {

                    message($"开始爬取 {id}");

                    var response = await http(id.ToString()).ConfigureAwait(false);

                    
                    if (response.IsSuccessStatusCode)
                    {
                        var s = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        message($"已获取HTML {id}");

                        var mark = GetMarkFromHtml(s);

                        var tags = GetTagsFromHtml(s);

                        //Console.WriteLine(string.Join(' ', tags));

                        DateTime d;
                        byte b;

                        GetDateTimeAndENFromHtml(s, out d, out b);

                        await writer.WriteAsync(new CalingTmep(id, mark, tags, d, b)).ConfigureAwait(false);


                        //Console.WriteLine(item);

                        message($"完成一个 {d} {id}");



                        err = 0;
                    }
                    else
                    {

                        if (err++ > 100)
                        {
                            return;
                        }


                        message($"{(int)response.StatusCode}HTML {id}");
                    }

                    id++;
                }
                catch (TaskCanceledException)
                {
                    message($"Cancel HTML {id}");
                }
                catch (HttpRequestException e)
                {
                    message($"{e.Message} HTML {id}");
                }
            }
        }

        public static string LoopMessage { get; set; }

        public static string CM1 { get; set; }
        public static string CM2 { get; set; }

        static async Task StartCalingHtml2(Func<DataConnection> func, ChannelWriter<CalingTmep> writer)
        {
            static int GetOffsetId(Func<DataConnection> func)
            {
                using var db = func();
                return (db.GetTable<PixivOffset>().FirstOrDefault() ?? new PixivOffset { Offset = 80176039 }).Offset;
            }

            const int SPAN = 1000;
            
            while (true)
            {
                var id = GetOffsetId(func) - (SPAN * 4);

                await CalingHtml(writer, id, (s) => CM2 = s, (n) => n <= (id + SPAN));

                using var db = func();
            
                db.InsertOrReplace(new PixivOffset { Index = 0, Offset = (id + SPAN) });
            }


        }

        static async Task StartCalingHtml(ChannelWriter<CalingTmep> writer)
        {
            while (true)
            {
                DateTime d = DateTime.Now.AddDays(-30);

                int n = await GetDateTimeId(d).ConfigureAwait(false);

                //int n = 90000000;

                LoopMessage = $"start {d} {n}";
                await CalingHtml(writer, n, (s) => CM1 = s, (b) => true).ConfigureAwait(false);
                LoopMessage = $"end {d} {n}";
            }
        }

        static async Task MessageLog()
        {
            while (true)
            {
                await Task.Delay(new TimeSpan(0, 0, 3)).ConfigureAwait(false);

                Info.LogLine($"{WriteDBMessage} {CM1} {CM2} {LoopMessage}");
            }
        }

        

        public static void Start(Func<DataConnection> func)
        {
            
            var chn = Channel.CreateBounded<CalingTmep>(ConstValue.BU_LOAD_COUNT);

            Task.Run(() => MessageLog());

            StartWriteDB(func, chn);

            Task.Run(() => Catch(() => StartCalingHtml(chn)));
            Task.Run(() => Catch(() => StartCalingHtml2(func, chn)));          
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

            Info.Init();

            PixCaling.Start(Info.DbCreateFunc);
          
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

    internal struct CalingTmep
    {
        public int id;
        public int mark;
        public string[] tags;
        public DateTime d;
        public byte b;

        public CalingTmep(int id, int mark, string[] tags, DateTime d, byte b)
        {
            this.id = id;
            this.mark = mark;
            this.tags = tags;
            this.d = d;
            this.b = b;
        }

        public void Deconstruct(out int id, out int mark, out string[] tags, out DateTime d, out byte b)
        {
            id = this.id;
            mark = this.mark;
            tags = this.tags;
            d = this.d;
            b = this.b;
        }
    }
}