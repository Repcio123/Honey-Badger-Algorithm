using HoneyBadger;
using HoneyBagder.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder
{
    public class Statistics
    {
        static double avg(double[] values)
        {
            double average = 0;
            foreach (var x in values)
            {
                average += x;
            }
            average /= values.Length;
            return average;
        }

        static double dev(double[] values)
        {
            double average = avg(values);
            double dev = 0;
            foreach (var x in values)
            {
                dev += (x - average) * (x - average);
            }
            dev /= values.Length - 1;
            return dev;
        }

        static public OptimumResultDTO optimum(int population, int iterations)
        {
            Tuple<double, double>[] domain = { Tuple.Create(-4.5, 4.5), Tuple.Create(-4.5, 4.5) };
            Func<double[], double> executor = (double[] parameters) => {
                double x = parameters[0];
                double y = parameters[1];
                return Math.Pow(1.5 - x + x * y, 2) + Math.Pow(2.25 - x + x * y * y, 2) * Math.Pow(2.625 - x + x * y * y * y, 2);
            };
            ObjectiveFunction beatle = new ObjectiveFunction(domain, executor);
            int n = 10;
            HoneyBadgerResultDTO[] output = new HoneyBadgerResultDTO[n];

            OptimumResultDTO? finalResult = null;

            for (double c = 0.5; c < 3; c += 0.1)
            {
                for (double b = 0.5; b < 8; b += 0.1)
                {
                    HoneyBadgerResultDTO? ibest = null;
                    for (int i = 0; i < n; i++)
                    {
                        HoneyBadgerResultDTO result = HoneyBadgerAlgorithm.Run(population, iterations, b, c, beatle);
                        output[i] = result;
                        if (ibest == null || result.value < ibest.value)
                        {
                            ibest = result;
                        }
                    }

                    double ParameterMean = avg(ibest.parameters);
                    double ParamsStandardDev = Math.Sqrt(dev(ibest.parameters));
                    double standardDevValues = Math.Sqrt(dev(output.Select(x => x.value).ToArray()));

                    double v = 100 * ParamsStandardDev / Math.Abs(ParameterMean);
                    if (finalResult == null || v < finalResult.vbest)
                    {
                        finalResult = new OptimumResultDTO
                        {
                            vbest = v,
                            bbest = b,
                            cbest = c,
                            parameters = ibest.parameters,
                            population = population,
                            iterations = iterations,
                            iterationParametersStandardDev = ParamsStandardDev,
                            iterationValuesStandardDev = standardDevValues
                        };

                    }
                }
            }
            return finalResult;
        }
    }
}
