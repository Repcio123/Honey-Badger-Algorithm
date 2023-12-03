using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder.GenerateTextReport
{
    internal class DefaultTextReportGenerator : IGenerateTextReport
    {
        public string ReportString { get; private set; } = null!;
    }
}
