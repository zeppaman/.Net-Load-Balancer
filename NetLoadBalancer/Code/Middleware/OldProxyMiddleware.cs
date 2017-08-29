using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NetLoadBalancer.Code.Extension;

namespace NetLoadBalancer.Code.Middleware
{
    public class OldProxyMiddleware
    {
       
            private readonly RequestDelegate _next;

            private ILogger<OldProxyMiddleware> logger;

            public OldProxyMiddleware(RequestDelegate next)
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
                if (!endRequest && _next!=null)
                {
                    await _next(context);
                }
            }

            private static async Task DownloadAsync(HttpContext context, string url)
            {


            var localResponse = context.Response;
            try
            {

                Uri uri = new Uri(url);
                var webRequest = (HttpWebRequest)WebRequest.Create(uri);// HttpWebRequest.CreateHttp(url);
                webRequest.Method = context.Request.Method;
                //webRequest.AllowAutoRedirect = true;
               // webRequest.AllowReadStreamBuffering = false;
                webRequest.Referer = webRequest.Referer;
                webRequest.CookieContainer = new CookieContainer();

                //webRequest.KeepAlive = false;
                //webRequest.PreAuthenticate = true;
                webRequest.Headers.Set("Pragma", "no-cache");



                foreach (var item in context.Request.Headers)
                {
                    webRequest.Headers.Add(item.Key, item.Value);
                }

                webRequest.Headers["X-Forwarded-For"] = GetIp();
                
                webRequest.Date = DateTime.Now;


                foreach (var item in context.Request.Cookies)
                {

                    var k = item.Key;
                    var v = item.Value;





                }




                //TODO: THIS IS THE BEST SOLUTION TO SERVE FILES DIRECTLY. KEEP TWO DIFFERENT FLOWS FOR THIS AND THE ONES THAT NEED FULL RESPOSNSE



                HttpWebResponse remoteResponse = (HttpWebResponse)webRequest.GetResponse();
                await DumpFromRequest( localResponse, remoteResponse);
            }
            catch (WebException e)
            {
                using (HttpWebResponse response = (HttpWebResponse)e.Response)
                {
                    await DumpFromRequest(localResponse, response);
                }
            }
        }

        private static async Task DumpFromRequest( HttpResponse localResponse, HttpWebResponse remoteResponse)
        {
            
            var buffer = new byte[4 * 1024];
            foreach (string key in remoteResponse.Headers.Keys)
            {

                localResponse.Headers[key] = remoteResponse.Headers[key];
            }


            CookieContainer cc = new CookieContainer();


            foreach (Cookie currentCookie in remoteResponse.Cookies)
            {
                cc.Add(currentCookie);
                var ci = currentCookie.ToString().FromCookieString();
                localResponse.Cookies.Append(currentCookie.Value, currentCookie.ToString());

            }

            localResponse.ContentType = remoteResponse.ContentType;
           // localResponse.ContentLength = remoteResponse.ContentLength;
            localResponse.StatusCode = (int)remoteResponse.StatusCode;

            
            using (var remoteStream = remoteResponse.GetResponseStream())
            {
                
                    int len = 0;
                long fullSize = 0 ;
                    
                try
                {
                    
                    while ((len = remoteStream.Read(buffer, 0, buffer.Length )) != 0)
                    {
                        
                         localResponse.Body.Write(buffer, 0, len);
                        fullSize += len;
                    }
                    localResponse.ContentLength =fullSize;
                }
                catch (NotSupportedException empty)
                {
                    
                }
            }
        }

        private static string GetIp()
        {
            return "127.0.0.1";
            //TODO: FIX ME
        }
    }
}
