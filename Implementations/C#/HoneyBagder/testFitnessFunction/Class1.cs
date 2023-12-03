namespace testFitnessFunction
{
    class Test
    {
        public double fitnessFunction(params double[] arg) { return arg.Select(d => d + 2).Sum(); }
    }
}