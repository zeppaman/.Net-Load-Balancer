using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NetLoadBalancer.Code.Middleware
{
    public class ProxyMiddleware
    {
       
            private readonly RequestDelegate _next;

            private ILogger<ProxyMiddleware> logger;

            public ProxyMiddleware(RequestDelegate next)
            {
              
                _next = next;
            }

            public async Task Invoke(HttpContext context)
            {
                var endRequest = false;
                object urlToProxy = null;
                if ( context.Items.TryGetValue("finalurl",out urlToProxy))
                {       
                        await DownloadAsync(context, urlToProxy as string);
                        endRequest = true;
                    
                }
                if (!endRequest)
                {
                    await _next(context);
                }
            }

            private static async Task DownloadAsync(HttpContext context, string url)
            {
                var httpClientHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = false
                };
                var webRequest = new HttpClient(httpClientHandler);

                var buffer = new byte[4 * 1024];

                //TODO: THIS IS THE BEST SOLUTION TO SERVE FILES DIRECTLY. KEEP TWO DIFFERENT FLOWS FOR THIS AND THE ONES THAT NEED FULL RESPOSNSE

                var localResponse = context.Response;
                try
                {
                    using (var remoteStream = await webRequest.GetStreamAsync(url))
                    {
                        var bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

                        localResponse.Clear();
                        localResponse.ContentType = "application/octet-stream";
                        var fileName = Path.GetFileName(url);
                        localResponse.Headers.Add("Content-Disposition", "attachment; filename=" + fileName);

                        if (remoteStream.Length != -1)
                            localResponse.ContentLength = remoteStream.Length;

                        while (bytesRead > 0) // && localResponse.IsClientConnected)
                        {
                            await localResponse.Body.WriteAsync(buffer, 0, bytesRead);
                            bytesRead = remoteStream.Read(buffer, 0, buffer.Length);
                        }


                    }
                }
                catch (Exception e)
                {
                   // logger.LogError(e, "Error during proxy reqest");
                }
            }
        
    }
}
