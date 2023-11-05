using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder.WebServer
{
    internal class Server
    {
        public static void Run()
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8001/");

            listener.Start();

            Console.WriteLine("Listening on port 8001...");

            WebServerRouter router = new();
            router.registerPublicDirectory("tempFront");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                router.handleRequest(context);
            }
        }
    }
}
