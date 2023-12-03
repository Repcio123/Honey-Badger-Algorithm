using HoneyBagder.GenerateTextReport;
using HoneyBagder.MiscInterfaces;
using HoneyBagder.PDFReportGenerator;
using HoneyBagder.StateReader;
using HoneyBagder.StateWriter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder.OptimizationAlgorithm
{
    public interface IOptimizationAlgorithm
    {
        public string Name { get; set; }
        public void Solve(fitnessFunction f, Tuple<double, double>[] domain, params double[] parameters);

        public ParamInfo[] ParamsInfo { get; set; }
        public IStateWriter Writer { get; set; }
        public IStateReader Reader { get; set; }
        public IGenerateTextReport StringReportGenerator { get; set; }
        public IGeneratePDFReport PdfReportGenerator { get; set; }
        double[] XBest { get; set; }
        double FBest { get; set; }
        int NumberOfEvaluationFitnessFunction { get; set; }

    }
}
