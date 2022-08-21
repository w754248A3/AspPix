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
    public sealed class PixCrawlingService
    {
        readonly HttpClient _http;

        readonly ILogger _logger;

        public PixCrawlingService(PixGetHtmlHttp http, ILogger<PixCrawlingService> logger)
        {
            _http = http.Http;

            _logger = logger;
        }

        async ValueTask Load(int notFoundCount, Func<int> nextid,
            Func<int, Task<string>> loadHtml,
            ChannelWriter<PixivHtml> writer)
        {
            try
            {
                await Task.Yield();

                int id = nextid();

                string html = await loadHtml(id).ConfigureAwait(false);

                _logger.LogError($"id:{id} load over");

                await writer.WriteAsync(DataParse.CreatePD(html, id)).ConfigureAwait(false);

                await Load(0, nextid, loadHtml, writer).ConfigureAwait(false);
            }
            catch(HttpRequestException e)
            {
                _logger.LogError(e, "");

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

        
        public ChannelReader<PixivHtml> Run(Uri baseUri, string referer)
        {
            _logger.LogError("Craling Run");

            var loadHtml = MyHttp.CreateLoadHtmlFunc(_http);

            var channel = Channel.CreateBounded<PixivHtml>(1000);
            
            void RunLoopLoad(Func<int,int> nextId)
            {
                Task.Run(() => LogExit.OnErrorExitAsync(nameof(PixCrawlingService), _logger, async () =>
                {
                    while (true)
                    {
                        var id = await GetMaxId(loadHtml).ConfigureAwait(false);
                      
                        await Load(
                            0,
                            () => { id = nextId(id); return id; },
                            (n) => loadHtml(new Uri(baseUri, n.ToString()), referer),
                            channel.Writer).ConfigureAwait(false);

                        _logger.LogError("已完成一轮爬取");
                    }
                }));  
            }

            RunLoopLoad((n) => n - 1);
            RunLoopLoad((n) => n + 1);


            return channel.Reader;
        }
    }
}