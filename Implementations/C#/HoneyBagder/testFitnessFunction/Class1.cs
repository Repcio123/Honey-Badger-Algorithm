namespace testFitnessFunction
{
    public class Test
    {
        public Tuple<double, double>[] Domain = {
                Tuple.Create<double, double>(0, 5), // 1st dimenstion bounds - (lower, upper)
                Tuple.Create<double, double>(0, 5) // 2nd dimenstion bounds - (lower, upper)
         };
        public double fitnessFunction(params double[] arg) { return arg.Select(d => d + 2).Sum(); }
    }
}