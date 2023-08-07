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
        // Add other properties as required
        public string Type { get; set; }
    }
}
