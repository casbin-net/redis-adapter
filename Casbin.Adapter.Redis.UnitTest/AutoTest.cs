using System.Collections.Generic;
using System.Threading.Tasks;
using Casbin.Adapter.Redis.Entities;
using Casbin.Adapter.Redis.UnitTest.Fixtures;
using Casbin.Persist;
using Newtonsoft.Json;
using StackExchange.Redis;
using Xunit;

namespace Casbin.Adapter.Redis.UnitTest
{
    [Collection("Casbin.Adapter.Redis.UnitTest")]
    public class AdapterTest : TestUtil, IClassFixture<ModelProvideFixture>
    {
        private readonly ModelProvideFixture _modelProvideFixture;
        private static readonly RedisKey _key = "casbin_rules";

        public AdapterTest(ModelProvideFixture modelProvideFixture)
        {
            _modelProvideFixture = modelProvideFixture;
        }

        [Fact]
        public void TestAdapterAutoSave()
        {
            InitPolicy();
            var adapter = new TestRedisAdapter();
            var enforcer = new Enforcer(_modelProvideFixture.GetNewRbacModel(), adapter);
            enforcer.AutoSave = true;
            
            #region Load policy test
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data2_admin")))
            );
            #endregion

            #region Add policy test
            enforcer.AddPolicy("alice", "data1", "write");
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data1", "write")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data2_admin"),
                AsList("alice", "data1", "write")))
            );
            #endregion
            
            #region Remove poliy test
            enforcer.RemovePolicy("alice", "data1", "write");
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data2_admin")))
            );

            enforcer.RemoveFilteredPolicy(0, "data2_admin");
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("alice", "data2_admin")))
            );
            #endregion
            
            #region Batch APIs test
            enforcer.AddPolicies(new []
            {
                new List<string>{"alice", "data2", "write"},
                new List<string>{"bob", "data1", "read"}
            });
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("alice", "data2_admin"),
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")))
            );

            enforcer.RemovePolicies(new []
            {
                new List<string>{"alice", "data1", "read"},
                new List<string>{"bob", "data2", "write"}
            });
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data2_admin"),
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")))
            );
            #endregion
            
            #region IFilteredAdapter test
            enforcer.LoadFilteredPolicy(new Filter
            {
                P = new List<string>{"bob", "data1", "read"},
            });
            TestGetPolicy(enforcer, AsList(
                AsList("bob", "data1", "read")
            ));
            Assert.True(enforcer.Model.Sections["g"]["g"].Policy.Count is 0);
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data2_admin"),
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")))
            );

            enforcer.LoadFilteredPolicy(new Filter
            {
                P = new List<string>{"", "data2", ""},
                G = new List<string>{"", "data2_admin"},
            });
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data2", "write")
            ));
            TestGetGroupingPolicy(enforcer, AsList(
                AsList("alice", "data2_admin")
            ));
            Assert.True(enforcer.Model.Sections["g"]["g"].Policy.Count is 1);
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data2_admin"),
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")))
            );
            #endregion
        }

        [Fact]
        public async Task TestAdapterAutoSaveAsync()
        {
            InitPolicy();
            var adapter = new TestRedisAdapter();
            var enforcer = new Enforcer(_modelProvideFixture.GetNewRbacModel(), adapter);
            enforcer.AutoSave = true;
            
            #region Load policy test
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data2_admin")))
            );
            #endregion
        
            #region Add policy test
            await enforcer.AddPolicyAsync("alice", "data1", "write");
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data1", "write")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data2_admin"),
                AsList("alice", "data1", "write")))
            );            
            #endregion
        
            #region Remove policy test
            await enforcer.RemovePolicyAsync("alice", "data1", "write");
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data2_admin")))
            );
        
            await enforcer.RemoveFilteredPolicyAsync(0, "data2_admin");
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("alice", "data2_admin")))
            );
            #endregion
        
            #region Batch APIs test
            await enforcer.AddPoliciesAsync(new []
            {
                new List<string>{"alice", "data2", "write"},
                new List<string>{"bob", "data1", "read"}
            });
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("alice", "data2_admin"),
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")))
            );
        
            await enforcer.RemovePoliciesAsync(new []
            {
                new List<string>{"alice", "data1", "read"},
                new List<string>{"bob", "data2", "write"}
            });
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")
            ));
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data2_admin"),
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")))
            );
            #endregion
        
            #region IFilteredAdapter test
            await enforcer.LoadFilteredPolicyAsync(new Filter
            {
                P = new List<string>{"bob", "data1", "read"},
            });
            TestGetPolicy(enforcer, AsList(
                AsList("bob", "data1", "read")
            ));
            Assert.True(enforcer.Model.Sections["g"]["g"].Policy.Count is 0);
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data2_admin"),
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")))
            );
        
            await enforcer.LoadFilteredPolicyAsync(new Filter
            {
                P = new List<string>{"", "data2", ""},
                G = new List<string>{"", "data2_admin"},
            });
            TestGetPolicy(enforcer, AsList(
                AsList("alice", "data2", "write")
            ));
            TestGetGroupingPolicy(enforcer, AsList(
                AsList("alice", "data2_admin")
            ));
            Assert.True(enforcer.Model.Sections["g"]["g"].Policy.Count is 1);
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data2_admin"),
                AsList("alice", "data2", "write"),
                AsList("bob", "data1", "read")))
            );
            #endregion
        }

        [Fact]
        public void TestSavePolicy()
        {
            var adapter = new TestRedisAdapter();
            adapter.Clear();
            var e = new Enforcer("examples/rbac_model.conf", "examples/rbac_policy.csv");
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList()))
            );
            adapter.SavePolicy(e.Model);
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data2_admin")))
            );
        }
        
        [Fact]
        public async Task TestSavePolicyAsync()
        {
            var adapter = new TestRedisAdapter();
            adapter.Clear();
            var e = new Enforcer("examples/rbac_model.conf", "examples/rbac_policy.csv");
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList()))
            );
            await adapter.SavePolicyAsync(e.Model);
            Assert.True(Array2DEquals(adapter.GetPolicies(), AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data2_admin")))
            );
        }

        private static void InitPolicy()
        {
            ConnectionMultiplexer connect = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = connect.GetDatabase();
            db.KeyDelete(_key);
            db.ListRightPush(_key, JsonConvert.SerializeObject(new CasbinRule
            {
                PType = "p",
                V0 = "alice",
                V1 = "data1",
                V2 = "read",
            }));
            db.ListRightPush(_key, JsonConvert.SerializeObject(new CasbinRule
            {
                PType = "p",
                V0 = "bob",
                V1 = "data2",
                V2 = "write",
            }));
            db.ListRightPush(_key, JsonConvert.SerializeObject(new CasbinRule
            {
                PType = "p",
                V0 = "data2_admin",
                V1 = "data2",
                V2 = "read",
            }));
            db.ListRightPush(_key, JsonConvert.SerializeObject(new CasbinRule
            {
                PType = "p",
                V0 = "data2_admin",
                V1 = "data2",
                V2 = "write",
            }));
            db.ListRightPush(_key, JsonConvert.SerializeObject(new CasbinRule
            {
                PType = "g",
                V0 = "alice",
                V1 = "data2_admin",
            }));
            connect.Close();
        }
    }
}
