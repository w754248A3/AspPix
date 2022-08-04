using LinqToDB;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AspPix
{
    public sealed class PixCrawling
    {
        private readonly HttpClient _http;

        private readonly ILogger _logger;

        public PixCrawling(PixGetHtmlHttp http, ILogger<PixCrawling> logger)
        {
            _http = http.Http;

            _logger = logger;
        }

        static async ValueTask Load(int notFoundCount, Func<int> nextid,
            Func<int, Task<string>> loadHtml,
            ChannelWriter<PixivHtml> writer)
        {
            try
            {
                await Task.Yield();

                int id = nextid();

                string html = await loadHtml(id).ConfigureAwait(false);

                await writer.WriteAsync(DataParse.CreatePH(html, id)).ConfigureAwait(false);

                await Load(0, nextid, loadHtml, writer).ConfigureAwait(false);
            }
            catch(HttpRequestException)
            {
                if (notFoundCount >= 1000)
                {
                    return;
                }
                else
                {
                    await Load(notFoundCount + 1, nextid, loadHtml, writer).ConfigureAwait(false);
                }
            }  
            catch (OperationCanceledException)
            {
                await Load(0, nextid, loadHtml, writer).ConfigureAwait(false);
            }
        }


        static async Task<int> GetMaxId(Func<Uri, string, Task<string>> loadHtml)
        {

            string html = await loadHtml(
                new Uri("http://www.pixiv.net/ranking.php"),
                "https://www.google.com/").ConfigureAwait(false);


            //100175386_p0_master1200.jpg
            Regex regex = new(@"/(\d+)_p0");


            return regex.Matches(html).Select(p => int.Parse(p.Groups[1].Value)).Max();

        }

        
        public ChannelReader<PixivHtml> Run()
        {
            var loadHtml = MyHttp.CreateLoadHtmlFunc(_http);

            var channel = Channel.CreateBounded<PixivHtml>(100);
            
            void RunLoopLoad(Func<int,int> nextId)
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        var id = await GetMaxId(loadHtml).ConfigureAwait(false);
                        Uri baseUri = new("http://www.pixiv.net/artworks/");
                        await Load(
                            0,
                            () => { id = nextId(id); return id; },
                            (n) => loadHtml(new Uri(baseUri, n.ToString()), "https://www.pixivision.net"),
                            channel.Writer).ConfigureAwait(false);
                    }                    
                });
            }

            RunLoopLoad((n) => n - 1);
            RunLoopLoad((n) => n + 1);


            return channel.Reader;
        }
    }
}