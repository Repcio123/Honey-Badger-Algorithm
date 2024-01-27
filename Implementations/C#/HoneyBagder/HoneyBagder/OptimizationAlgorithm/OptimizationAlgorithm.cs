﻿using HoneyBadger;
using HoneyBagder.DTO;
using HoneyBagder.GeneratePDFReport;
using HoneyBagder.GenerateTextReport;
using HoneyBagder.MiscInterfaces;
using HoneyBagder.PDFReportGenerator;
using HoneyBagder.StateReader;
using HoneyBagder.StateWriter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HoneyBadger.VectorEquations;

namespace HoneyBagder.OptimizationAlgorithm
{
    public class OptimizationAlgorithm : IOptimizationAlgorithm, ISubject
    {
        public string Name { get; set; } = "Honey badger algorithm";
        public ParamInfo[] ParamsInfo { get; set; } =
        {
            new ParamInfo
            {
                LowerBoundary = 1,
                UpperBoundary = 4,
                Description = "This is a description",
                Name = "b parameter",
            },
            new ParamInfo
            {
                Description = "This is a description",
                LowerBoundary = 1,
                UpperBoundary = 6,
                Name = "c parameter",
            },
            new ParamInfo
            {
                Description = "This is a description",
                LowerBoundary = 20,
                UpperBoundary = 20,
                Name = "population",
            },
             new ParamInfo
            {
                Description = "This is a description",
                LowerBoundary = 20,
                UpperBoundary = 20,
                Name = "iteration",
            }
        };
        public IStateWriter Writer { get; set; } = new DefautlStateWriter();
        public IStateReader Reader { get; set; } = new DefaultStateReader();
        public IGenerateTextReport StringReportGenerator { get; set; } = new DefaultTextReportGenerator();
        public IGeneratePDFReport PdfReportGenerator { get; set; } = new DefaultReportGenerator();
        public double[] XBest { get; set; }
        public double FBest { get; set; }
        public int FCounter { get; set; }
        public int NumberOfEvaluationFitnessFunction { get; set; }
        private static readonly double Epsilon = Math.Pow(2, -52); // smallest possible number in C#

        

        public void Solve(
                fitnessFunction f,
                Tuple<double, double>[] domain,
                params double[] parameters
            )
        {
            if (parameters.Length != ParamsInfo.Length)
            {
                throw new Exception("Parameter count");
            }
            double b = parameters[0];
            double c = parameters[1];
            int population = (int)parameters[2];
            int iterations = (int)parameters[3];
            double[][] positions = new double[population][];
            var random = new Random();

            Debug.WriteLine(b+" : "+c);

            for (int i = 0; i < population; i++)
            {
                double[] k = new double[domain.Length];
                for (int j = 0; j < domain.Length; j++)
                {
                    (double lowerBound, double upperBound) = domain[j];
                    k[j] = lowerBound + random.NextDouble() * (upperBound - lowerBound);
                }
                positions[i] = (double[])k.Clone()!;
            }

            var population_futness_values = Enumerable.Range(0, population).Select(i => f(positions[i])).ToArray();
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
                Debug.WriteLine(i);
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
                    double new_prey_value = f(new_position);
                    counter++;
                    if (Math.Abs(new_prey_value) < Math.Abs(population_futness_values[population_index]))
                    {
                        positions[population_index] = new_position;

                        population_futness_values[population_index] = new_prey_value;

                        if (Math.Abs(new_prey_value) < Math.Abs(best_fitness_value))
                        {
                            best_fitness_value = new_prey_value;
                            best_row_idx = population_index;
                        }
                    }

                    // Writer = new DefautlStateWriter(i, positions, population_futness_values);
                    // Notify();
                }
            }



            XBest = positions[best_row_idx];
            FBest = best_fitness_value;
            NumberOfEvaluationFitnessFunction = counter;
        }

        private List<IObserver> _observers = new List<IObserver>();
        int counter = 0;

        public void Attach(IObserver observer) 
        {
            _observers.Add(observer);
        }
        public void Detach(IObserver observer)
        {
            _observers.Remove(observer);
        }
        public void Notify()
        {
            foreach (var observer in _observers)
            {
                observer.Update(this);
            }
        }
    }
}
