using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZadanieAA
{
     public class FunkcjaCelu12
    {
        double R25 = 15;
        double R75 = 25;


        private Transforamtor12 t12;
        double pierwiastek = Math.Sqrt(3.0);
        private double[,] u;
        private int n;
        private double deltaT;
        public FunkcjaCelu12(double[,] u, int n,Transforamtor12 t12,double deltaT)
        {
            //t12 = new Transforamtor12(n);
            this.t12 = t12;
            this.u = u;
            this.n = n;
            this.deltaT = deltaT;

        }

        private double WartoscSkuteczna(double[] v, double krok)
        {
            int n = v.Length;
            double s= 0.0;
            for (int i = 0; i < n; i++)
            {
                s += v[i] * v[i];
            }

            return Math.Sqrt(s/n);



        }

        public string DrukujWSkuteczne(double[] x)
        {
            var napis = "";
            var v=Wartosc(x);
            double[][] pK = new double[6][];
            double[,] p = t12.Prady;


           
            for (int i = 0; i < 6; i++)
            {
                pK[i] = new double[n];
                for(int j=0;j<n;j++)
                 pK[i][j] = p[j, i];
               
            }


            for (int i = 0; i < 6; i++)
            {
                napis+=$"{i} {WartoscSkuteczna(pK[i], deltaT)}";
                napis += Environment.NewLine;
            }
            return napis;
        }

        public double Wartosc()
        {
            double wU;
            if (t12.R > R75) wU = 1.0;
            else if (t12.R > R25) wU = (t12.R - R25) / (R75 - R25);
            else wU = 0.0;

            double wI = 1.0 - wU;



            double[] v= t12.Symulacja(u, n);
            double[,] p = t12.Prady;

            double pierw3 = Math.Sqrt(3);

            double[] p0 = new double[n];
            double[] p1 = new double[n];
            double[] p2 = new double[n];
            double[] p3 = new double[n];
            double[] p4 = new double[n];
            double[] p5 = new double[n];

            double suma = 0.0;
            for (int i = 0; i < n; i++)
            {
               // suma += Math.Abs(p[i, 1] - p[i, 0]) + Math.Abs(p[i, 2] - p[i, 0]) + Math.Abs(p[i, 2] - p[i, 1]);
               // suma += Math.Abs(p[i, 4] - p[i, 3]) + Math.Abs(p[i, 5] - p[i, 3]) + Math.Abs(p[i, 5] - p[i, 4]);
                p0[i] = p[i,0];
                p1[i] = p[i,1];
                p2[i] = p[i, 2];
                p3[i] = p[i, 3];
                p4[i] = p[i, 4];
                p5[i] = p[i, 5];
            }



            suma += Math.Abs(WartoscSkuteczna(p1, deltaT) - WartoscSkuteczna(p0, deltaT));
            suma += Math.Abs(WartoscSkuteczna(p2, deltaT) - WartoscSkuteczna(p0, deltaT));
            suma += Math.Abs(WartoscSkuteczna(p2, deltaT) - WartoscSkuteczna(p1, deltaT));
            suma += Math.Abs(WartoscSkuteczna(p4, deltaT) - WartoscSkuteczna(p3, deltaT));
            suma += Math.Abs(WartoscSkuteczna(p5, deltaT) - WartoscSkuteczna(p3, deltaT));
            suma += Math.Abs(WartoscSkuteczna(p5, deltaT) - WartoscSkuteczna(p4, deltaT));

            suma += Math.Abs(pierw3 - WartoscSkuteczna(p3,deltaT) / WartoscSkuteczna(p0,deltaT));
            suma += Math.Abs(pierw3 - WartoscSkuteczna(p4, deltaT) / WartoscSkuteczna(p1, deltaT));
            suma += Math.Abs(pierw3 - WartoscSkuteczna(p5, deltaT) / WartoscSkuteczna(p2, deltaT));


            //return suma;

            double min = v[0];
            double max = v[0];
            for (int i = 1; i < n; i++)
            {
                if (min > v[i]) min = v[i];
                else
                if (max < v[i]) max = v[i];
            }
            return wU * (max - min) + wI * suma;

        }

        public double Wartosc(params double[] x)
        {   
            //obliczenia napięć 
            for (int i = 0; i <=2; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    u[i + 3, j] = x[i] * u[i, j]/pierwiastek;
                }
            }

            return Wartosc();
        }


        public double[] V(params double[] x)
        {
            for (int i = 0; i <= 2; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    u[i + 3, j] = x[i] * u[i, j] / pierwiastek;
                }
            }
            return t12.Symulacja(u, n);
        }

        public double Wartosc(double v1, double v2, double v3)
        {
            throw new NotImplementedException();
        }
    }
}
