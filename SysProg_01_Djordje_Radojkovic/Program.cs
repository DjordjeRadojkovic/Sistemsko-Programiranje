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
        
        public static void Main(String[] args)
        {
            try
            {
                ServerThreading st = new ServerThreading();
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:5050/");
                listener.Start();
                while (true)
                {
                    ThreadPool.QueueUserWorkItem(st.ProcessReq, listener.GetContext());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}