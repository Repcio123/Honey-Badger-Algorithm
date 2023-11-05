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
using HoneyBagder.WebServer;

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

        static void Main(string[] args)
        {
            Server.Run();
        }
    }
}
