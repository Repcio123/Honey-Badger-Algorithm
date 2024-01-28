
using ZadanieAA;

namespace testFitnessFunction

{
    class Test
    {

        ObjectiveFunction of = new ObjectiveFunction();
        public double fitnessFunction(params double[] arg) { return of.FunkcjaCelu.Wartosc(arg); }
        
    }
}