using SysProg_01_Djordje_Radojkovic;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
namespace SysProgProj1
{
    class Program
    {
        public static async Task Main(String[] args)
        {
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource())
                {
                    Task serverTask = Task.Run(async () => await ListenerTask(cts.Token));

                    await Console.In.ReadLineAsync();

                    cts.Cancel();

                    await serverTask;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public static async Task ListenerTask(CancellationToken ct)
        {
            ServerThreading st = new ServerThreading();
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5050/");
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (!listener.IsListening)
                    listener.Stop();
            }
            try
            {
                ct.Register(() =>
                {
                    listener.Stop();
                    listener.Close();
                });
                while (!ct.IsCancellationRequested && listener.IsListening)
                {
                    var context = await listener.GetContextAsync();
                    Task listenerTask = Task.Run(() => { st.ProcessReq(context); });
                }
            }
            catch (HttpListenerException hlex)
            {
                Console.WriteLine("Shutting down!");
            }
        }
    }
}