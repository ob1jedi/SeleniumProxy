using System;
using System.Collections.Generic;
using System.Text;

namespace TitaniumProxy.Models
{
    public class RuleCollection
    {
        public IEnumerable<ProxyRule> Rules { get; set; }
    }
}
