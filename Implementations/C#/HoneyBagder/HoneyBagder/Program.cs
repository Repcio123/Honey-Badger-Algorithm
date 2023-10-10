using System;

namespace HoneyBadger 
{
    internal class Program
    {
        private const int Dimensions = 7;
        private const int Population = 7;
        private static readonly double Epsilon = Math.Pow(2, -52); // smallest possible number in C#

        static private double FitnessFunction(double x) => Math.Abs(x);


        static private double[,] SetUpPosition(int population,
            int dimensions,
            double upperBound,
            double lowerBound)
        {
            var x  = new double[dimensions, population];
            for (int i = 0; i < dimensions; i++)
            {
                for(int j = 0; j < population; j++)
                {
                    var rand = new Random();
                    double r1 = rand.NextDouble() * 2 - 1;
                    x[i,j] = lowerBound + r1 * (lowerBound-upperBound);   
                }
            }
            return x;
        }

        static private double[] CreateAgentsFitness(double[,] x, int population, int dimensions)
        {
            var f = new double[population];
            return f;
        }

        static private double[,] HoneyBadgerAlgorithm(int population,
            int iterations,
            double lowerBound,
            double upperBound,
            int dimensions,
            double c,
            double b)
        {
            var positions = SetUpPosition(population, dimensions,  upperBound, lowerBound);

            return positions;
        }
        static void Main(string[] args)
        {

        }
    }
}
