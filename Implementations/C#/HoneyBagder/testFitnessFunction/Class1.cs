using TSFDE_fractional_boundary_condition;

namespace testFitnessFunction
{
    class Test
    {
        TSFDE_fractional_boundary tsfde = new TSFDE_fractional_boundary();
        public double fitnessFunction(params double[] arg) { return tsfde.fintnessFunction(arg); }
    }
}