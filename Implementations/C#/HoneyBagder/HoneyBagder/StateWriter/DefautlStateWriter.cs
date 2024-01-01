using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder.StateWriter
{
    public class DefautlStateWriter : IStateWriter
    {
        public int Iterator {  get; set; }
        public double[][] Population { get; set; }
        public double[] Fitness { get; set; }
        public DefautlStateWriter(int _iterator, double[][] _population, double[] _fitness)
        {
            Iterator = _iterator;
            Population = _population;
            Fitness = _fitness;
        }
        public DefautlStateWriter() 
        {
            Iterator = 0;
            Population = new double[0][];
            Fitness = new double[0];
        }
        public void SaveToFileStateOfAlgorithm(string path)
        {
            Console.WriteLine(path);
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(path, "state.txt")))
            {
                outputFile.WriteLine(Iterator);
                for (int i = 0; i < Population.Length; i++)
                {
                    for (int j = 0; j < Population[i].Length; j++)
                    {
                        outputFile.Write(Population[i][j] + ' ');
                    }
                    outputFile.Write(" : " + Fitness[i] +'\n');
                }
            }
        }
    }
}
