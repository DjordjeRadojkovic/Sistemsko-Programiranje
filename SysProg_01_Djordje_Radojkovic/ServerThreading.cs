using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SysProg_01_Djordje_Radojkovic
{
    internal class ServerThreading
    {
        protected readonly ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>(Environment.ProcessorCount * 2, 5);
        protected readonly ConcurrentDictionary<string, DateTime> cacheTime = new ConcurrentDictionary<string, DateTime>(Environment.ProcessorCount*2, 5);
        public ServerThreading() { }
        public void ProcessReq(object o)
        {
            try
            {

                //getting context
                var context = o as HttpListenerContext;

                //getting request
                HttpListenerRequest request = context.Request as HttpListenerRequest;
                Console.WriteLine(request.RawUrl);

                //creating response object
                HttpListenerResponse response = context.Response;
                response.StatusCode = 200;
                response.ContentType = "text/html";
                Stream s = null;
                Stream output = response.OutputStream;

                //check cache
                string fi;
                if(cache.TryGetValue(request.RawUrl.Substring(1),out fi))
                {
                    Console.WriteLine($"READING FROM CACHE!");
                    response.ContentLength64 = fi.Length;
                    byte[] bytes = Encoding.UTF8.GetBytes(fi);
                    output.Write(bytes, 0, bytes.Length);
                    output.Close();
                    cacheTime[request.RawUrl.Substring(1)] = DateTime.Now;
                    Console.WriteLine(cacheTime[request.RawUrl.Substring(1)].ToString() + "\n");
                }
                else
                {
                    //not found in cache, checking memory
                    var files = Directory.EnumerateFiles("./", request.RawUrl.Substring(1), SearchOption.AllDirectories);

                    //checking if file exists
                    if(files.Count()>0)
                    {
                        //file exists
                        foreach (var file in files)
                        {
                            Console.WriteLine("NOT IN CACHE, READING FROM MEMORY!");
                            s = File.OpenRead(file);
                            if (s.Length > 0)
                            {
                                //if fine has content
                                string str;
                                using (StreamReader sr = new StreamReader(s))
                                {
                                    str = sr.ReadToEnd();
                                }
                                //sorting file content
                                string[] strs = str.Split(' ').OrderBy(x => x).ToArray();
                                str = String.Join(" ", strs);

                                //sending content back as response
                                response.ContentLength64 = str.Length;
                                byte[] bytes = Encoding.UTF8.GetBytes(str);
                                output.Write(bytes, 0, bytes.Length);
                                output.Close();

                                //checking if cache is full
                                if (cache.Count < 5)
                                {
                                    //cache not full
                                    //just add file to cache
                                    cache[request.RawUrl.Substring(1)] = str;
                                    cacheTime[request.RawUrl.Substring(1)] = DateTime.Now;
                                }
                                else
                                {
                                    //cache full
                                    //find oldest
                                    KeyValuePair<string, DateTime> oldest = cacheTime.OrderBy(x => x.Value).FirstOrDefault();

                                    //removing oldest from cache
                                    if (!cache.TryRemove(oldest.Key, out string cacheVal))
                                    {
                                        Console.WriteLine("nije izbrisan iz kesa! {0}", oldest.Key);
                                    }
                                    //adding new file to cache
                                    cache[request.RawUrl.Substring(1)] = str;

                                    //tracking time
                                    cacheTime.TryRemove(oldest.Key, out DateTime dt);
                                    cacheTime.TryAdd(request.RawUrl.Substring(1), DateTime.Now);
                                }
                                Console.WriteLine(cacheTime[request.RawUrl.Substring(1)].ToString() + "\n");
                            }
                            else
                            {
                                //file empty
                                Console.WriteLine("File is empty!");
                                response.StatusCode = 404;
                                string responseStr = "File empty!";
                                byte[] buffer = Encoding.UTF8.GetBytes(responseStr);
                                response.ContentLength64 = buffer.Length;
                                output.Write(buffer, 0, buffer.Length);
                                output.Close();
                            }


                        }
                    }
                    else
                    {
                        //file not found
                        response.StatusCode = 404;
                        string responseStr = "File not found!";
                        byte[] buffer = Encoding.UTF8.GetBytes(responseStr);
                        response.ContentLength64 = buffer.Length;
                        output.Write(buffer, 0, buffer.Length);
                        output.Close();
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
