using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder.StateWriter
{
    public interface IStateWriter
    {
        void SaveToFileStateOfAlgorithm(string path);
    }
}
