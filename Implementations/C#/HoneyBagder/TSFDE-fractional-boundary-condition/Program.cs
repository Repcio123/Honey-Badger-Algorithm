using System;
using System.IO;

namespace TSFDE_fractional_boundary_condition
{
    class Program
    {

        static void Main(string[] args)
        {
                      
            
            // Tworzymy obiekt zadania testowego tzw. funkcji celu
            // obiekt ten posiada publiczną metodę fitnessFunction - tę metodę przekażemy obiektowi algorytmu optymalizacji
            TSFDE_fractional_boundary tsfde_inv = new TSFDE_fractional_boundary();


            // Testowo uruchamiam algorytm GTOA
            #region Tworzenie obiektu odpowiedzialnego za zadanie odwrotne - alg. GTOA

            // Funkcja celu zależna od 7 parametrów zadanych w poniższej dziedzinie
            double[] a = { 0.1, 1.1, 1.0, -70.0, 250.0, -30.0, 50.0 };
            double[] b = { 0.9, 1.9, 5.0, -20.0, 450.0, -10.0, 250.0 };

            // parametry algorytmu GTOA
            int liczbaOsobnikow = 20;
            int liczbaIteracji = 70;


            // WAŻNE: agorytmowi optymalizacji przekazujemy funkcję fitnessFunction
            GTOA algorytmGTOA = new GTOA(tsfde_inv.fintnessFunction, a, b, a.Length, liczbaOsobnikow, liczbaIteracji);

            // mierzymy czas
            DateTime t0 = DateTime.Now;
            algorytmGTOA.Solve();
            DateTime t1 = DateTime.Now;
            TimeSpan t = t1 - t0;
            //File.WriteAllText("time.txt", t.TotalSeconds.ToString());


            #endregion

            Console.ReadKey();

            
        }
    }
}
