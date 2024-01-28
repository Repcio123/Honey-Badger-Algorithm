using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSFDE_fractional_boundary_condition
{
    public class TSFDE_fractional_boundary
    {
        #region ARGUMENTY METODY
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////

        int N; //siatka w dziedzinie przestrzeni x
        int M; //siatka w dziedzinie czasu t
        double Lx; //koniec przedziału x
        double T; //końcowy czas
        double alfa; //rząd pochodnej względem czasu t
        double beta; //rząd pochodnej względem przestrzeni x
        //double c; //ciepło właściwe
        //double ro; //gęstość
        double gamma; // współczynnik w warunku brzegowym (gamma=0 --> fractional Neumann, gamma > 0 - fractional Robin)

        // stałe pomocnicze
        double r;

        // warunki początkowo-brzegowe
        public delegate double InitialCondition(double x); //funkcja określająca warunek początkowy
        InitialCondition war_pocz;
        public delegate double Fractional_boundary_right_side(double t, params double[] w); //funkcja występująca w ułamkowym warunku brzegowym na prawym brzegu (x = Lx)
        Fractional_boundary_right_side war_brzeg_prawy;
        
        public delegate double SourceTerm(double x, double t); //funkcja występująca po prawej stronie równania, zależy od x, t
        SourceTerm f;
        public delegate double diffusionCoefficient(double x, double t); //współczynnik przewodności cieplnej przy pochodnej po x i w warunku brzegowym
        diffusionCoefficient lambda;
        
        public delegate double exactSolution(double x, double t); // rozwiązanie dokładne - do testów
        exactSolution exact_sol;
        //-----------------------------------------------------------------
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion


        #region ZMIENNE PROGRAMU
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////

        double[,] U; // dwuwymiarowa tablica w której będą przechowywane rozwiązania, wymiar (M+1)x(N+1) czas x przestrzeń
        double deltaX; // krok siatki dla x
        double deltaT; //krok siatki dla czasu

        double[,] macierzA; // macierz wymiaru N x N - macierz współczynników układu równań
        double[] prawa; // kolumna wyrazów wolnych dla układu równań N x 1
        double[] wektorRoz; // wektor N x 1 przechowujący rozwiązanie układu równań
        double[] x0; // początkowy wektor rozwiązania dla pierwszego układu równań (dla metody BiCGSTAB) - zazwyczaj zerowy

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////

        //--------------------------- zmienne do odwrotnych -------------------------------------------------------
        //zmienne do odwrotnych
        int liczba_termopar;
        double[,] dokladne; //[ liczba_termopar ][ liczba_pomiarow ]
        double[,] odczytane_temperatury; //[ liczba_termopar ][ liczba_pomiarow ]
        int ile_pomiarow;
        int[] punkty_pomiarowe_ind; //[liczba_termopar] w tablicy bedę przechowywane indeksy odpowiadające lokalizacji termopar
        double wart_funkcjonalu;
        //------------------------------------------------------------------------------------
        #endregion


        // dane modelu, jak warunki początkowo-brzegowe i dodatkowe źródło
        private double warPocz_inv(double x)
        {
            return 150.0 * x;
        }

        private double warPrawyBrzeg_inv(double t, params double[] w)
        {
            return 50.0 + Math.Pow(Math.Sqrt(t) - 10.0, 2) +
                (2.0 * t * (900.0 - 45.0 * Math.PI * Math.Sqrt(t) + 8.0 * t)) / (3.0 * Math.Sqrt(Math.PI));
        }

        private double lambda_inv(double x, double t)
        {
            return 2 * x * t;
        }

        // dodatkowy składnik równania
        private double f_sourceTerm_inv(double x, double t)
        {
            return -(2.0 * t * Math.Sqrt(x) * (150.0 + 4.0 * t * x - 15.0 * Math.PI * Math.Sqrt(t * x))) / Math.Sqrt(Math.PI) +
                (5.0 * Math.Pow(t, 0.4) * x * x) / (2.0 * alglib.gammafunction(0.4)) -
                (10.0 * Math.Sqrt(Math.PI) * Math.Pow(x, 1.5)) / (Math.Pow(t, 0.1) * alglib.gammafunction(0.9));
        }


        #region KONSTRUKTOR

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        public TSFDE_fractional_boundary(/*double Lx, int N, double T, int M, double alfa,
                                 double beta, double gamma, InitialCondition war_pocz, 
                                 Fractional_boundary_right_side war_brzeg_prawy,                                 
                                 SourceTerm f, diffusionCoefficient lambda,
                                 int liczbaTermopar, int[] punktyPomiarowe*/)
        {
            this.Lx = 1.0; // Lx;
            this.T = 400.0; // T;
            this.N = 100; //N;
            this.M = 200; // M;
            this.alfa = 0.6; // alfa;
            this.beta = 1.5; // beta;
            //this.c = c;
            //this.ro = ro;
            this.gamma = 1.0; // gamma;
            this.war_pocz = warPocz_inv; // war_pocz;
            this.war_brzeg_prawy = warPrawyBrzeg_inv; // war_brzeg_prawy;
            this.f = f_sourceTerm_inv;  //f;
            this.lambda = lambda_inv; //lambda;

            // siatka
            this.deltaX = Lx / N;
            this.deltaT = T / M;
            this.U = new double[M + 1, N + 1];

            this.prawa = new double[N];
            this.wektorRoz = new double[N];
            this.x0 = new double[N];
            this.macierzA = new double[N, N];           

            // warunek początkowy
            // zerowy wiersz w macierzu U to wartości dla t = 0
            for (int i = 0; i <= N; i++)
            {
                U[0, i] = war_pocz(i * deltaX);
            }

            // warunek brzegowy na lewym końcu
            // kolumna zerowa to wartości w lewym brzegu dla kolejnych chwil czasu
            // na ten moment stały warunek brzegowy równy 0
            for (int k = 1; k <= M; k++)
            {
                U[k, 0] = 0;
            }

            // stałe pomocnicze
            this.r = (Math.Pow(deltaT, alfa) * alglib.gammafunction(2.0 - alfa)) / Math.Pow(deltaX, beta);


            // zadanie odwrotne
            this.liczba_termopar = 1; // liczbaTermopar;
            this.ile_pomiarow = M + 1;
            this.dokladne = new double[1/*liczbaTermopar*/, this.ile_pomiarow];
            this.odczytane_temperatury = new double[1/*liczbaTermopar*/, this.ile_pomiarow];
            this.punkty_pomiarowe_ind = new int[1] { 100 }; // punktyPomiarowe;
            this.wart_funkcjonalu = 0;

            OdczytDanych("danePomiarowe200_c#_xp=1.txt");
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion


        #region FUNKCJE POMOCNICZE
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////

        private double g(double alfa, int i)
        {
            if (i == 0)
                return 1.0;
            return (1 - (alfa + 1.0) / i) * g(alfa, i - 1);
        }
        private double omega(double alfa, int i)
        {
            if (i == 0)
                return (alfa / 2.0) * g(alfa, 0);
            return (alfa / 2.0) * g(alfa, i) + ((2.0 - alfa) / 2.0) * g(alfa, i - 1);
        }

        private double funkcjaB(double alfa, int j)
        {
            return Math.Pow(j + 1.0, 1.0 - alfa) - Math.Pow(j, 1.0 - alfa);
        }





        // Rozwiązywanie układu równań
        public double[] GaussElimination(double[,] A, double[] b, int n)
        {
            double[] x = new double[n];

            double[,] tmpA = new double[n, n + 1];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    tmpA[i, j] = A[i, j];
                }
                tmpA[i, n] = b[i];
            }

            double tmp = 0;

            for (int k = 0; k < n - 1; k++)
            {
                for (int i = k + 1; i < n; i++)
                {
                    tmp = tmpA[i, k] / tmpA[k, k];
                    for (int j = k; j < n + 1; j++)
                    {
                        tmpA[i, j] -= tmp * tmpA[k, j];
                    }
                }
            }

            for (int k = n - 1; k >= 0; k--)
            {
                tmp = 0;
                for (int j = k + 1; j < n; j++)
                {
                    tmp += tmpA[k, j] * x[j];
                }
                x[k] = (tmpA[k, n] - tmp) / tmpA[k, k];
            }

            return x;
        }

        // Metoda BiCGSTAB
        public static void BiCgSTAB(double[,] A, double[] b, int n, double[] x0, double[] rozwiazanie, double epsilon, int iteracje)
        {
            // rozPop - odgrywa rolę xPop
            // rozwiązanie - odgrywa rolę xTeraz
            double[] rDaszek = new double[n];
            double[] r = new double[n];
            double[] rozPop = new double[n];
            double tmp = 0, tmp2 = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    tmp += A[i, j] * x0[j];
                }
                r[i] = b[i] - tmp;
                rDaszek[i] = r[i];
                tmp = 0;
                rozPop[i] = x0[i];
            }
            double sigmaTeraz, sigmaPop = 1;
            double omegaTeraz, omegaPop = 1;
            double alfa = 1, beta;
            double[] vTeraz = new double[n];
            double[] vPop = new double[n];
            double[] pTeraz = new double[n];
            double[] pPop = new double[n];
            double[] s = new double[n];
            double[] t = new double[n];
            double norma = 0;
            int licznik = 1;

            for (licznik = 1; licznik < iteracje; licznik++)
            {
                tmp = 0;
                for (int i = 0; i < n; i++)
                {
                    tmp += rDaszek[i] * r[i];
                }
                sigmaTeraz = tmp;
                beta = (sigmaTeraz / sigmaPop) * (alfa / omegaPop);
                for (int i = 0; i < n; i++)
                {
                    pTeraz[i] = r[i] + beta * (pPop[i] - omegaPop * vPop[i]);
                }
                tmp = 0;
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        tmp += A[i, j] * pTeraz[j];
                    }
                    vTeraz[i] = tmp;
                    tmp = 0;
                }
                tmp = 0;
                for (int i = 0; i < n; i++)
                {
                    tmp += rDaszek[i] * vTeraz[i];
                }
                alfa = sigmaTeraz / tmp;
                for (int i = 0; i < n; i++)
                {
                    s[i] = r[i] - alfa * vTeraz[i];
                }
                tmp = 0;
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        tmp += A[i, j] * s[j];
                    }
                    t[i] = tmp;
                    tmp = 0;
                }
                tmp = 0;
                tmp2 = 0;
                for (int i = 0; i < n; i++)
                {
                    tmp += t[i] * s[i];
                    tmp2 += t[i] * t[i];
                }
                omegaTeraz = tmp / tmp2;
                for (int i = 0; i < n; i++)
                {
                    rozwiazanie[i] = rozPop[i] + alfa * pTeraz[i] + omegaTeraz * s[i];
                }
                // Liczymy błąd rozwiązania
                //tmp = 0;
                //for (int i = 0; i < n; i++)
                //{
                //    for (int j = 0; j < n; j++)
                //    {
                //        tmp += A[i, j] * rozwiazanie[j];
                //    }
                //    r[i] = b[i] - tmp;
                //    tmp = 0;
                //}
                norma = 0;
                tmp = 0;
                // jeśli rozwiązanie satysfakcjonujace to kończymy
                //for (int i = 0; i < n; i++)
                //{
                //    norma += r[i] * r[i];
                //}
                //norma = Math.Sqrt(norma);
                //if (norma < epsilon)
                //    break;

                // Inny warunek stopu: jeśli || x^i - x^{i-1} || <= epsilon
                for (int i = 0; i < n; i++)
                {
                    norma += (rozwiazanie[i] - rozPop[i]) * (rozwiazanie[i] - rozPop[i]);
                }
                norma = Math.Sqrt(norma);
                if (norma < epsilon)
                    break;
                norma = 0;
                for (int i = 0; i < n; i++)
                {
                    r[i] = s[i] - omegaTeraz * t[i];
                    vPop[i] = vTeraz[i];
                    pPop[i] = pTeraz[i];
                    rozPop[i] = rozwiazanie[i];
                }
                sigmaPop = sigmaTeraz;
                omegaPop = omegaTeraz;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion


        #region FUNKCJA SOLVE


        // To właśnie tę funkcję należy przekazać algorytmowi optymalizacyjnemu - jest to funkcja celu
        public double fintnessFunction(params double[] szukaneParametry)
        {
            wart_funkcjonalu = 0;

            #region Uwzględnienie odtwarzanych parametrów

            this.Alfa = szukaneParametry[0];
            this.Beta = szukaneParametry[1];

            this.war_brzeg_prawy = warunekBrzegowyRobin;

            #endregion

            // Główna pętla (przebiega po czasie)
            for (int m = 0; m < M; m++)
            {

                // Komunikat na konsolę
                // if (m % 5 == 0)
                //    Console.WriteLine((((double)m / (double)M) * 100).ToString() + "%");

                #region Uzupełniamy macierz A i wektor wolny układu równań

                for (int i = 1; i <= N; i++)
                {
                    for (int j = 1; j <= N; j++)
                    {
                        if (i <= N - 1 && j <= i - 1)
                        {
                            macierzA[i - 1, j - 1] = -r * lambda(i * deltaX, (m + 1) * deltaT) * omega(beta, i - j + 1);
                        }

                        if (i <= N - 1 && j == i)
                        {
                            macierzA[i - 1, j - 1] = 1.0 - r * lambda(i * deltaX, (m + 1) * deltaT) * omega(beta, 1);
                        }

                        if (i <= N - 1 && j == i + 1)
                        {
                            macierzA[i - 1, j - 1] = -r * lambda(i * deltaX, (m + 1) * deltaT) * omega(beta, 0);
                        }

                        if (i <= N - 2 && j <= N && j >= i + 2)
                        {
                            macierzA[i - 1, j - 1] = 0;
                        }

                        if (i == N && j <= N - 3)
                        {
                            macierzA[i - 1, j - 1] = lambda(N * deltaX, (m + 1) * deltaT) * omega(beta - 1.0, N - j + 1);
                        }

                        if (i == N && j == N - 2)
                        {
                            macierzA[i - 1, j - 1] = lambda(N * deltaX, (m + 1) * deltaT) * omega(beta - 1.0, 3) + lambda(N * deltaX, (m + 1) * deltaT) * omega(beta - 1.0, 0);
                        }

                        if (i == N && j == N - 1)
                        {
                            macierzA[i - 1, j - 1] = lambda(N * deltaX, (m + 1) * deltaT) * omega(beta - 1.0, 2) - 3 * lambda(N * deltaX, (m + 1) * deltaT) * omega(beta - 1.0, 0);
                        }

                        if (i == N && j == N)
                        {
                            macierzA[i - 1, j - 1] = gamma * Math.Pow(deltaX, beta - 1.0) + lambda(N * deltaX, (m + 1) * deltaT) * omega(beta - 1.0, 1) + 3 * lambda(N * deltaX, (m + 1) * deltaT) * omega(beta - 1.0, 0);
                        }
                    }

                    if (m == 0) // przypadek dla pierwszej szukanej chwili czasu wygląda inaczej niż w pozostałych chwilach
                    {
                        if (i <= N - 1)
                        {
                            prawa[i - 1] = U[0, i] + Math.Pow(deltaT, alfa) * alglib.gammafunction(2.0 - alfa) * f(i * deltaX, deltaT);
                        }
                        else // tylko przypadek i = N
                        {
                            prawa[i - 1] = Math.Pow(deltaX, beta - 1.0) * war_brzeg_prawy(deltaT, 
                                szukaneParametry[2], szukaneParametry[3], szukaneParametry[4], szukaneParametry[5], szukaneParametry[6]);
                        }

                    }
                    else
                    {
                        double sumaTemp = 0;
                        for (int k = 1; k <= m - 1; k++)
                        {
                            sumaTemp += (funkcjaB(alfa, k) - funkcjaB(alfa, k + 1)) * U[m - k, i];
                        }

                        if (i <= N - 1)
                        {
                            prawa[i - 1] = (1.0 - funkcjaB(alfa, 1)) * U[m, i] + sumaTemp + funkcjaB(alfa, m) * U[0, i] + Math.Pow(deltaT, alfa) * alglib.gammafunction(2.0 - alfa) * f(i * deltaX, (m + 1) * deltaT);
                        }
                        else // tylko przypadek i = N
                        {
                            prawa[i - 1] = Math.Pow(deltaX, beta - 1.0) * war_brzeg_prawy((m + 1) * deltaT, 
                                szukaneParametry[2], szukaneParametry[3], szukaneParametry[4], szukaneParametry[5], szukaneParametry[6]);
                        }
                    }
                }

                #endregion


                #region Rozwiązanie układu równań 


                //for (int i = 0; i < N; i++)
                //{
                //    for (int j = 0; j < N; j++)
                //    {
                //        Console.Write(macierzA[i, j] + " ");
                //    }
                //    Console.WriteLine(" ");
                //}

                //Console.WriteLine("Prawa: ");



                //for (int i = 0; i < N; i++)
                //{
                //    Console.Write(prawa[i] + " ");
                //}

                //Console.WriteLine(" ");

                //int info;
                //alglib.densesolverreport dsr;
                //alglib.rmatrixsolve(macierzA, N, prawa, out info, out dsr, out wektorRoz);
                //alglib.rmatrixsolvefast(macierzA, N, ref prawa, out info);
                wektorRoz = GaussElimination(macierzA, prawa, N);
                //BiCgSTAB(macierzA, prawa, N, x0, wektorRoz, 0.0000000001, 1000);

                // Wpisanie rozwiązania do macierzy rozwiązań U
                for (int i = 1; i <= N; i++)
                {
                    U[m + 1, i] = wektorRoz[i - 1];
                }

                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < N; j++)
                    {
                        macierzA[i, j] = 0;
                    }
                    prawa[i] = 0;
                    wektorRoz[i] = 0;
                }

                #endregion
            }



            //-----------------------------//funkcjonał//------------------------------------

            //przygotowanie danych - odczyty temperatury z punktów pomiarowych
            for (int i = 0; i < liczba_termopar; i++)
            {
                double[] x_temp = zwrocX(punkty_pomiarowe_ind[i]); //zwracamy wart temp w punkcie pomiarowym
                for (int j = 0; j < ile_pomiarow; j++)
                {
                    odczytane_temperatury[i, j] = x_temp[j];
                }
            }

            wart_funkcjonalu = 0;
            //obliczenie wartości funkcjonału
            for (int i = 0; i < liczba_termopar; i++)
            {
                for (int j = 0; j < ile_pomiarow; j++)
                {
                    wart_funkcjonalu += (Math.Pow(Math.Abs(odczytane_temperatury[i, j] - dokladne[i, j]), 2));
                }
            }

            //MessageBox.Show(wart_funkcjonalu.ToString());
            return wart_funkcjonalu;

            //---------------------------------------------------------------------------------------

            //ZapiszMacierzRozwiazanDoMathematiki("macierzU");
        }

        #endregion


        #region Funkcje i właściwości dotyczące odtwarzanych parametrów w zadaniu odwrotnym

        // warunek brzegowy trzeciego rodzaju
        private double warunekBrzegowyRobin(double t, params double[] g)
        {
            return g[0] * t * t + g[1] * Math.Pow(t, 1.5) + g[2] * t + g[3] * Math.Sqrt(t) + g[4];
        }

        // pochodna po czasie
        public double Alfa
        {
            set { alfa = value; }
            get { return alfa; }
        }

        // pochodna po przestrzeni
        public double Beta
        {
            set { beta = value; }
            get { return beta; }
        }

        #endregion


        #region własności i funkcje pomocnicze

        //jedna kolumna w pliku txt to pomiary z jednej termopary
        public bool OdczytDanych(string pathName)
        {

            if (!System.IO.File.Exists(pathName))
            {
                Console.WriteLine("Plik z danymi nie istnieje czemu?");
                return false;
            }
            else
            {
                string[] linie = System.IO.File.ReadAllLines(pathName);
                string[][] pola = new string[linie.Length][];
                int jk = 0;
                foreach (string s in linie)
                {
                    pola[jk] = s.Split(' ');
                    jk++;
                }

                for (int i = 0; i < linie.Length; i++)
                    for (int j = 0; j < liczba_termopar; j++)
                        dokladne[j, i] = Convert.ToDouble(pola[i][j]);
            }
            //MessageBox.Show("ok");
            return true;
        }

        public double[] zwrocX(int indeks)
        {
            double[] wyn = new double[M + 1];
            for (int i = 0; i < M + 1; i++)
            {
                wyn[i] = U[i, indeks];
            }

            return wyn;
        }

        // Ustawienie rozwiązania dokładnego (do obliczania błędów)
        public exactSolution ExactSolution
        {
            set { this.exact_sol = value; }
        }


        // Zwraca średni błąd bezwzględny
        public double AvgAbsoluteError()
        {
            double err = 0;


            for (int i = 0; i < M + 1; i++)
            {
                for (int j = 0; j < N + 1; j++)
                {
                    err += Math.Abs(exact_sol(j * deltaX, i * deltaT) - U[i, j]);
                }
            }

            err = err / ((N + 1) * (M + 1));
            return err;
        }

        // Zwraca maksymalny błąd bezwzględny
        public double MaxAbsoluteError()
        {
            double maxErr = 0;
            int gdzieX = 0;
            int gdzieT = 0;


            for (int i = 0; i < M + 1; i++)
            {
                for (int j = 0; j < N + 1; j++)
                {
                    if (Math.Abs(exact_sol(j * deltaX, i * deltaT) - U[i, j]) > maxErr)
                    {
                        maxErr = Math.Abs(exact_sol(j * deltaX, i * deltaT) - U[i, j]);
                        gdzieX = j;
                        gdzieT = i;
                    }
                }
            }

            //Console.WriteLine("x: " + gdzieX + " + t: " + gdzieT);
            return maxErr;
        }


        // Funkcja zapisuje dowolną macierz kwadratową do pliku (w celu testów np. macierz wsp układu)
        public void ZapiszMacierzRozwiazanDoMathematiki(string nazwaDoZapisu)
        {
            int liczbaWierszy = U.GetLength(0);
            int liczbaKolumn = U.GetLength(1);
            string napis = "";
            
            for (int j = 0; j < liczbaWierszy; j++)
            {
                for (int i = 0; i < liczbaKolumn; i++)
                {
                    napis += "{ " + (i * deltaX).ToString("F5", CultureInfo.CreateSpecificCulture("en-US")) + ", " +
                        (j * deltaT).ToString("F5", CultureInfo.CreateSpecificCulture("en-US")) + ", " +
                        U[j, i].ToString("F18", CultureInfo.CreateSpecificCulture("en-US"));
                    if (j == liczbaWierszy - 1 && i == liczbaKolumn - 1)
                        napis += "} ";
                    else
                        napis += "}, ";
                }
            }
            System.IO.File.WriteAllText(nazwaDoZapisu + ".txt", napis);
        }


        // Zapisanie stanu temperatury w chwili t = t*deltaT
        public void ZapiszWChwiliT(int t)
        {
            string[] napis = new string[N + 1];
            for (int j = 0; j < M + 1; j++)
            {

                napis[j] += U[t, j].ToString("F18", CultureInfo.InvariantCulture.NumberFormat) + " ";
         
            }
            System.IO.File.WriteAllLines(@"wyniki_w_chwili_t=" + t * deltaT + ".txt", napis);
        }


        public void WygenerujDanePomiarowe(int indeksX)
        {
            //string[] dane_do_zapisu = new string[M + 1];
            string[] dane_do_zapisu2 = new string[(M + 2) / 2];
            //string[] dane_do_zapisu3 = new string[251];
            //string[] dane_do_zapisu4 = new string[1001];
            for (int i = 0; i < M + 1; i++)
            {
                //dane_do_zapisu[i] = U[i, x].ToString();
                if (i % 2 == 0)
                    dane_do_zapisu2[i / 2] = U[i, indeksX].ToString();
                //if (i % 4 == 0)
                //dane_do_zapisu3[i / 4] = U[i, x].ToString();
                //if (i % 5 == 0)
                //dane_do_zapisu4[i / 5] = U[i, x].ToString();

            }

            //System.IO.File.WriteAllLines(@"co_05s.txt", dane_do_zapisu);
            System.IO.File.WriteAllLines(@"danePomiarowe200.txt", dane_do_zapisu2);
        }

        #endregion
    }
}
