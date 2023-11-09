using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder.PDFReportGenerator
{
    public interface IGeneratePDFReport
    {
        public void GenerateReport(string path);
    }
}
