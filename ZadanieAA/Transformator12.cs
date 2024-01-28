using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZadanieAA
{
    public class Transforamtor12
    {

        int iloscPulsow = 12;
        const double blokowanie = 10_000.0;
        const double przewodzenie = 0.01;
        const double spadek = 0.5;


        double[] Rz = new[] { 1.0, 1.0, 1.0, 0.33, 0.33, 0.33 };

        public double R { get; set; } = 25.0;

        double[] d = new double[12];
        double[,] a = new double[8, 8];
        double[] w = new double[8];
        double[] uD = new double[12];
        double[] i = new double[6];
        double[] v;
        private int iloscKrokow;
        double[,] prady;

        public double[,] Prady => prady;

        public double V { get { return v[7]; } }

        bool zmiana = false;

        public Transforamtor12(int n)
        {
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    a[i, j] = 0.0;
            iloscKrokow = n;
            prady = new double[iloscKrokow, 6];

        }

        private double[] GaussElimination(double[,] A, double[] b, int n)
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
            //ZapiszMacierz(tmpA, "start");

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
                //ZapiszMacierz(tmpA, k.ToString());
            }
            //ZapiszMacierz(tmpA, "35");

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

        private void LiczAdmitancje()
        {

            a[0, 0] = 1 / Rz[3] + 1 / Rz[4] + 1 / Rz[5];
            a[0, 1] = -1 / Rz[3];
            a[0, 2] = -1 / Rz[4];
            a[0, 3] = -1 / Rz[5];

            a[1, 0] = -1 / Rz[3];
            a[1, 1] = 1 / Rz[3] + 1 / d[0] + 1 / d[1];
            a[1, 7] = -1 / d[0];

            a[2, 0] = -1 / Rz[4];
            a[2, 2] = 1 / Rz[4] + 1 / d[2] + 1 / d[3];
            a[2, 7] = -1 / d[2];

            a[3, 0] = -1 / Rz[5];
            a[3, 3] = 1 / Rz[5] + 1 / d[4] + 1 / d[5];
            a[3, 7] = -1 / d[4];

            a[4, 4] = 1 / Rz[0] + 1 / Rz[2] + 1 / d[6] + 1 / d[7];
            a[4, 5] = -1 / Rz[0];
            a[4, 6] = -1 / Rz[2];
            a[4, 7] = -1 / d[6];


            a[5, 4] = -1 / Rz[0];
            a[5, 5] = 1 / Rz[0] + 1 / Rz[1] + 1 / d[8] + 1 / d[9];
            a[5, 6] = -1 / Rz[1];
            a[5, 7] = -1 / d[8];

            a[6, 4] = -1 / Rz[2];
            a[6, 5] = -1 / Rz[1];
            a[6, 6] = 1 / Rz[2] + 1 / Rz[1] + 1 / d[10] + 1 / d[11];
            a[6, 7] = -1 / d[10];

            a[7, 1] = -1 / d[0];
            a[7, 2] = -1 / d[2];
            a[7, 3] = -1 / d[4];
            a[7, 4] = -1 / d[6];
            a[7, 5] = -1 / d[8];
            a[7, 6] = -1 / d[10];
            a[7, 7] = 1 / d[0] + 1 / d[2] + 1 / d[4] + 1 / d[6] + 1 / d[8] + 1 / d[10] + 1 / R;

        }
        private void LiczWymuszenia(double[] u)
        {
            w[0] = -u[3] / Rz[3] - u[4] / Rz[4] - u[5] / Rz[5];
            w[1] = u[3] / Rz[3];
            w[2] = u[4] / Rz[4];
            w[3] = u[5] / Rz[5];
            w[4] = u[0] / Rz[0] - u[2] / Rz[2];
            w[5] = u[1] / Rz[1] - u[0] / Rz[0];
            w[6] = u[2] / Rz[2] - u[1] / Rz[1];
            w[7] = 0.0;

        }

        private void LiczUD(double[] v)
        {
            uD[0] = v[1] - v[7];
            uD[1] = -v[1];
            uD[2] = v[2] - v[7];
            uD[3] = -v[2];
            uD[4] = v[3] - v[7];
            uD[5] = -v[3];
            uD[6] = v[4] - v[7];
            uD[7] = -v[4];
            uD[8] = v[5] - v[7];
            uD[9] = -v[5];
            uD[10] = v[6] - v[7];
            uD[11] = -v[6];
        }

        private bool Test()
        {
            for (int i = 0; i < iloscPulsow; i++)
            {
                if (uD[i] > spadek && d[i] == blokowanie) return false;
                else
                if (uD[i] < 0.0 && d[i] == przewodzenie) return false;
            }
            return true;
        }

        public void Iteracja(double[] u)
        {
            //zamkniecie wszystkich diod
            for (int i = 0; i < iloscPulsow; i++)
                d[i] = blokowanie;

            var it = 0;

            LiczAdmitancje();
            LiczWymuszenia(u);
            v = GaussElimination(a, w, 8);
            LiczUD(v);

            while (!Test() && it++ <= iloscPulsow + 1)
            {
                zmiana = false;
                //sprawdzenie czy nie ma tu za dużo otwartych
                for (int i = 0; i < iloscPulsow; i++)
                    if (uD[i] < 0.0 && d[i] == przewodzenie)
                    {
                        d[i] = blokowanie;
                        zmiana = true;
                    }

                //wlaczanie najbardziej dodatnich
                if (!zmiana)
                {
                    int indeks = 0;
                    double max = uD[0];
                    for (int i = 1; i < iloscPulsow; i++)
                    {
                        if (uD[i] > max)
                        {
                            max = uD[i];
                            indeks = i;
                        }
                    }
                    //odblokowanie wszystkich z maksymalną wartością
                    for (int i = 0; i < iloscPulsow; i++)
                    {
                        if (Math.Abs(uD[i] - max) < 0.0001 && uD[i] > spadek)
                        {
                            d[i] = przewodzenie;
                        }

                    }

                }

                //nowe wyznaczenie Admintancji i Wymuszenia
                LiczAdmitancje();
                LiczWymuszenia(u);

                v = GaussElimination(a, w, 8);
                LiczUD(v);

            }



        }

        public double[] Symulacja(double[,] u, int n)
        {
            double[] vtmp = new double[n];
            double[] utk = new double[6];
            for (int i = 0; i < n; i++)
            {
                utk[0] = u[0, i];
                utk[1] = u[1, i];
                utk[2] = u[2, i];
                utk[3] = u[3, i];
                utk[4] = u[4, i];
                utk[5] = u[5, i];
                Iteracja(utk);
                vtmp[i] = V;
                LiczPrad(utk, i);

            }
            return vtmp;

        }

        public void LiczPrad(double[] u, int k)
        {
            prady[k, 0] = (v[5] + u[0] - v[4]) / Rz[0];
            prady[k, 1] = (v[6] + u[1] - v[5]) / Rz[1];
            prady[k, 2] = (v[4] + u[2] - v[6]) / Rz[2];
            prady[k, 3] = (v[0] + u[3] - v[1]) / Rz[3];
            prady[k, 4] = (v[0] + u[4] - v[2]) / Rz[4];
            prady[k, 5] = (v[0] + u[5] - v[3]) / Rz[5];

        }

        public void ZapiszPrady(string plik)
        {
            string napis = "";
            for (int i = 0; i < iloscKrokow; i++)
            {
                napis += prady[i, 0] + " ";
                for (int j = 1; j < 6; j++)
                {
                    napis += prady[i, j] + " ";
                }
                napis += Environment.NewLine;
            }
            System.IO.File.WriteAllText(plik, napis);
        }
    }
}
