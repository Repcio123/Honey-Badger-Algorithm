using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoneyBagder.StateReader
{
    public interface IStateReader
    {
        public void LoadFromFileStateOfAlgorithm(string path);
    }
}
