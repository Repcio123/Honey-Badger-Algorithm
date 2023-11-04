using Microsoft.VisualBasic;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static HoneyBadger.VectorEquations;
using HoneyBadger;
using HoneyBagder.DTO;
using HoneyBagder;
using System.Net;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HoneyBadger 
{
    internal class Program
    {
        static void Test()
        {
            Tuple<double, double>[] domain = { Tuple.Create(-4.5, 4.5), Tuple.Create(-4.5, 4.5) };
            Func<double[], double> bealeFunction = (double[] parameters) => {
                double x = parameters[0];
                double y = parameters[1];
                return Math.Pow(1.5 - x + x * y, 2) + Math.Pow(2.25 - x + x * y * y, 2) + Math.Pow(2.625 - x + x * y * y * y, 2);
            };
            ObjectiveFunction executor = new(domain, bealeFunction);
            for (var i = 5; i < 11; i++)
            {
                var result = Statistics.optimum(20 + 10 * i, 10 + 10 * i);
                Console.WriteLine($"beale/{result.bbest}/{result.cbest}/{result.iterations}/{result.population}/min{i}/{result.iterationParametersStandardDev}/{bealeFunction(result.parameters)}/{result.iterationValuesStandardDev}////min{i}/{String.Join("/", result.parameters)}".Replace(',', '.').Replace("/", ","));
            }
        }

        static int? ToNullableInt(string? val)
            => int.TryParse(val, out var i) ? (int)i : null;

        static void RunAlgorithm(HttpListenerContext ctx)
        {
            Tuple<double, double>[] domain = { Tuple.Create(-4.5, 4.5), Tuple.Create(-4.5, 4.5) };
            Func<double[], double> bealeFunction = (double[] parameters) => {
                double x = parameters[0];
                double y = parameters[1];
                return Math.Pow(1.5 - x + x * y, 2) + Math.Pow(2.25 - x + x * y * y, 2) + Math.Pow(2.625 - x + x * y * y * y, 2);
            };
            ObjectiveFunction executor = new(domain, bealeFunction);

            using HttpListenerResponse resp = ctx.Response;


            resp.Headers.Set("Cache-Control", "no-store");
            resp.Headers.Set("Content-Type", "text/event-stream");
            resp.Headers.Add("Connection", "keep-alive");


            var query = ctx.Request.QueryString;
            int? population = ToNullableInt(query.Get("population"));
            if (population == null)
            {
                return;
            }

            int? iterations = ToNullableInt(query.Get("population"));
            if (iterations == null)
            {
                return;
            }

            using (Stream output = resp.OutputStream)
            {
                foreach ( HoneyBadgerResultDTO result in HoneyBadgerAlgorithm.Generator((int)population, (int)iterations, 0.5, 0.5, executor))
                {
                    string jsonString = JsonSerializer.Serialize(result);
                    string data = $"data: {jsonString}\r\r";
                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                    output.WriteAsync(dataBytes);
                    output.FlushAsync();
                }
                string finalData = $"data: END-OF-STREAM\r\r";
                byte[] finalDataBytes = Encoding.UTF8.GetBytes(finalData);
                output.WriteAsync(finalDataBytes);
                output.FlushAsync();
            }
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

        static void TempFront(HttpListenerContext ctx)
        {
            using HttpListenerResponse resp = ctx.Response;
            resp.Headers.Set("Content-Type", "text/html");

            byte[] buf = File.ReadAllBytes("tempFront/index.html");
            resp.ContentLength64 = buf.Length;

            using Stream ros = resp.OutputStream;
            ros.Write(buf, 0, buf.Length);
        }

        static void Router(HttpListenerContext ctx)
        {
            string? path = ctx.Request.Url?.LocalPath;

            switch (path)
            {
                case "/run":
                    RunAlgorithm(ctx);
                    break;
                case "/":
                    TempFront(ctx); break;
                default:
                    NotFound(ctx);
                    break;
            };
        }

        static void Main(string[] args)
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8001/");

            listener.Start();

            Console.WriteLine("Listening on port 8001...");


            while (true)
            {
                Router(listener.GetContext()); //g
            }
        }
    }
}
