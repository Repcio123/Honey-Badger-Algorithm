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

        static private double[][] SetUpPosition(int population,
            System.Tuple<double, double>[] domain
            )
        {
            double[][] x  = new double[population][];
            var rand = new Random();

            for (int i = 0; i < population; i++)
            {
                double[] k = new double[domain.Length];
                for(int j = 0; j < domain.Length; j++)
                {
                    (double lowerBound, double upperBound) = domain[j];
                    k[j] = lowerBound + rand.NextDouble() * (upperBound - lowerBound);   
                }
                x[i] = (double[])k.Clone()!;
            }
            return x;
        }

        static private double HoneyBadgerAlgorithm(
            int population,
            int iterations,
            double c,
            double b,
            ObjectiveFunction fn,
            bool log
            )
        {

            var random = new Random();
            var positions = SetUpPosition(population, fn.Domain);

            var population_futness_values = Enumerable.Range(0, population).Select(i => fn.Executor(positions[i])).ToArray();
            int best_row_idx = 0;
            for (int i = 1; i < population; i++)
            {
                if (population_futness_values[i] < population_futness_values[best_row_idx])
                {
                    best_row_idx = i;
                }
            }
            var best_fitness_value = population_futness_values[best_row_idx];

            for (int i = 0; i < iterations; i++)
            {
                for (int population_index = 0; population_index < population - 1; population_index++)
                {
                    double a = c * Math.Exp(-i / iterations);

                    double flag = random.NextDouble() < 0.5 ? 1 : -1;
                    double[] new_position;
                    if (random.NextDouble() < 0.5)
                    {
                        // Exploration
                        double move_factor = Math.Abs(
                            Math.Cos(
                                2.0 * Math.PI * random.NextDouble()
                            ) * (
                                Math.Cos(
                                    1 - 2.0 * Math.PI * random.NextDouble()
                                )
                            )
                        );
                        double move_intensity = flag * random.NextDouble() * a;
                        double[] targetSmellIntensity = 
                        ScalarMultiply(
                            Divide(
                                Power(
                                    Substract(
                                        positions[population_index],
                                        positions[population_index + 1]
                                    ),
                                    2
                                ),
                                ScalarMultiply(
                                    ScalarAdd(
                                        Power(
                                            Substract(
                                                positions[best_row_idx],
                                                positions[population_index]
                                            ),
                                            2
                                        ),
                                    Epsilon
                                    ),
                                4.0 * Math.PI
                                )
                            ),
                            random.NextDouble()
                        );

                        new_position =
                            Add(
                                positions[best_row_idx],
                                Add(
                                    Multiply(
                                        targetSmellIntensity,
                                        ScalarMultiply(
                                            positions[best_row_idx],
                                            b * flag
                                        )
                                    ),
                                    ScalarAdd(
                                        ScalarMultiply(
                                            Substract(
                                                positions[best_row_idx],
                                                positions[population_index]
                                            ),
                                            move_intensity
                                        ),
                                        move_factor
                                    )
                                )
                           );
                    }
                    else
                    {
                        // Exploitation 
                        new_position = Add(positions[best_row_idx], ScalarMultiply(positions[best_row_idx], flag * random.NextDouble() * a));
                    }

                    double new_prey_value = fn.Executor(new_position);

                    if(new_prey_value < population_futness_values[population_index]) {
                        positions[population_index] = new_position;
                        
                        population_futness_values[population_index] = new_prey_value;

                        if (new_prey_value < best_fitness_value) {
                            best_fitness_value = new_prey_value;
                            best_row_idx = population_index;
                        }
                    }
                }
            }
            if (log)
            {
                Console.WriteLine(best_fitness_value.ToString());
                Console.WriteLine(String.Join(" ", positions[best_row_idx]));
                Console.WriteLine(population_futness_values[best_row_idx].ToString());
            }
            return fn.Executor(positions[best_row_idx]);
        }

        static Tuple<double, double> optimum()
        {
            Tuple<double, double>[] domain = { Tuple.Create(-4.5, 4.5), Tuple.Create(-4.5, 4.5) };
            Func<double[], double> executor = (double[] parameters) => {
                double x = parameters[0];
                double y = parameters[1];
                return Math.Pow(1.5 - x + x * y, 2) + Math.Pow(2.25 - x + x * y * y, 2) * Math.Pow(2.625 - x + x * y * y * y, 2);
            };
            ObjectiveFunction beatle = new ObjectiveFunction(domain, executor);
            int n = 10;
            double[] output = new double[n];
            double best = double.MaxValue;
            double cbest = 0;
            double bbest = 0;
            for (double c = 0.5; c < 3; c += 0.1)
            {
                for (double b = 0.5; b < 8; b += 0.1)
                {
                    for (int i = 0; i < n; i++)
                    {
                        output[i] = HoneyBadgerAlgorithm(20, 30, b, c, beatle, false);
                    }

                    double average = 0;
                    foreach (var result in output)
                    {
                        average += result;
                    }
                    average /= n;

                    double dev = 0;
                    foreach (var result in output)
                    {
                        dev += (result - average) * (result - average);
                    }
                    dev /= n - 1;
                    dev = Math.Sqrt(dev);

                    double v = 100 * dev / average;

                    if (v < best)
                    {
                        best = v;
                        bbest = b;
                        cbest = c;
                    }
                }
            }
            return Tuple.Create(bbest, cbest);
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
            (var b, var c) = optimum();
            //var b = 0.5;
            //var c = 0.5;
            Console.WriteLine(b.ToString() + " " + c.ToString());
            HoneyBadgerAlgorithm(20, 30, b, c, beatle, true);
            //HoneyBadgerAlgorithm(20, 30, 0.5, 0.5, beatle);
        }
    }
}
