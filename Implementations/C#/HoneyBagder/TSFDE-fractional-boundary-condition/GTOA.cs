using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSFDE_fractional_boundary_condition
{
    public class GTOA
    {
        #region Parametry algorytmu:

        // funkcja celu
        public delegate double funkcjaCelu(params double[] arg);
        private funkcjaCelu f;
        public funkcjaCelu F
        {
            set { f = value; }
        }

        // ograniczenia dziedziny z dołu i góry
        private double[] brzegDol;
        private double[] brzegGora;

        public double[] BrzegDol
        {
            get { return brzegDol; }
            set
            {
                brzegDol = value;
                wymiarZadania = brzegDol.Length;
            }
        }
        public double[] BrzegGora
        {
            get { return brzegGora; }
            set
            {
                brzegGora = value;
                wymiarZadania = brzegGora.Length;
            }
        }

        // wymiar dziedziny
        private int wymiarZadania;

        // populacja (rozwiązania)
        private double[][] populacja;
        private int liczbaOsobnikow;

        // populacja po uczeniu przez nauczyciela
        private double[][] populacjaTeaching;

        // populacja po uczeniu przez uczniów w czasie wolnym
        private double[][] populacjaStudent;

        // tablica wartości funkcji celu osobników w populacji
        private double[] wartosciFunkcjiCelu;

        // tablica wartości funkcji celu osobników w populacjiTeaching
        private double[] wartosciFunkcjiCeluTeaching;

        // tablica wartości funkcji celu osobników w populacjiStudent
        private double[] wartosciFunkcjiCeluStudent;

        // liczba iteracji
        private int liczbaIteracji;

        // najlepszy znaleziony wynik
        public double[] xBest
        {
            get; private set;
        }

        public double yBest
        {
            get; private set;
        }

        // drugi najlepszy znaleziony wynik
        public double[] xSecondBest
        {
            get; private set;
        }

        public double ySecondBest
        {
            get; private set;
        }


        // trzeci najlepszy znaleziony wynik
        public double[] xThirdBest
        {
            get; private set;
        }

        public double yThirdBest
        {
            get; private set;
        }

        private int licznikFunkcjiCelu;

        // zmienna odpowiedzialna za genereowanie liczb pseudolosowych
        static Random random = new Random();

        // parametry algorytmu
        private double[] teacherAllocation;
        private double teachingFactor; // teaching factor - w artykule ozn. F

        #endregion



        #region Konstruktor

        public GTOA(funkcjaCelu fCelu, double[] brzegDol, double[] brzegGora, int wymiarZadania, int liczbaOsobnikow, int liczbaIteracji)
        {
            this.f = fCelu;
            this.brzegDol = brzegDol;
            this.brzegGora = brzegGora;
            this.wymiarZadania = wymiarZadania;
            this.liczbaOsobnikow = liczbaOsobnikow;
            this.liczbaIteracji = liczbaIteracji;
            this.populacja = new double[liczbaOsobnikow][];
            for (int i = 0; i < liczbaOsobnikow; i++)
                populacja[i] = new double[wymiarZadania];
            this.populacjaTeaching = new double[liczbaOsobnikow][];
            for (int i = 0; i < liczbaOsobnikow; i++)
                populacjaTeaching[i] = new double[wymiarZadania];
            this.populacjaStudent = new double[liczbaOsobnikow][];
            for (int i = 0; i < liczbaOsobnikow; i++)
                populacjaStudent[i] = new double[wymiarZadania];
            this.wartosciFunkcjiCelu = new double[liczbaOsobnikow];
            this.wartosciFunkcjiCeluTeaching = new double[liczbaOsobnikow];
            this.wartosciFunkcjiCeluStudent = new double[liczbaOsobnikow];
            this.licznikFunkcjiCelu = 0;
            this.xBest = new double[wymiarZadania];
            this.xSecondBest = new double[wymiarZadania];
            this.xThirdBest = new double[wymiarZadania];
            this.teacherAllocation = new double[wymiarZadania];
            this.teachingFactor = 1.0;

            generujPopulacjeStartowa();
        }

        #endregion


        #region Funkcje pomocnicze

        // Generowanie w sposób losowy populacji startowej
        private void generujPopulacjeStartowa()
        {
            double[] tmp = new double[wymiarZadania];
            for (int i = 0; i < liczbaOsobnikow; i++)
            {
                for (int j = 0; j < wymiarZadania; j++)
                {
                    tmp[j] = brzegDol[j] + random.NextDouble() * (brzegGora[j] - brzegDol[j]);
                    populacja[i][j] = tmp[j];
                }
            }
        }

        // Sortowanie populacji względem jakości rozwiązania
        private void sortuj()
        {
            int n = liczbaOsobnikow;
            do
            {
                for (int i = 0; i < n - 1; i++)
                {
                    if (wartosciFunkcjiCelu[i] > wartosciFunkcjiCelu[i + 1])
                    {
                        double[] temp = new double[wymiarZadania];
                        for (int k = 0; k < wymiarZadania; k++)
                        {
                            temp[k] = populacja[i][k];
                            populacja[i][k] = populacja[i + 1][k];
                            populacja[i + 1][k] = temp[k];
                        }
                        double tmp2 = wartosciFunkcjiCelu[i];
                        wartosciFunkcjiCelu[i] = wartosciFunkcjiCelu[i + 1];
                        wartosciFunkcjiCelu[i + 1] = tmp2;
                    }
                }
                n = n - 1;
            }
            while (n > 1);
        }

        // Funkcja ustala najlepszego osobnika w populacji oraz wwartość funkcji celu dla niego i zapisuje we własności xBest, yBest
        private void ustalNajlepszegoOsobnika()
        {
            int indeks = 0;
            double wartosc = wartosciFunkcjiCelu[0];
            for (int i = 1; i < liczbaOsobnikow; i++)
            {
                if (wartosciFunkcjiCelu[i] < wartosc)
                {
                    wartosc = wartosciFunkcjiCelu[i];
                    indeks = i;
                }
            }

            for (int k = 0; k < wymiarZadania; k++)
                xBest[k] = populacja[indeks][k];
            yBest = wartosc;
        }

        // Zapis stanu populacji
        private void zapiszStanPopulacji(string nazwaPliku, int nrIteracji)
        {
            string infoTekst = $"Iteracja nr: {nrIteracji}\n";
            for (int i = 0; i < liczbaOsobnikow; i++)
            {
                infoTekst += $"( ";
                for (int j = 0; j < wymiarZadania; j++)
                {
                    infoTekst += $"{populacja[i][j]} ";
                }
                infoTekst += $"), {wartosciFunkcjiCelu[i]}\n";
            }
            infoTekst += $"Liczba wywołań funkcji celu: {licznikFunkcjiCelu}\n";

            File.WriteAllText(nazwaPliku, infoTekst);
        }

        #endregion


        public void Solve()
        {
            int numerIteracji = 0;
            int idxBest = 0;
            int idxSecondBest = 1;
            int idxThirdBest = 2;
            double[] xMeanOfThreeBest = new double[wymiarZadania];
            double[] xMean = new double[wymiarZadania];
            double[] xTemp = new double[wymiarZadania];
            double yTemp;
            // zmienne na losowe liczby wykorzystywane w algorytmie
            double a, b, c, d, e, g;
            int losowyIndeks;
            double yLosowy;

            // liczymy wartości funkcji celu dla populacji
            for (int i = 0; i < liczbaOsobnikow; i++)
            {
                wartosciFunkcjiCelu[i] = f(populacja[i]);
                licznikFunkcjiCelu++;
            }
            // sortujemy populację względem jakości rozwiązań
            sortuj();

            // na start w tablicy wartosciFunkcjiCeluTeaching powinno być to samo to w wartosciFunkcjiCelu (tylko na start)
            for (int i = 0; i < liczbaOsobnikow; i++)
            {
                wartosciFunkcjiCeluTeaching[i] = wartosciFunkcjiCelu[i];
            }


            //////////////////////////////////////////////////////
            // główna pętla algorytmu
            while (numerIteracji < liczbaIteracji)
            {
                // ustalamy trzech najlepszych osobników i wartości funkcji celu dla nich,
                // zapisujemy te dane do xBest[], yBest, xSecondBest[], ySecondBest, xThirdBest[], yThirdBest
                for (int k = 0; k < wymiarZadania; k++)
                {
                    xBest[k] = populacja[idxBest][k];
                    xSecondBest[k] = populacja[idxSecondBest][k];
                    xThirdBest[k] = populacja[idxThirdBest][k];
                }
                yBest = wartosciFunkcjiCelu[idxBest];
                ySecondBest = wartosciFunkcjiCelu[idxSecondBest];
                yThirdBest = wartosciFunkcjiCelu[idxThirdBest];

                // ustalenie parametru teacherAllocation
                for (int k = 0; k < wymiarZadania; k++)
                    xMeanOfThreeBest[k] = (xBest[k] + xSecondBest[k] + xThirdBest[k]) / 3.0;
                if(yBest <= f(xMeanOfThreeBest))
                {
                    for (int k = 0; k < wymiarZadania; k++)
                        teacherAllocation[k] = xBest[k];
                    licznikFunkcjiCelu++;
                }
                else
                {
                    for (int k = 0; k < wymiarZadania; k++)
                        teacherAllocation[k] = xMeanOfThreeBest[k];
                    licznikFunkcjiCelu++;
                }

                ///////////////////////////////////////////////////////////////////////////////////
                // teacher phase i student phase: najpierw połowa najlepszych, potem reszta

                // zanim nauczyciel będzie uczył, liczymy średnią
                for (int i = 0; i < liczbaOsobnikow; i++)
                {
                    for (int k = 0; k < wymiarZadania; k++)
                        xMean[k] += populacja[i][k];
                }
                for (int k = 0; k < wymiarZadania; k++)
                    xMean[k] = xMean[k] / (double)liczbaOsobnikow;

                
                // połowa najlepszych
                for (int i = 0; i < liczbaOsobnikow / 2; i++)
                {
                    ///////////////////////////////////////////
                    // TEACHER PHASE
                    a = random.NextDouble();
                    b = random.NextDouble();
                    c = 1 - b;
                    // tworzymy osobnika po fazie uczenia przez nauczyciela
                    for (int k = 0; k < wymiarZadania; k++)
                        xTemp[k] = populacja[i][k] + a * (teacherAllocation[k] - teachingFactor * (b * xMean[k] + c * populacja[i][k]));

                    // jeśli uzyskamy lepszego osobnika, to zastępujemy go w populacjiTeaching
                    yTemp = f(xTemp);
                    licznikFunkcjiCelu++;

                    if (yTemp < wartosciFunkcjiCelu[i])
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                        {
                            populacjaTeaching[i][k] = xTemp[k];
                        }
                        wartosciFunkcjiCeluTeaching[i] = yTemp;
                    }
                    else
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                        {
                            populacjaTeaching[i][k] = populacja[i][k];
                        }
                        wartosciFunkcjiCeluTeaching[i] = wartosciFunkcjiCelu[i];
                    }

                    /////////////////////////////////////
                    // STUDENT PHASE
                    e = random.NextDouble();
                    g = random.NextDouble();

                    // losujemy jakiegoś osobnika (o indksie różnym od i)
                    losowyIndeks = random.Next(0, liczbaOsobnikow);
                    while (losowyIndeks == i)
                        losowyIndeks = random.Next(0, liczbaOsobnikow);

                    // przystępujemy do modyfikacji fazy student phase - wzór (7)
                    yLosowy = wartosciFunkcjiCeluTeaching[losowyIndeks];

                    // tworzymy osobnika po fazie samodzielnego lub od innych studentów uczenia się
                    if (wartosciFunkcjiCeluTeaching[i] < yLosowy)
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                        {
                            xTemp[k] = populacjaTeaching[i][k] + e * (populacjaTeaching[i][k] - populacjaTeaching[losowyIndeks][k])
                                + g * (populacjaTeaching[i][k] - populacja[i][k]);
                        }
                    }
                    else
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                        {
                            xTemp[k] = populacjaTeaching[i][k] - e * (populacjaTeaching[i][k] - populacjaTeaching[losowyIndeks][k])
                                + g * (populacjaTeaching[i][k] - populacja[i][k]);
                        }
                    }

                    // wartość funkcji celu dla osobnika po student phase
                    yTemp = f(xTemp);
                    licznikFunkcjiCelu++;

                    //// jeśli dostaniemy coś lepszego niż po fazie teaching phase, to zastępujemy w głównej populacji
                    //if (wartosciFunkcjiCeluTeaching[i] < yTemp)
                    //{
                    //    for (int k = 0; k < wymiarZadania; k++)
                    //        populacja[i][k] = populacjaTeaching[i][k];
                    //    wartosciFunkcjiCelu[i] = wartosciFunkcjiCeluTeaching[i];
                    //}
                    //else
                    //{
                    //    for (int k = 0; k < wymiarZadania; k++)
                    //        populacja[i][k] = xTemp[k];
                    //    wartosciFunkcjiCelu[i] = yTemp;
                    //}

                    // jeśli dostaniemy coś lepszego niż po fazie teaching phase, to zastępujemy w populacji student
                    if (wartosciFunkcjiCeluTeaching[i] < yTemp)
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                            populacjaStudent[i][k] = populacjaTeaching[i][k];
                        wartosciFunkcjiCeluStudent[i] = wartosciFunkcjiCeluTeaching[i];
                    }
                    else
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                            populacjaStudent[i][k] = xTemp[k];
                        wartosciFunkcjiCeluStudent[i] = yTemp;
                    }
                }
                // pozostała reszta
                for (int i = liczbaOsobnikow / 2; i < liczbaOsobnikow; i++)
                {
                    ///////////////////////////////////////////
                    // TEACHER PHASE

                    d = random.NextDouble();
                    // tworzymy osobnika po fazie uczenia przez nauczyciela
                    for (int k = 0; k < wymiarZadania; k++)
                        xTemp[k] = populacja[i][k] + 2.0 * d * (teacherAllocation[k] - populacja[i][k]);

                    // jeśli uzyskamy lepszego osobnika, to zastępujemy go w populacjiTeaching
                    yTemp = f(xTemp);
                    licznikFunkcjiCelu++;

                    if (yTemp < wartosciFunkcjiCelu[i])
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                        {
                            populacjaTeaching[i][k] = xTemp[k];
                        }
                        wartosciFunkcjiCeluTeaching[i] = yTemp;
                    }
                    else
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                        {
                            populacjaTeaching[i][k] = populacja[i][k];
                        }
                        wartosciFunkcjiCeluTeaching[i] = wartosciFunkcjiCelu[i];
                    }



                    /////////////////////////////////////
                    // STUDENT PHASE
                    e = random.NextDouble();
                    g = random.NextDouble();

                    // losujemy jakiegoś osobnika (o indksie różnym od i)
                    losowyIndeks = random.Next(0, liczbaOsobnikow);
                    while (losowyIndeks == i)
                        losowyIndeks = random.Next(0, liczbaOsobnikow);

                    // przystępujemy do modyfikacji fazy student phase - wzór (7)
                    yLosowy = wartosciFunkcjiCeluTeaching[losowyIndeks];

                    // tworzymy osobnika po fazie samodzielnego lub od innych studentów uczenia się
                    if (wartosciFunkcjiCeluTeaching[i] < yLosowy)
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                        {
                            xTemp[k] = populacjaTeaching[i][k] + e * (populacjaTeaching[i][k] - populacjaTeaching[losowyIndeks][k])
                                + g * (populacjaTeaching[i][k] - populacja[i][k]);
                        }
                    }
                    else
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                        {
                            xTemp[k] = populacjaTeaching[i][k] - e * (populacjaTeaching[i][k] - populacjaTeaching[losowyIndeks][k])
                                + g * (populacjaTeaching[i][k] - populacja[i][k]);
                        }
                    }

                    // wartość funkcji celu dla osobnika po student phase
                    yTemp = f(xTemp);
                    licznikFunkcjiCelu++;

                    //// jeśli dostaniemy coś lepszego niż po fazie teaching phase, to zastępujemy w głównej populacji
                    //if (wartosciFunkcjiCeluTeaching[i] < yTemp)
                    //{
                    //    for (int k = 0; k < wymiarZadania; k++)
                    //        populacja[i][k] = populacjaTeaching[i][k];
                    //    wartosciFunkcjiCelu[i] = wartosciFunkcjiCeluTeaching[i];
                    //}
                    //else
                    //{
                    //    for (int k = 0; k < wymiarZadania; k++)
                    //        populacja[i][k] = xTemp[k];
                    //    wartosciFunkcjiCelu[i] = yTemp;
                    //}

                    // jeśli dostaniemy coś lepszego niż po fazie teaching phase, to zastępujemy w populacji student
                    if (wartosciFunkcjiCeluTeaching[i] < yTemp)
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                            populacjaStudent[i][k] = populacjaTeaching[i][k];
                        wartosciFunkcjiCeluStudent[i] = wartosciFunkcjiCeluTeaching[i];
                    }
                    else
                    {
                        for (int k = 0; k < wymiarZadania; k++)
                            populacjaStudent[i][k] = xTemp[k];
                        wartosciFunkcjiCeluStudent[i] = yTemp;
                    }
                }

                // zerujemy średnią
                for (int k = 0; k < wymiarZadania; k++)
                    xMean[k] = 0;

                // do następnej populacji głównej przepisujemy osobniki po fazie student phase
                for (int i = 0; i < liczbaOsobnikow; i++)
                {
                    for (int k = 0; k < wymiarZadania; k++)
                    {
                        populacja[i][k] = populacjaStudent[i][k];
                    }
                    wartosciFunkcjiCelu[i] = wartosciFunkcjiCeluStudent[i];
                }

                // sortujemy populację
                sortuj();
                Directory.CreateDirectory($@"uczniowie={liczbaOsobnikow} iteracje={liczbaIteracji}");
                zapiszStanPopulacji($@"uczniowie={liczbaOsobnikow} iteracje={liczbaIteracji}/GTOA iteracja={numerIteracji}.txt",
                    numerIteracji);
                numerIteracji++;
            }

            Console.Write($"xBest: ");
            for (int k = 0; k < wymiarZadania; k++)
                Console.Write($"{xBest[k]} ");
            Console.Write($"fcelu: {yBest}");
            Console.Write("\n");
        }
    }
}
