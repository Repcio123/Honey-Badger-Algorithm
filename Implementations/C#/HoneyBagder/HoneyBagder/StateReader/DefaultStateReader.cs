using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder.StateReader
{
    public class DefaultStateReader : IStateReader
    {
        public int Size { get; set; }
        public int Dimension { get; set; }
        public int Iterator { get; set; }
        public double[][] Population { get; set; }
        public double[] Fitness { get; set; }
        public void LoadFromFileStateOfAlgorithm(string path)
        {
            using (StreamReader inputFile = new StreamReader(Path.Combine(path, "state.txt")))
            {
                Iterator = int.Parse(inputFile.ReadLine());
                var tmp = inputFile.ReadLine();
                Size = int.Parse(tmp[0].ToString());
                Dimension = int.Parse(tmp[2].ToString());
                Population = new double[Size][];
                Fitness = new double[Size];
                for (int i = 0; i < Size; i++)
                {
                    tmp = inputFile.ReadLine();
                    var splitted = tmp.Split(' ');
                    Population[i] = new double[Dimension];
                    for (int j = 0; j < Dimension; j++)
                    {
                        Population[i][j] = double.Parse(splitted[j]);
                    }
                    Fitness[i] = double.Parse(splitted[splitted.Length - 1]);
                }
            }
            Console.WriteLine(Iterator);
            Console.WriteLine(Size + ' ' +  Dimension);
            Console.WriteLine(Fitness);
            Console.WriteLine(Population);
        }
    }
}
