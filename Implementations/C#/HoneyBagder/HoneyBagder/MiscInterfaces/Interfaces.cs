using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder.MiscInterfaces
{
    public delegate double fitnessFunction(params double[] args);

    public class ParamInfo
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public double UpperBoundary { get; set; }
        public double LowerBoundary { get; set; }
    }

    public interface IObserver
    {
        void Update(ISubject subject);
    }

    public interface ISubject
    {
        void Attach(IObserver observer);
        void Detach(IObserver observer);
        void Notify();
    }

    //double bealeFunction (double[] parameters) {
    //    double x = parameters[0];
    //    double y = parameters[1];
    //    return Math.Pow(1.5 - x + x * y, 2) + Math.Pow(2.25 - x + x * y * y, 2) + Math.Pow(2.625 - x + x * y * y * y, 2);
    //}
}
