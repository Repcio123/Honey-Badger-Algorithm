using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZadanieAA
{
    public class ObjectiveFunction
    {
        private const int n = 401;
        private double deltaT = 0.00005;

        private double omega = 100 * Math.PI;
        private double alpha = 2.0 * Math.PI / 3.0;
        private double wsp = Math.Sin(7.5 * Math.PI / 180.0) / Math.Sin(52.5 * Math.PI / 180.0);
        private double[][] uabc = new double[n][];


        private void GenerujNapiecieSieci2(params double[] param)
        {
            double t = 0.0;// 27218*(deltaT-1);
            string res = "";
            for (int i = 0; i < n; i++)
            {
                uabc[i] = new double[3];

                uabc[i][0] = param[0] * Math.Sin(omega * t) +
                                    param[3] * Math.Sin(2 * (omega * t + param[9])) +
                                    param[4] * Math.Sin(3 * (omega * t + param[10])) +
                                    param[5] * Math.Sin(5 * (omega * t + param[11])) +
                                    param[6] * Math.Sin(7 * (omega * t + param[12])) +
                                    param[7] * Math.Sin(11 * (omega * t + param[13])) +
                                    param[8] * Math.Sin(13 * (omega * t + param[14]))
                                   ;
                uabc[i][1] = param[1] * Math.Sin(omega * t + alpha) +
                                    param[3] * Math.Sin(2 * (omega * t + param[9])) +
                                    param[4] * Math.Sin(3 * (omega * t + param[10])) +
                                    param[5] * Math.Sin(5 * (omega * t + param[11] + alpha)) +
                                    param[6] * Math.Sin(7 * (omega * t + param[12] + alpha)) +
                                    param[7] * Math.Sin(11 * (omega * t + param[13] + alpha)) +
                                    param[8] * Math.Sin(13 * (omega * t + param[14] + alpha))
                                   ;
                uabc[i][2] = param[2] * Math.Sin(omega * t + 2.0 * alpha) +
                                  param[3] * Math.Sin(2 * (omega * t + param[9])) +
                                   param[4] * Math.Sin(3 * (omega * t + param[10])) +
                                   param[5] * Math.Sin(5 * (omega * t + param[11] + 2.0 * alpha)) +
                                   param[6] * Math.Sin(7 * (omega * t + param[12] + 2.0 * alpha)) +
                                   param[7] * Math.Sin(11 * (omega * t + param[13] + 2.0 * alpha)) +
                                   param[8] * Math.Sin(13 * (omega * t + param[14] + 2.0 * alpha))
                                   ;
                t += deltaT;
                // res += $"{u[i][0]} {u[i][1]} {u[i][2]}" + Environment.NewLine;
            }
            //File.WriteAllText("\\zasilanie.txt", res);
        }


        Transforamtor12 t12 = new Transforamtor12(n);
                   
        double[,] u = new double[6, n];
        double t = 0;


        public FunkcjaCelu12 FunkcjaCelu { get; }

        public ObjectiveFunction()
        {
            t12.R = 15;
            
            GenerujNapiecieSieci2(100.0, 100.0, 100.0, 1.5, 2.3, 1.2, 2.2, 0.5, 1.1, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);

            for (int i = 0; i < n; i++)
            {
                u[0, i] = uabc[i][0];
                u[1, i] = uabc[i][1];
                u[2, i] = uabc[i][2];
                u[3, i] = u[0, i] / Math.Sqrt(3.0);
                u[4, i] = u[1, i] / Math.Sqrt(3.0);
                u[5, i] = u[2, i] / Math.Sqrt(3.0);
                t += deltaT;
            }

            var a = new[] { 0.5, 0.5, 0.5 };
            var b = new[] { 1.5, 1.5, 1.5 };

            FunkcjaCelu = new FunkcjaCelu12(u, 401, t12, deltaT);

        }

    }
}
