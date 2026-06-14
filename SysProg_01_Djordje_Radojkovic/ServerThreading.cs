using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SysProg_01_Djordje_Radojkovic
{
    internal class ServerThreading
    {
        MyCache myCache = new MyCache();
        public ServerThreading() { }
        public async Task ProcessReq(object o)
        {
            //getting context
            HttpListenerContext context = o as HttpListenerContext;
            try
            {
                Console.WriteLine("In thread for request!");
                Stopwatch sw = Stopwatch.StartNew();

                //getting request
                HttpListenerRequest request = context.Request;
                Console.WriteLine(request.RawUrl);

                //creating response object
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html";

                //string fi = await myCache.GetFileAsync(request.RawUrl.Substring(1));
                string fi = string.Empty;
                Task<string> files = myCache.GetFileAsync(request.RawUrl.Substring(1));
                await files.ContinueWith(f =>
                {
                    if (f.Status == TaskStatus.RanToCompletion)
                    {
                        fi = f.Result;
                        if (fi == "File not found!" || fi == "File empty!")
                        {
                            context.Response.StatusCode = 404;
                        }
                        else if(fi == "Error!")
                        {
                            context.Response.StatusCode = 500;
                            fi = "An error has ocurred while retreving file!";
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        fi = "An error has ocurred while retreving file!";
                    }
                });
                byte[] buffer = Encoding.UTF8.GetBytes(fi);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                sw.Stop();
                context.Response.OutputStream.Close();
                Console.WriteLine(fi + "\n" + sw.ToString());

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                context.Response.Close();
            }
        }
    }
}
