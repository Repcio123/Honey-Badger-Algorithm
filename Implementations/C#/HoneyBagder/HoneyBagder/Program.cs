using System;
using System.Security.Cryptography.X509Certificates;

namespace HoneyBadger 
{
    internal class Program
    {
        private const int dim = 7;
        private const int n = 7;
        static double[,] SetUpPosition(int n,
            int dim, double ub, double lb)
        {
            var x  = new double[dim,n];
            for (int i = 0; i < dim; i++)
            {
                for(int j = 0; j < n; j++)
                {
                    var rand = new Random();
                    double r1 = rand.NextDouble()*2-1;
                    x[i,j] = lb + r1 * (lb-ub);
                    
                }
                
            }
            return x;
        }
        static List<double> HoneyBadgerAlgorithm(int n,
            int tmax, double lb, double up, int dim, double c, double b)
        {

            return new List<double> { };
        }
        static void Main(string[] args)
        {
           var d =  SetUpPosition(n,dim,-3.5,3.5);
            for (int i = 0;i < dim;i++)
            {
                for(int j= 0; j < n; j++)
                {
                    Console.Write(d[i,j].ToString().Substring(0,5) + " ");
                }
                Console.WriteLine();
            }
        }
    }
}