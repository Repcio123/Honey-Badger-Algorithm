using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder.DTO
{
    public class OptimumResultDTO
    {
        public double vbest;
        public double bbest;
        public double cbest;
        public double[] parameters = null!;
        public int population;
        public int iterations;
        public double iterationValuesStandardDev;
        public double iterationParametersStandardDev;
    }
}
