using System;
using System.Collections.Generic;
using System.Linq;
using TitaniumProxy.Contracts.Enums;
using TitaniumProxy.Models;

namespace TitaniumProxy.Services
{
    public class ProxyRuleEvaluator
    {
        public static IEnumerable<ProxyRule> EvaluateRulesAndReturnPassedRules(IEnumerable<ProxyRule> rules, IEnumerable<Assertion> worldState)
        {
            return rules
                .Where(rule => RuleIsTrue(rule, worldState))
                .ToList();            
        }

        public static bool RuleIsTrue(ProxyRule rule, IEnumerable<Assertion> worldState)
        {
            var factGroup = new FactGroup()
            {
                Junction = rule.Junction,
                Facts = rule.IfTrue
            };
            return FactGroupIsTrue(factGroup, worldState);
            //return rule.FactGroups.All(fg => FactGroupIsTrue(fg, worldState));
        }

        private static bool FactGroupIsTrue(FactGroup factGroup, IEnumerable<Assertion> worldState)
        {
            switch (factGroup.Junction)
            {
                case JunctionEnum.And:
                    return factGroup.Facts.All(fact => FactIsTrue(fact, worldState));
                case JunctionEnum.Or:
                    return factGroup.Facts.Any(fact => FactIsTrue(fact, worldState));
            }
            throw new Exception("Unknown junction type: " + factGroup.Junction);
        }

        private static bool FactIsTrue(Assertion fact, IEnumerable<Assertion> worldState)
        {
            if (!worldState.Any(f => f.Part == fact.Part)) return false;

            var expectedValue = fact.Value;
            var actualValue = worldState.Single(f => f.Part == fact.Part).Value;
            if (actualValue == null) return false;

            switch (fact.Op)
            {
                case OpEnum.Equals:
                    return expectedValue == worldState.Single(f => f.Part == fact.Part).Value;
                case OpEnum.IsNotEqualTo:
                    return expectedValue != worldState.Single(f => f.Part == fact.Part).Value;
                case OpEnum.Contains:
                    return actualValue.Contains(expectedValue);
                case OpEnum.In:
                    return expectedValue.Contains(actualValue);
            }
            throw new Exception("Unknown operator type: " + fact.Op);
        }
    }

}
