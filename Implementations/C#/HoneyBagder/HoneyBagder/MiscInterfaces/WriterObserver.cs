using HoneyBagder.OptimizationAlgorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder.MiscInterfaces
{
    public class WriterObserver : IObserver
    {
        public string Path { get; set; }
        public WriterObserver(string _path) 
        {
            Path = _path;
        }
        public void Update(ISubject subject)
        {
           var tmp = subject as IOptimizationAlgorithm;
           tmp.Writer.SaveToFileStateOfAlgorithm(Path);
        }
    }
}
