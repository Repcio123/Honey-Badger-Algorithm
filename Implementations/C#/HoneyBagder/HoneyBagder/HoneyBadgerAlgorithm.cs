using HoneyBadger;
using static HoneyBadger.VectorEquations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HoneyBagder.DTO;

namespace HoneyBagder
{
    internal class HoneyBadgerAlgorithm
    {
        private static readonly double Epsilon = Math.Pow(2, -52); // smallest possible number in C#
        static private double[][] SetUpPosition(
            int population,
            System.Tuple<double, double>[] domain
        )
        {
            double[][] x = new double[population][];
            var rand = new Random();

            for (int i = 0; i < population; i++)
            {
                double[] k = new double[domain.Length];
                for (int j = 0; j < domain.Length; j++)
                {
                    (double lowerBound, double upperBound) = domain[j];
                    k[j] = lowerBound + rand.NextDouble() * (upperBound - lowerBound);
                }
                x[i] = (double[])k.Clone()!;
            }
            return x;
        }

        static public IEnumerable<HoneyBadgerResultDTO> Generator(
                    int population,
                    int iterations,
                    double c,
                    double b,
                    ObjectiveFunction fn
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
                double a = c * Math.Exp(-(double)i / (double)iterations);
                for (int population_index = 0; population_index < population - 1; population_index++)
                {
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
                                ScalarAdd(
                                    ScalarMultiply(
                                        Power(
                                            Substract(
                                                positions[best_row_idx],
                                                positions[population_index]
                                            ),
                                            2
                                        ),
                                    4.0 * Math.PI
                                    ),
                                Epsilon
                                )
                            ),
                            random.NextDouble()
                        );

                        new_position =
                            Add(
                                positions[best_row_idx],
                                Add(
                                    ScalarMultiply(
                                        Multiply(
                                            targetSmellIntensity,
                                            positions[best_row_idx]
                                        ),
                                        b * flag
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
                        new_position = Add(positions[best_row_idx], ScalarMultiply(positions[best_row_idx], flag * random.NextDouble() * a));
                    }
                    double new_prey_value = fn.Executor(new_position);

                    if (new_prey_value < population_futness_values[population_index])
                    {
                        positions[population_index] = new_position;

                        population_futness_values[population_index] = new_prey_value;

                        if (new_prey_value < best_fitness_value)
                        {
                            best_fitness_value = new_prey_value;
                            best_row_idx = population_index;
                        }
                        yield return new HoneyBadgerResultDTO
                        {
                            value = best_fitness_value,
                            parameters = positions[best_row_idx],
                        };
                    }
                }
            }

            yield return new HoneyBadgerResultDTO
            {
                value = best_fitness_value,
                parameters = positions[best_row_idx],
            };
        }

        static public HoneyBadgerResultDTO Run(
                    int population,
                    int iterations,
                    double c,
                    double b,
                    ObjectiveFunction fn
            )
        {
            return Generator(population, iterations, c, b, fn).Last();
        }
    }
}
