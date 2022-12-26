using LinqToDB;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
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

        async Task Load(IEnumerable<int> nextId,
            Func<int, Task<string>> loadHtml,
            ChannelWriter<PixivHtml> writer)
        {
            int notFoundCount = 0;
            foreach (var id in nextId)
            {
                string html = null;
                while (true)
                {
                   
                    try
                    {
                        html = await loadHtml(id).ConfigureAwait(false);

                        notFoundCount = 0;

                        break;
                    }
                    catch (HttpRequestException e)
                    when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        notFoundCount++;
                        if (notFoundCount >= 1000)
                        {
                            return;
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    when (e is HttpRequestException || e is OperationCanceledException)
                    {
                        await Task.Delay(new TimeSpan(0, 0, 3)).ConfigureAwait(false);
                    }            
                }

                if(html is not null)
                {
                    
                    try
                    {
                        _logger.LogError($"id:{id} load over");

                        var data = DataParse.CreatePD(html, id);
                        
                        await writer.WriteAsync(data).ConfigureAwait(false);
                    }
                    catch (UriFormatException)
                    {

                    }

                    
                }
                
            }
        }


        static async Task<int> GetMaxId(Func<Uri, string, Task<string>> loadHtml)
        {

            string html = null;

            int n = 0;
            do
            {
                try
                {
                    html = await loadHtml(
                                  new Uri("http://www.pixiv.net/ranking.php"),
                                  "https://www.google.com/").ConfigureAwait(false);


                }
                catch (Exception e) when  (e is HttpRequestException || e is OperationCanceledException) 
                {
                    if (n++ > 10)
                    {
                        throw;
                    }
                }
            } while (html is null);

          

            //100175386_p0_master1200.jpg
            Regex regex = new(@"/(\d+)_p0");


            return regex.Matches(html).Select(p => int.Parse(p.Groups[1].Value)).Max();

        }

        
        public ChannelReader<PixivHtml> Run(Uri baseUri, string referer)
        {
            static IEnumerable<int> CreateNextId(int start, Func<int ,int> next)
            {
                while (true)
                {
                    yield return start;

                    start = next(start);
                }
            }


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
                            CreateNextId(id, nextId),
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