/*
    This file is part of NetLoadBalancer, Daniele Fontani (https://github.com/zeppaman/.Net-Load-Balancer).

    NetLoadBalancer is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    NetLoadBalancer is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Nome-Programma.  If not, see <http://www.gnu.org/licenses/>.
*/
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
    

    /// <summary>
    /// this module serve content basing on input coming from previous steps
    /// </summary>
    public class ProxyMiddleware: FilterMiddleware
    {
        private const int DefaultBufferSize = 4096;

        private readonly RequestDelegate _next;
        private readonly HttpClient _httpClient;
        private readonly InternalProxyOptions _defaultOptions;

        private static readonly string[] NotForwardedWebSocketHeaders = new[] { "Connection", "Host", "Upgrade", "Sec-WebSocket-Key", "Sec-WebSocket-Version" };

        public override string Name => "Proxy";

       
        /// <summary>
        /// default init
        /// </summary>
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
        /// Entry point. Switch between websocket requests and regular http request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async override Task InvokeImpl(HttpContext context, string host, VHostOptions vhost, IConfigurationSection settings)
        {


            var _options = (context.Items["proxy-options"] ?? _defaultOptions) as InternalProxyOptions;

            var destination = (context.Items["bal-destination"]) as Node;
            if (destination == null)
            {     
                 destination = settings.GetSection("Settings:Proxy:DefaultDestination").Get<Node>();
            }
            var chost = (destination == null) ? _options.Host : destination.Host;
            var cport = (destination == null) ? _options.Port : destination.Port;
            var scheme = (destination == null) ? _options.Scheme : destination.Scheme;

            if (context.WebSockets.IsWebSocketRequest)
            {
                await HandleWebSocketRequest(context,  _options,  destination,  chost,  cport.Value,  scheme);
            }
            else
            {
                await HandleHttpRequest(context, _options, destination, chost, cport.Value, scheme);
            }
        }

        /// <summary>
        /// Handle also Web socket, calling pump method inside
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_options"></param>
        /// <param name="destination"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="scheme"></param>
        /// <returns></returns>
        private async Task HandleWebSocketRequest(HttpContext context, InternalProxyOptions _options, Node destination, string host, int port, string scheme)
        {
            
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
                    await Task.WhenAll(PumpWebSocket(context,client, server, _options, context.RequestAborted), PumpWebSocket(context,server, client, _options, context.RequestAborted));
                }
            }
        }
        
        /// <summary>
        /// Core pump method
        /// </summary>
        /// <param name="context"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="_options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task PumpWebSocket(HttpContext context, WebSocket source, WebSocket destination, InternalProxyOptions _options, CancellationToken cancellationToken)
        {
          

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

        /// <summary>
        /// Handle a simple http request dumping remote content to the client
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_options"></param>
        /// <param name="destination"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="scheme"></param>
        /// <returns></returns>
        private async Task HandleHttpRequest(HttpContext context, InternalProxyOptions _options, Node destination, string host, int port, string scheme)
        {
         

          
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

        /// <summary>
        /// compute uri of remote request basing on context
        /// </summary>
        /// <param name="context"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="scheme"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Tell to terminate the pipeline
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public override bool Terminate(HttpContext httpContext)
        {
            return true;
        }

    }
}
