using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TitaniumProxy.Models;

namespace TitaniumProxy.Services
{
    class ProxyRuleInterpreter
    {
        public ProxyRule InterpretRuleString(string ruleString)
        {
            Regex regex = new Regex(@"\d+");
            Match match = regex.Match("Dot 55 Perls");
            if (match.Success)
            {
                Console.WriteLine(match.Value);
            }
            return new ProxyRule();
        }
    }
}
