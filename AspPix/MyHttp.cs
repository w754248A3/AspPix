using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AspPix
{
    public static class MyHttp
    {
        public static HttpRequestMessage CreateHttpRequestMessage(Uri uri, string referer)
        {
            static void addHeaders(System.Net.Http.Headers.HttpHeaders headers, string referer)
            {
                headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");

                headers.Add("Accept-Language", "zh-CN,zh;q=0.9");

                headers.Add("Accept-Charset", "utf-8");

                headers.Add("Accept-Encoding", "gzip, deflate, br");

                headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.75 Safari/537.36");

                headers.Add("Referer", referer);
            }

            HttpRequestMessage request = new();

            request.Method = HttpMethod.Get;

            request.RequestUri = uri;

            request.Version = HttpVersion.Version20;

            request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;



            addHeaders(request.Headers, referer);

            return request;


        }

        public static SocketsHttpHandler CreateSocketsHttpHandler(string dnsName, int port, string sniName)
        {
            var v = new SocketsHttpHandler
            {
                UseProxy = false,
                AutomaticDecompression = DecompressionMethods.All,
                ConnectCallback = CreateConnectCallback(dnsName, port, sniName)
            };


            return v;
        }

        public static Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>>
            CreateConnectCallback(string dnsName, int port, string sniName)
        {


            return async (info, tokan) =>
            {

                Socket socket = new(SocketType.Stream, ProtocolType.Tcp);


                await socket.ConnectAsync(dnsName, port, tokan).ConfigureAwait(false);

                SslStream sslStream = new(new NetworkStream(socket, true), false);

                await sslStream.AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions
                    {
                        TargetHost = sniName,
                        ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 },

                    },
                    tokan).ConfigureAwait(false);




                return sslStream;
            };
        }


        public static Func<Uri, string, Task<string>> CreateLoadHtmlFunc(HttpMessageInvoker http)
        {
            static void CanThrow(HttpResponseMessage response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(string.Empty, null, response.StatusCode);
                }
            }

            return async (uri, referer) =>
            {
               
                HttpRequestMessage request = CreateHttpRequestMessage(uri, referer);

                HttpResponseMessage response = await http.SendAsync(
                    request,
                    new CancellationTokenSource(new TimeSpan(0, 0, 10)).Token).ConfigureAwait(false);

                CanThrow(response);

                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            };

            
        }
    }
}