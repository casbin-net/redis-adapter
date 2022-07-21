using System;
using System.Collections.Generic;
using System.Linq;
using Casbin.Persist;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Casbin.Adapter.Redis.Extensions
{
    public static class RedisValuesExtension
    {
        public static IEnumerable<TCasbinRule> ToCasbinRules<TCasbinRule>(this RedisValue[] redisValues)
            where TCasbinRule : ICasbinRule
        {
            ICollection<TCasbinRule> casbinRules = new List<TCasbinRule>();
            foreach (var redisValue in redisValues)
            {
                casbinRules.Add(JsonConvert.DeserializeObject<TCasbinRule>(redisValue.ToString()));
            }
            return casbinRules;
        }
        
        internal static IEnumerable<TCasbinRule> ApplyQueryFilter<TCasbinRule>(this RedisValue[] redisValues, 
            string policyType , int fieldIndex, IEnumerable<string> fieldValues)
            where TCasbinRule : ICasbinRule
        {
            if (fieldIndex > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldIndex));
            }
            
            ICollection<TCasbinRule> casbinRulesCollection = new List<TCasbinRule>();
            foreach (var redisValue in redisValues)
            {
                casbinRulesCollection.Add(JsonConvert.DeserializeObject<TCasbinRule>(redisValue));
            }
            IEnumerable<TCasbinRule> casbinRules = casbinRulesCollection;
            
            var fieldValuesList = fieldValues as IList<string> ?? fieldValues.ToArray();
            int fieldValueCount = fieldValuesList.Count;

            if (fieldValueCount is 0)
            {
                return casbinRules;
            }

            int lastIndex = fieldIndex + fieldValueCount - 1;

            if (lastIndex > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(lastIndex));
            }

            casbinRules = casbinRules.Where(p => string.Equals(p.PType, policyType));

            if (fieldIndex is 0 && lastIndex >= 0)
            {
                string field = fieldValuesList[fieldIndex];
                if (string.IsNullOrWhiteSpace(field) is false)
                {
                    casbinRules = casbinRules.Where(p => p.V0 == field);
                }
            }

            if (fieldIndex <= 1 && lastIndex >= 1)
            {
                string field = fieldValuesList[1 - fieldIndex];
                if (string.IsNullOrWhiteSpace(field) is false)
                {
                    casbinRules = casbinRules.Where(p => p.V1 == field);
                }
            }

            if (fieldIndex <= 2 && lastIndex >= 2)
            {
                string field = fieldValuesList[2 - fieldIndex];
                if (string.IsNullOrWhiteSpace(field) is false)
                {
                    casbinRules = casbinRules.Where(p => p.V2 == field);
                }
            }

            if (fieldIndex <= 3 && lastIndex >= 3)
            {
                string field = fieldValuesList[3 - fieldIndex];
                if (string.IsNullOrWhiteSpace(field) is false)
                {
                    casbinRules = casbinRules.Where(p => p.V3 == field);
                }
            }

            if (fieldIndex <= 4 && lastIndex >= 4)
            {
                string field = fieldValuesList[4 - fieldIndex];
                if (string.IsNullOrWhiteSpace(field) is false)
                {
                    casbinRules = casbinRules.Where(p => p.V4 == field);
                }
            }

            if (lastIndex is 5) // and fieldIndex <= 5
            {
                string field = fieldValuesList[5 - fieldIndex];
                if (string.IsNullOrWhiteSpace(field) is false)
                {
                    casbinRules = casbinRules.Where(p => p.V5 == field);
                }
            }

            return casbinRules;
        }
        
        internal static IEnumerable<TCasbinRule> ApplyQueryFilter<TCasbinRule>(this RedisValue[] redisValues, Filter filter)
            where TCasbinRule : ICasbinRule
        {
            var casbinRules = redisValues.ToCasbinRules<TCasbinRule>();
            
            if (filter is null)
            {
                return casbinRules;
            }

            if (filter.P is null && filter.G is null)
            {
                return casbinRules;
            }

            if (filter.P is not null && filter.G is not null)
            {
                var queryP = redisValues.ApplyQueryFilter<TCasbinRule>(PermConstants.DefaultPolicyType, 0, filter.P);
                var queryG = redisValues.ApplyQueryFilter<TCasbinRule>(PermConstants.DefaultGroupingPolicyType, 0, filter.G);
                return queryP.Union(queryG);
            }

            if (filter.P is not null)
            {
                casbinRules = redisValues.ApplyQueryFilter<TCasbinRule>(PermConstants.DefaultPolicyType, 0, filter.P);
            }

            if (filter.G is not null)
            {
                casbinRules = redisValues.ApplyQueryFilter<TCasbinRule>(PermConstants.DefaultGroupingPolicyType, 0, filter.G);
            }

            return casbinRules;
        }

    }
}