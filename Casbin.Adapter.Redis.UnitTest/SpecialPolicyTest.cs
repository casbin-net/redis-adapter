using Casbin.Adapter.Redis.UnitTest.Fixtures;
using Casbin.Model;
using Xunit;

namespace Casbin.Adapter.Redis.UnitTest
{
    [Collection("Casbin.Adapter.Redis.UnitTest")]
    public class SpecialPolicyTest :  TestUtil, IClassFixture<ModelProvideFixture>
    {
        private readonly ModelProvideFixture _modelProvideFixture;

        public SpecialPolicyTest(ModelProvideFixture modelProvideFixture)
        {
            _modelProvideFixture = modelProvideFixture;
        }

        [Fact]
        public void TestCommaPolicy()
        {
            var adapter = new TestRedisAdapter();
            adapter.Clear();
            var enforcer = new Enforcer(DefaultModel.CreateFromText(@"
[request_definition]
r = _

[policy_definition]
p = rule, a1, a2

[policy_effect]
e = some(where (p.eft == allow))

[matchers]
m = eval(p.rule)
"), adapter);
            enforcer.AddFunction("equal", (a1 , a2) => a1 == a2);
            
            enforcer.AddPolicy("equal(p.a1, p.a2)", "a1", "a1");
            Assert.True(enforcer.Enforce("_"));
            
            enforcer.LoadPolicy();
            Assert.True(enforcer.Enforce("_"));
            
            enforcer.RemovePolicy("equal(p.a1, p.a2)", "a1", "a1");
            enforcer.AddPolicy("equal(p.a1, p.a2)", "a1", "a2");
            Assert.False(enforcer.Enforce("_"));
            
            enforcer.LoadPolicy();
            Assert.False(enforcer.Enforce("_"));
         }
    }
}