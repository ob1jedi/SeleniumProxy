using System;
using System.Collections.Generic;
using System.Text;
using TitaniumProxy.Contracts.Enums;

namespace TitaniumProxy.Models
{
    public class Transform
    {
        public EventEnum On { get; set; }
        public PartEnum Part { get; set; }
        public string Value { get; set; }
    }
}
