using System.Collections.Generic;
using System.Linq;
using Casbin.Model;

namespace Casbin.Adapter.Redis.Extensions
{
    public static class CasbinRuleExtenstion
    {
        internal static List<string> ToList(this ICasbinRule rule)
        {
            var list = new List<string> {rule.PType};
            if (string.IsNullOrEmpty(rule.V0) is false)
            {
                list.Add(rule.V0);
            }
            if (string.IsNullOrEmpty(rule.V1) is false)
            {
                list.Add(rule.V1);
            }
            if (string.IsNullOrEmpty(rule.V2) is false)
            {
                list.Add(rule.V2);
            }
            if (string.IsNullOrEmpty(rule.V3) is false)
            {
                list.Add(rule.V3);
            }
            if (string.IsNullOrEmpty(rule.V4) is false)
            {
                list.Add(rule.V4);
            }
            if (string.IsNullOrEmpty(rule.V5) is false)
            {
                list.Add(rule.V5);
            }
            return list;
        }
        
        internal static void ReadPolicyFromCasbinModel<TCasbinRule>(this ICollection<TCasbinRule> casbinRules, IPolicyStore casbinModel) 
            where TCasbinRule : class,ICasbinRule, new()
        {
            if (casbinModel.Sections.ContainsKey("p"))
            {
                foreach (var assertionKeyValuePair in casbinModel.Sections["p"])
                {
                    string policyType = assertionKeyValuePair.Key;
                    Assertion assertion = assertionKeyValuePair.Value;
                    foreach (TCasbinRule rule in assertion.Policy
                                 .Select(ruleStrings => 
                                     Parse<TCasbinRule>(policyType, ruleStrings)))
                    {
                        casbinRules.Add(rule);
                    }
                }
            }
            if (casbinModel.Sections.ContainsKey("g"))
            {
                foreach (var assertionKeyValuePair in casbinModel.Sections["g"])
                {
                    string policyType = assertionKeyValuePair.Key;
                    Assertion assertion = assertionKeyValuePair.Value;
                    foreach (TCasbinRule rule in assertion.Policy
                                 .Select(ruleStrings => 
                                     Parse<TCasbinRule>(policyType, ruleStrings)))
                    {
                        casbinRules.Add(rule);
                    }
                }
            }
        }

        internal static TCasbinRule Parse<TCasbinRule>(string policyType, IEnumerable<string> ruleStrings)
            where TCasbinRule : ICasbinRule, new()
        {
            var rule = new TCasbinRule{PType = policyType};
            var strings = ruleStrings.ToList();
            int count = strings.Count;

            if (count > 0)
            {
                rule.V0 = strings[0];
            }

            if (count > 1)
            {
                rule.V1 = strings[1];
            }

            if (count > 2)
            {
                rule.V2 = strings[2];
            }

            if (count > 3)
            {
                rule.V3 = strings[3];
            }

            if (count > 4)
            {
                rule.V4 = strings[4];
            }

            if (count > 5)
            {
                rule.V5 = strings[5];
            }

            return rule;
        }

    }
}