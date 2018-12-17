using System;
using System.Collections.Generic;
using System.Text;
using TitaniumProxy.Contracts.Enums;

namespace TitaniumProxy.Models
{
    public class FactGroup
    {
        public JunctionEnum Junction { get; set; }
        public ICollection<Assertion> Facts { get; set; }
    }
}
