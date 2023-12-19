using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitParametersAddin.Models
{
    public class ParametersViewModel
    {
        public bool IsSelected { get; set; }
        public string Name { get; set; }

        [JsonProperty("Type / Instance")]
        public string TypeOrInstance { get; set; }
        public string Category { get; set; }
        public string Id { get; set; }
    }
}
