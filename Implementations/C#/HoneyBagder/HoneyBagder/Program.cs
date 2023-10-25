using Microsoft.VisualBasic;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using static HoneyBadger.VectorEquations;

namespace HoneyBadger 
{
    internal class Program
    {
        private const int Dimensions = 7;
        private const int Population = 7;
        private static readonly double Epsilon = Math.Pow(2, -52); // smallest possible number in C#
        static private double[] GetRow(double[,] v, int idx)
        {
            return Enumerable.Range(0, v.GetLength(1)).Select(i => v[idx, i]).ToArray();
        }

        static private double[,] SetUpPosition(int population,
            System.Tuple<double, double>[] domain
            )
        {
            double[,] x  = new double[population, domain.Length];
            var rand = new Random();

            for (int i = 0; i < population; i++)
            {
                for(int j = 0; j < domain.Length; j++)
                {
                    (double lowerBound, double upperBound) = domain[j];
                    double r1 = rand.NextDouble() * 2 - 1;
                    x[i,j] = lowerBound + r1 * (upperBound - lowerBound);   
                }
            }
            return x;
        }

        static private double[,] HoneyBadgerAlgorithm(
            int population,
            int iterations,
            double c,
            double b,
            ObjectiveFunction fn
            )
        {
            int best_row_idx = 0;

            var random = new Random();
            var positions = SetUpPosition(population, fn.Domain);

            var population_futness_values = Enumerable.Range(0, population).Select(i => fn.Executor(GetRow(positions, i))).ToArray();
            var best_fitness_value = double.MaxValue;

            for (int i = 0; i < iterations; i++)
            {
                for (int population_index = 0; population_index < population - 1; population_index++)
                {
                    double a = c * Math.Exp(-i / iterations);

                    double flag = random.NextDouble() < 0.5 ? 1 : -1;
                    double[] best_row = GetRow(positions, best_row_idx);
                    double[] new_position;
                    
                    if (random.NextDouble() < 0.5)
                    {
                        // Exploration
                        double move_factor = Math.Abs(Math.Cos(2.0 * Math.PI * random.NextDouble()) * Math.Cos(1.0 - 2.0 * Math.PI * random.NextDouble()));
                        double move_intensity = flag * random.NextDouble() * a;
                        double[] current_row = GetRow(positions, population_index);
                        double[] next_row = GetRow(positions, population_index + 1);
                        double[] targetSmellIntensity = 
                            ScalarMultiply(Divide(
                                Power(Substract(current_row, next_row), 2),
                                ScalarAdd(ScalarMultiply(Power(Substract(best_row, current_row), 2), 4.0 * Math.PI), Epsilon)
                            ), random.NextDouble());

                        new_position = ScalarAdd(Add(
                            Add(best_row, Multiply(targetSmellIntensity, ScalarMultiply(current_row, b * flag))),
                            ScalarMultiply(Add(best_row, current_row), move_intensity)
                        ), move_factor);
                    }
                    else
                    {
                        // Exploitation
                        new_position = Add(best_row, ScalarMultiply(best_row, flag * random.NextDouble() * a));
                    }

                    double new_prey_value = fn.Executor(new_position);

                    if(new_prey_value < population_futness_values[population_index]) {
                        for (int j = 0; j < fn.Dimmentions; j++) {
                            positions[population_index, j] = new_position[j];
                        }
                        population_futness_values[population_index] = new_prey_value;

                        if (new_prey_value < best_fitness_value) {
                            best_fitness_value = new_prey_value;
                            best_row_idx = population_index;
                        }
                    }
                }
            }

            foreach (var x in GetRow(positions, best_row_idx))
            {
                Console.WriteLine(x);
            }
            return positions;

        }
        static void Main(string[] args)
        {
            Tuple<double, double>[] domain = { Tuple.Create(-4.5, 4.5), Tuple.Create(-4.5, 4.5) };
            Func<double[], double> executor = (double[] parameters) => {
                double x = parameters[0];
                double y = parameters[1];
                return Math.Pow(1.5 - x + x * y, 2) + Math.Pow(2.25 - x + x * y * y, 2) * Math.Pow(2.625 - x + x * y * y * y, 2);
            };
            ObjectiveFunction beatle = new ObjectiveFunction(domain, executor);
            HoneyBadgerAlgorithm(20, 30, 0.25, 0.5, beatle);
        }
    }
}
