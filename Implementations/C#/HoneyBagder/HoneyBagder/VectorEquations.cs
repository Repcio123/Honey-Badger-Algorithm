using System;

namespace HoneyBadger
{

    public static class VectorEquations
    {
        public static double[] Add(double[] v1, double[] v2)
        {
            if (v1.Length == v2.Length)
            {
                double[] sum = new double[v1.Length];
                for (int i = 0; i < v1.Length; i++)
                {
                    sum[i] = v1[i] + v2[i];
                }
                return sum;
            }
            else
            {
                throw new Exception();
            }
        }
        public static double[] Substract(double[] v1, double[] v2)
        {
            double[] sub = new double[v1.Length];
            for (int i = 0; i < v1.Length; i++)
            {
                sub[i] = v1[i] - v2[i];
            }
            return sub;


        }
        public static double[] Multiply(double[] v1, double[] v2)
        {
            double[] v = new double[v1.Length];
            for (int i = 0; i < v1.Length; i++)
            {
                v[i] = v1[i] * v2[i];
            }
            return v;
        }
        public static double[] ScalarMultiply(double[] v1, double scalar)
        {
            double[] v = new double[v1.Length];
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = v1[i] * scalar;
            }
            return v;
        }
        public static double[] ScalarAdd(double[] v1, double scalar)
        {
            double[] v = new double[v1.Length];
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = v1[i] + scalar;
            }
            return v;
        }
        public static double[] Power(double[] v1, double power)
        {
            double[] v = new double[v1.Length];
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = Math.Pow(v1[i], power);
            }
            return v;
        }
        public static double[] Divide(double[] v1, double[] v2)
        {
            if (v1.Length == v2.Length)
            {
                double[] v = new double[v1.Length];
                for (int i = 0; i < v1.Length; i++)
                {
                    v[i] = v1[i] / v2[i];
                }
                return v;
            }
            else
            {
                throw new Exception();
            }
        }
    }
}