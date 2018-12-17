using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TitaniumProxy.Contracts.Enums;

namespace TitaniumProxy.Models
{

    [DebuggerDisplay("{Part} {Op} {Value}")]
    public class Assertion
    {
        public PartEnum Part { get; set; }
        //public EventEnum OnEvent { get; set; }
        public OpEnum Op { get; set; } = OpEnum.Equals;
        public string Value { get; set; }
    }
}
