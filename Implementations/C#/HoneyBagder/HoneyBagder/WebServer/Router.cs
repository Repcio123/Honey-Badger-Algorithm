using HoneyBadger;
using HoneyBagder.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Web;
using System.IO;

namespace HoneyBagder.WebServer
{
    internal class WebServerRouter
    {
        public Dictionary<string, string> urlToFileMap = new () { };

        public void registerPublicDirectory(string directoryPath)
        {
            foreach (string osPath in Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories))
            {
                string url = osPath.Replace("\\", "/").Remove(0, directoryPath.Length);

                urlToFileMap.Add(url, osPath);

                if (url.EndsWith("/index.html"))
                {
                    string indexUrl = url.Replace("index.html", "");
                    urlToFileMap.Add(indexUrl, osPath);
                }
            }
        }

        static int? ToNullableInt(string? val)
            => int.TryParse(val, out var i) ? (int)i : null;
        
        static void PushAsJSON(Stream output, object message)
        {
            var jsonString = JsonSerializer.Serialize(message);
            var data = $"data: {jsonString}\r\r";
            var dataBytes = Encoding.UTF8.GetBytes(data);
            output.WriteAsync(dataBytes);
            output.FlushAsync();
        }

        public static void Debounced<T>(IEnumerable<T> generator, Action<T> onElement, int actionMilisecondInterval = 100)
        {
            bool isLastResultStale = false;
            DateTime debounce = DateTime.Now;
            T lastResult = default(T)!;
            foreach (T result in generator)
            {
                if (DateTime.Now > debounce)
                {
                    debounce = DateTime.Now.AddMilliseconds(actionMilisecondInterval);
                    isLastResultStale = false;
                    onElement(result);
                }
                lastResult = result;
                isLastResultStale = true;
            }
            if (lastResult != null && isLastResultStale)
            {
                onElement(lastResult);
            }
        }

        static async void RunAlgorithm(HttpListenerContext ctx)
        {
            using HttpListenerResponse resp = ctx.Response;
            resp.Headers.Set("Content-Type", "text/json");

            using Stream ros = resp.OutputStream;

            ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
            string err = "404 - not found";

            byte[] ebuf = Encoding.UTF8.GetBytes(err);
            resp.ContentLength64 = ebuf.Length;

            ros.Write(ebuf, 0, ebuf.Length);
        }

        static void NotFound(HttpListenerContext ctx)
        {
            using HttpListenerResponse resp = ctx.Response;
            resp.Headers.Set("Content-Type", "text/plain");

            using Stream ros = resp.OutputStream;

            ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
            string err = "404 - not found";

            byte[] ebuf = Encoding.UTF8.GetBytes(err);
            resp.ContentLength64 = ebuf.Length;

            ros.Write(ebuf, 0, ebuf.Length);
        }

        static string GetMimeTypeFromFileExtension(string extension)
        {
            switch (extension)
            {
                case ".html":
                    return "text/html";
                case ".js":
                    return "text/x-javascript";
                case ".css":
                    return "text/css";
                default:
                    return "text/unknown";
            }
        }

        static void ServeFile(HttpListenerContext ctx, string filePath)
        {
            using HttpListenerResponse resp = ctx.Response;
            string mimeType = GetMimeTypeFromFileExtension(Path.GetExtension(filePath));
            resp.Headers.Set("Content-Type", mimeType);

            byte[] buf = File.ReadAllBytes(filePath);
            resp.ContentLength64 = buf.Length;

            using Stream ros = resp.OutputStream;
            ros.Write(buf, 0, buf.Length);
        }

        public void handleRequest(HttpListenerContext ctx)
        {
            string? path = ctx.Request.Url?.LocalPath;

            if (ctx.Request.HttpMethod == "GET" && path != null && urlToFileMap.ContainsKey(path))
            {
                ServeFile(ctx, urlToFileMap[path]);
                return;
            }

            switch (path)
            {
                case "/run":
                    Thread t1 = new Thread(() => RunAlgorithm(ctx)) { Name = System.Guid.NewGuid().ToString() };
                    t1.Start();
                    break;
                default:
                    NotFound(ctx);
                    break;
            };
        }
    }
}
