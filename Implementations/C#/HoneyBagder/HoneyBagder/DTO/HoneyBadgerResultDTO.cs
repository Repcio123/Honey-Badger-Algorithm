using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HoneyBagder.DTO
{
    internal class HoneyBadgerResultDTO
    {
        [JsonInclude]
        public double value;
        [JsonInclude]
        public double[] parameters = null!;
    }
}
