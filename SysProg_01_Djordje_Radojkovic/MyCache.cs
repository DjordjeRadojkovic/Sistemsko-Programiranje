using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysProg_01_Djordje_Radojkovic
{
    internal class MyCache
    {
        protected readonly ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>(Environment.ProcessorCount * 2, 5);
        protected readonly ConcurrentDictionary<string, DateTime> cacheTime = new ConcurrentDictionary<string, DateTime>(Environment.ProcessorCount * 2, 5);
        protected readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public async Task<string> GetFileAsync(string key)
        {
            string fi;
            if (cache.TryGetValue(key, out fi))
            {
                Console.WriteLine($"READING FROM CACHE!");
                cacheTime[key] = DateTime.Now;
                Console.WriteLine(cacheTime[key].ToString());
                return fi;
            }

            Console.WriteLine("Cache miss!");

            var keylock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await keylock.WaitAsync();

            if (cache.TryGetValue(key, out fi))
            {
                Console.WriteLine($"READING FROM CACHE!");
                cacheTime[key] = DateTime.Now;
                Console.WriteLine(cacheTime[key].ToString());
                return fi;
            }

            //fi = await ReadFromFileAsync(key);
            Task<string> taskRead = ReadFromFileAsync(key);
            await taskRead.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    fi = taskRead.Result;
                    if(fi != "File not found!" && fi != "File empty!")
                    {
                        fi = ReOrderContent(fi);
                    }
                }
                else
                {
                    fi = "Error!";
                }
            });

            if (fi == "File not found!")
            {
                keylock.Release();
                return fi;
            }
            if (fi == "File empty!")
            {
                keylock.Release();
                return fi;
            }
            if (fi == "Error!")
            {
                keylock.Release();
                return fi;
            }

            if (cache.Count < 5)
            {
                //cache not full
                //just add file to cache
                cache[key] = fi;
                cacheTime[key] = DateTime.Now;
            }
            else
            {
                //cache full
                //find oldest
                KeyValuePair<string, DateTime> oldest = cacheTime.OrderBy(x => x.Value).FirstOrDefault();

                //removing oldest from cache
                if (!cache.TryRemove(oldest.Key, out string cacheVal))
                    Console.WriteLine("nije izbrisan iz kesa! {0}", oldest.Key);

                //adding new file to cache
                cache[key] = fi;

                //tracking time
                cacheTime.TryRemove(oldest.Key, out DateTime dt);
                cacheTime.TryAdd(key, DateTime.Now);
            }
            keylock.Release();
            Console.WriteLine(cacheTime[key].ToString());
            return fi;
        }

        public async Task<string> ReadFromFileAsync(string key)
        {
            string res = string.Empty;
            var files = Directory.EnumerateFiles("./", key, SearchOption.AllDirectories);
            if (files.Any())
            {
                FileStream fs = new FileStream(files.First(), FileMode.Open, FileAccess.Read);
                if (fs.Length > 0)
                {
                    using StreamReader streamReader = new StreamReader(fs);
                    res = await streamReader.ReadToEndAsync();
                    fs.Close();
                }
                else
                {
                    fs.Close();
                    return "File empty!";
                }
            }
            else
                return "File not found!";
            return res;
        }
        public string ReOrderContent(string content)
        {
            string[] strs = content.Split(' ').OrderBy(x => x).ToArray();
            content = String.Join(" ", strs);
            return content;
        }
    }
}
