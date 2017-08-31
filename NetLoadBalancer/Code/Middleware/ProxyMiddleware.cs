using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NetLoadBalancer.Code.Classes;
using NetLoadBalancer.Code.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NetLoadBalancer.Code.Middleware
{

    


    public class ProxyMiddleware: FilterMiddleware
    {
        private const int DefaultBufferSize = 4096;

        private readonly RequestDelegate _next;
        private readonly HttpClient _httpClient;
        private readonly InternalProxyOptions _defaultOptions;

        private static readonly string[] NotForwardedWebSocketHeaders = new[] { "Connection", "Host", "Upgrade", "Sec-WebSocket-Key", "Sec-WebSocket-Version" };

        public override string Name => "Proxy";

       

        public ProxyMiddleware()
        {
            _defaultOptions = new InternalProxyOptions()
            {
                SendChunked = false

            };

            // if port is not specified default one is taken
            if (!_defaultOptions.Port.HasValue)
            {
                if (string.Equals(_defaultOptions.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                {
                    _defaultOptions.Port = 443;
                }
                else
                {
                    _defaultOptions.Port = 80;
                }

            }

            //if no scheme is choosen, http is taken
            if (string.IsNullOrEmpty(_defaultOptions.Scheme))
            {
                _defaultOptions.Scheme = "http";
            }

            _httpClient = new HttpClient(_defaultOptions.BackChannelMessageHandler ?? new HttpClientHandler());
        }

        /// <summary>
        /// Entry point. Switch betweeb websocket requests and regular http request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async override Task InvokeImpl(HttpContext context, string host, VHostOptions vhost, IConfigurationSection settings)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await HandleWebSocketRequest(context);
            }
            else
            {
                await HandleHttpRequest(context);
            }
        }

        private async Task HandleWebSocketRequest(HttpContext context)
        {


          

            var _options = (context.Items["proxy-options"] ?? _defaultOptions) as InternalProxyOptions;

            var destination = (context.Items["bal-destination"] ?? _defaultOptions) as Node;
            var host = (destination == null) ? _options.Host : destination.Host;
            var port = (destination == null) ? _options.Port : destination.Port;
            var scheme = (destination == null) ? _options.Scheme : destination.Scheme;


            using (var client = new ClientWebSocket())
            {
                foreach (var headerEntry in context.Request.Headers)
                {
                    if (!NotForwardedWebSocketHeaders.Contains(headerEntry.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        client.Options.SetRequestHeader(headerEntry.Key, headerEntry.Value);
                    }
                }

                var wsScheme = string.Equals(destination.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
                string url = GetUri(context, host, port, scheme);

                if (_options.WebSocketKeepAliveInterval.HasValue)
                {
                    client.Options.KeepAliveInterval = _options.WebSocketKeepAliveInterval.Value;
                }

                try
                {
                    await client.ConnectAsync(new Uri(url), context.RequestAborted);
                }
                catch (WebSocketException)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                using (var server = await context.WebSockets.AcceptWebSocketAsync(client.SubProtocol))
                {
                    await Task.WhenAll(PumpWebSocket(context,client, server, context.RequestAborted), PumpWebSocket(context,server, client, context.RequestAborted));
                }
            }
        }

        private async Task PumpWebSocket(HttpContext context, WebSocket source, WebSocket destination, CancellationToken cancellationToken)
        {
            var _options = (context.Items["proxy-options"] ?? _defaultOptions) as InternalProxyOptions;

            var buffer = new byte[_options.BufferSize ?? DefaultBufferSize];
            while (true)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, null, cancellationToken);
                    return;
                }
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await destination.CloseOutputAsync(source.CloseStatus.Value, source.CloseStatusDescription, cancellationToken);
                    return;
                }

                await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, cancellationToken);
            }
        }

        private async Task HandleHttpRequest(HttpContext context)
        {
            var _options = (context.Items["proxy-options"] ?? _defaultOptions) as InternalProxyOptions;

            var destination = (context.Items["bal-destination"] ?? _defaultOptions) as Node;
            var host = (destination == null) ? _options.Host : destination.Host;
            var port = (destination == null) ? _options.Port : destination.Port;
            var scheme = (destination == null) ? _options.Scheme : destination.Scheme;

          
                var requestMessage = new HttpRequestMessage();
                var requestMethod = context.Request.Method;

                if (!HttpMethods.IsGet(requestMethod) && !HttpMethods.IsHead(requestMethod) && !HttpMethods.IsDelete(requestMethod) && !HttpMethods.IsTrace(requestMethod))
                {
                    var streamContent = new StreamContent(context.Request.Body);
                    requestMessage.Content = streamContent;
                }

                // All request headers and cookies must be transferend to remote server. Some headers will be skipped
                foreach (var header in context.Request.Headers)
                {
                    if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                    {
                        requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                host = "www.vecchievie.it";
                requestMessage.Headers.Host = host;
                //recreate remote url
                string uriString = GetUri(context, host, port, scheme);
                requestMessage.RequestUri = new Uri(uriString);
                requestMessage.Method = new HttpMethod(context.Request.Method);
                using (var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
                {
                    context.Response.StatusCode = (int)responseMessage.StatusCode;
                    foreach (var header in responseMessage.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }

                    foreach (var header in responseMessage.Content.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }


                    if (!_options.SendChunked)
                    {
                        //tell to the browser that response is not chunked
                        context.Response.Headers.Remove("transfer-encoding");
                        await responseMessage.Content.CopyToAsync(context.Response.Body);
                    }
                    else
                    {
                        var buffer = new byte[_options.BufferSize ?? DefaultBufferSize];

                        using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
                        {
                            //long pos = responseStream.Position;
                            //if (pos > 0)
                            //{
                            //    responseStream.Seek(0, SeekOrigin.Begin);
                            //}
                            //context.Response.Body = new MemoryStream();

                            int len = 0;
                            int full = 0;
                            while ((len = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await context.Response.Body.WriteAsync(buffer, 0, buffer.Length);
                                // await context.Response.Body.FlushAsync();
                                full += buffer.Length;
                            }
                            // context.Response.ContentLength = full;

                            context.Response.Headers.Remove("transfer-encoding");
                        }
                    }
                }
           
        }

        private static string GetUri(HttpContext context, string host, int? port, string scheme)
        {
            var urlPort = "";
            if (port.HasValue 
                && !(port.Value==443  && "https".Equals(scheme,StringComparison.InvariantCultureIgnoreCase))
                && !(port.Value == 80 && "http".Equals(scheme, StringComparison.InvariantCultureIgnoreCase))
                )
            {
                urlPort = ":" + port.Value;
            }
            return $"{scheme}://{host}{urlPort}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        }

        public override bool Terminate(HttpContext httpContext)
        {
            return true;
        }

    }
}
