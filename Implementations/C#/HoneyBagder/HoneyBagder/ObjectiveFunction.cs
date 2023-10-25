using System;
using System.Linq;


namespace HoneyBadger
{
    public class ObjectiveFunction
    {
        public Tuple<double, double>[] Domain;
        public Func<double[], double> Executor;

        public ObjectiveFunction(Tuple<double, double>[] domain, Func<double[], double> executor)
        {
            Domain = domain;
            Executor = executor;
        }

        public int Dimmentions {
            get { return Domain.Length; }
        }

        public double[] GetRandomParametersWithinDomain()
        {
            double[] result = new double[Domain.Length];
            var random = new Random();
            for (int i = 0; i < Dimmentions; i++)
            {
                (double lower, double upper) = Domain[i];
                result[i] = lower + random.NextDouble() * (upper - lower);
            }
            return result;
        }
    }
}
