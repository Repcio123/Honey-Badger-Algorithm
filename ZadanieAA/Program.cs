using System;
using System.Globalization;
using System.Threading;

namespace ZadanieAA
{
    class Program
    {
        

        static void Main(string[] args)
        {
            //klasa licząca wartość
            ObjectiveFunction of = new ObjectiveFunction();

            //obliczenie wartości w dowolnym punkcie
            var x = of.FunkcjaCelu.Wartosc(1.26, 1.3, 1.1);


            Console.WriteLine(x);

            //wartość referencyjna dla trzech jednynek
            Console.WriteLine(of.FunkcjaCelu.Wartosc(1.0, 1.0, 1.0));
            
            //optymalizacja w przdziałąch [0.5,1.5] dla każdego z parametrów
            Console.ReadKey();
        }
    }
}
