using System;
using System.Collections.Generic;
using System.Text;
using TitaniumProxy.Contracts.Enums;

namespace TitaniumProxy.Models
{

    public class ProxyRule
    {
        public string Name {get;set;}
        //public EventEnum OnEvent { get; set; }
        //public ICollection<FactGroup> FactGroups { get; set; }
        public JunctionEnum Junction { get; set; } = JunctionEnum.And;
        public ICollection<Assertion> IfTrue { get; set; }
        public ICollection<Transform> ThenSet { get; set; }
    }
}