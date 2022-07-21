using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Casbin.Adapter.Redis.Entities;
using Casbin.Adapter.Redis.Extensions;
using Casbin.Model;
using Casbin.Persist;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Casbin.Adapter.Redis
{
    public class RedisAdapter : RedisAdapter<CasbinRule>
    {
        public RedisAdapter() : base()
        {
        }
    }

    public class RedisAdapter<TCasbinRule> : IAdapter, IFilteredAdapter
        where TCasbinRule : class, ICasbinRule, new()
    {
        protected ConnectionMultiplexer Connection { get; }
        protected IDatabase Database { get; }
        protected IRedisAdapterOptions RedisAdapterOptions { get; }
        protected RedisKey Key { get; } = "casbin_rules";
        
        public RedisAdapter()
        {
            IRedisAdapterOptions adapterOptions = new RedisAdapterOptions();
            adapterOptions.Address = "localhost";
            adapterOptions.Password = null;
            adapterOptions.Key = Key;
            
            RedisAdapterOptions = adapterOptions;
            Connection = ConnectionMultiplexer.Connect("localhost");
            Database = Connection.GetDatabase();
        }
        
        public RedisAdapter(IRedisAdapterOptions redisAdapterOptions)
        {
            RedisAdapterOptions = redisAdapterOptions;
            if (redisAdapterOptions.Key != null)
            {
                Key = redisAdapterOptions.Key;
            }
            else
            {
                redisAdapterOptions.Key = Key;
            }
            Connection = ConnectionMultiplexer.Connect($"{redisAdapterOptions.Address},password={redisAdapterOptions.Password}");
            Database = Connection.GetDatabase();
        }

        #region virtual method
        protected virtual IEnumerable<TCasbinRule> OnLoadPolicy(IPolicyStore model, IEnumerable<TCasbinRule> casbinRules)
        {
            return casbinRules;
        }
        
        protected virtual IEnumerable<TCasbinRule> OnSavePolicy(IPolicyStore model, IEnumerable<TCasbinRule> casbinRules)
        {
            return casbinRules;
        }
        
        protected virtual TCasbinRule OnAddPolicy(string section, string policyType, IEnumerable<string> rule, TCasbinRule casbinRule)
        {
            return casbinRule;
        }

        protected virtual IEnumerable<TCasbinRule> OnAddPolicies(string section, string policyType,
            IEnumerable<IEnumerable<string>> rules, IEnumerable<TCasbinRule> casbinRules)
        {
            return casbinRules;
        }

        protected virtual IEnumerable<TCasbinRule> OnRemoveFilteredPolicy(string section, string policyType, 
            int fieldIndex, string[] fieldValues, IEnumerable<TCasbinRule> casbinRules)
        {
            return casbinRules;
        }
        
        #endregion
        
        #region Load policy
        
        public virtual void LoadPolicy(IPolicyStore model)
        {
            var redisValues = Database.ListRange(Key);
            var casbinRules = redisValues.ToCasbinRules<TCasbinRule>();
            casbinRules = OnLoadPolicy(model, casbinRules);
            model.LoadPolicyFromCasbinRules(casbinRules);
            IsFiltered = false;
        }
        
        public virtual async Task LoadPolicyAsync(IPolicyStore model)
        {
            var redisValues = await Database.ListRangeAsync(Key);
            var casbinRules = redisValues.ToCasbinRules<TCasbinRule>();
            casbinRules = OnLoadPolicy(model, casbinRules);
            model.LoadPolicyFromCasbinRules(casbinRules);
            IsFiltered = false;
        }
        
        #endregion
        
        #region Save policy
        
        public virtual void SavePolicy(IPolicyStore model)
        {
            var casbinRules = new List<TCasbinRule>();
            casbinRules.ReadPolicyFromCasbinModel(model);

            if (casbinRules.Count is 0)
            {
                return;
            }

            var saveRules = OnSavePolicy(model, casbinRules);

            Database.KeyDelete(Key);
            foreach (var saveRule in saveRules)
            {
                Database.ListRightPush(Key, JsonConvert.SerializeObject(saveRule));
            }
        }
        
        public virtual async Task SavePolicyAsync(IPolicyStore model)
        {
            var casbinRules = new List<TCasbinRule>();
            casbinRules.ReadPolicyFromCasbinModel(model);

            if (casbinRules.Count is 0)
            {
                return;
            }

            var saveRules = OnSavePolicy(model, casbinRules);

            await Database.KeyDeleteAsync(Key);
            foreach (var saveRule in saveRules)
            {
                await Database.ListRightPushAsync(Key, JsonConvert.SerializeObject(saveRule));
            }
        }
        
        #endregion
        
        #region Add policy
        
        public virtual void AddPolicy(string section, string policyType, IEnumerable<string> rule)
        {
            if (rule is null || rule.Count() is 0)
            {
                return;
            }

            var casbinRule = CasbinRuleExtenstion.Parse<TCasbinRule>(policyType, rule);
            casbinRule = OnAddPolicy(section, policyType, rule, casbinRule);
            Database.ListRightPush(Key, JsonConvert.SerializeObject(casbinRule));
        }
        
        public virtual async Task AddPolicyAsync(string section, string policyType, IEnumerable<string> rule)
        {
            if (rule is null || rule.Count() is 0)
            {
                return;
            }

            var casbinRule = CasbinRuleExtenstion.Parse<TCasbinRule>(policyType, rule);
            casbinRule = OnAddPolicy(section, policyType, rule, casbinRule);
            await Database.ListRightPushAsync(Key, JsonConvert.SerializeObject(casbinRule));
        }
        
        public virtual void AddPolicies(string section, string policyType, IEnumerable<IEnumerable<string>> rules)
        {
            if (rules is null)
            {
                return;
            }

            var rulesArray = rules as IList<string>[] ?? rules.ToArray();
            if (rulesArray.Length is 0)
            {
                return;
            }

            var casbinRules = rulesArray.Select(r => 
                CasbinRuleExtenstion.Parse<TCasbinRule>(policyType, r.ToList()));
            casbinRules = OnAddPolicies(section, policyType, rulesArray, casbinRules);
            foreach (var casbinRule in casbinRules)
            {
                Database.ListRightPush(Key, JsonConvert.SerializeObject(casbinRule));
            }
        }
        
        public virtual async Task AddPoliciesAsync(string section, string policyType, IEnumerable<IEnumerable<string>> rules)
        {
            if (rules is null)
            {
                return;
            }

            var rulesArray = rules as IList<string>[] ?? rules.ToArray();
            if (rulesArray.Length is 0)
            {
                return;
            }

            var casbinRules = rulesArray.Select(r => 
                CasbinRuleExtenstion.Parse<TCasbinRule>(policyType, r.ToList()));
            casbinRules = OnAddPolicies(section, policyType, rulesArray, casbinRules);
            foreach (var casbinRule in casbinRules)
            {
                await Database.ListRightPushAsync(Key, JsonConvert.SerializeObject(casbinRule));
            }
        }
        
        #endregion
        
        #region Remove policy

        public virtual void RemovePolicy(string section, string policyType, IEnumerable<string> rule)
        {
            if (rule is null || rule.Count() is 0)
            {
                return;
            }

            RemoveFilteredPolicy(section, policyType, 0, rule as string[] ?? rule.ToArray());
        }

        public virtual async Task RemovePolicyAsync(string section, string policyType, IEnumerable<string> rule)
        {
            if (rule is null || rule.Count() is 0)
            {
                return;
            }

            await RemoveFilteredPolicyAsync(section, policyType, 0, rule as string[] ?? rule.ToArray());        
        }
        
        public virtual void RemoveFilteredPolicy(string section, string policyType, int fieldIndex, params string[] fieldValues)
        {
            if (fieldValues is null || fieldValues.Length is 0)
            {
                return;
            }

            var redisRules = Database.ListRange(Key);
            var casbinRules = redisRules.ApplyQueryFilter<TCasbinRule>(policyType, fieldIndex, fieldValues);
            casbinRules = OnRemoveFilteredPolicy(section, policyType, fieldIndex, fieldValues, casbinRules);
            
            foreach (var casbinRule in casbinRules)
            {
                Database.ListRemove(Key, JsonConvert.SerializeObject(casbinRule));
            }
        }
        
        public virtual async Task RemoveFilteredPolicyAsync(string section, string policyType, int fieldIndex, params string[] fieldValues)
        {
            if (fieldValues is null || fieldValues.Length is 0)
            {
                return;
            }

            var redisRules = await Database.ListRangeAsync(Key);
            var casbinRules = redisRules.ApplyQueryFilter<TCasbinRule>(policyType, fieldIndex, fieldValues);
            casbinRules = OnRemoveFilteredPolicy(section, policyType, fieldIndex, fieldValues, casbinRules);
            
            foreach (var casbinRule in casbinRules)
            {
                await Database.ListRemoveAsync(Key, JsonConvert.SerializeObject(casbinRule));
            }        
        }


        public virtual void RemovePolicies(string section, string policyType, IEnumerable<IEnumerable<string>> rules)
        {
            if (rules is null)
            {
                return;
            }

            var rulesArray = rules as IList<string>[] ?? rules.ToArray();
            if (rulesArray.Length is 0)
            {
                return;
            }

            foreach (var rule in rulesArray)
            {
                RemoveFilteredPolicy(section, policyType, 0, rule as string[] ?? rule.ToArray());
            }
        }

        public virtual async Task RemovePoliciesAsync(string section, string policyType, IEnumerable<IEnumerable<string>> rules)
        {
            if (rules is null)
            {
                return;
            }

            var rulesArray = rules as IList<string>[] ?? rules.ToArray();
            if (rulesArray.Length is 0)
            {
                return;
            }

            foreach (var rule in rulesArray)
            {
                await RemoveFilteredPolicyAsync(section, policyType, 0, rule as string[] ?? rule.ToArray());
            }
        }

        #endregion

        #region IFilteredAdapter

        public bool IsFiltered { get; private set; }

        public void LoadFilteredPolicy(IPolicyStore model, Filter filter)
        {
            var redisValues = Database.ListRange(Key);
            var casbinRules = redisValues.ApplyQueryFilter<TCasbinRule>(filter);
            casbinRules = OnLoadPolicy(model, casbinRules);
            model.LoadPolicyFromCasbinRules(casbinRules);
            IsFiltered = true;
        }

        public async Task LoadFilteredPolicyAsync(IPolicyStore model, Filter filter)
        {
            var redisValues = await Database.ListRangeAsync(Key);
            var casbinRules = redisValues.ApplyQueryFilter<TCasbinRule>(filter);
            casbinRules = OnLoadPolicy(model, casbinRules);
            model.LoadPolicyFromCasbinRules(casbinRules);
            IsFiltered = true;
        }

        #endregion
    }
}