﻿using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace Casbin.Adapter.Redis.Benchmark
{
    [MemoryDiagnoser]
    [BenchmarkCategory("Model")]
    [SimpleJob(RunStrategy.Throughput, targetCount: 10, runtimeMoniker: RuntimeMoniker.Net48)]
    [SimpleJob(RunStrategy.Throughput, targetCount: 10, runtimeMoniker: RuntimeMoniker.NetCoreApp31, baseline: true)]
    [SimpleJob(RunStrategy.Throughput, targetCount: 10, runtimeMoniker: RuntimeMoniker.Net50)]
    [SimpleJob(RunStrategy.Throughput, targetCount: 10, runtimeMoniker: RuntimeMoniker.Net60)]
    public class ModelBenchmark
    {
        private readonly Enforcer _enforcer;

        public ModelBenchmark()
        {
            var adapter = new TestHelper.TestRedisAdapter();
            adapter.Clear();
            _enforcer = new Enforcer(TestHelper.GetTestFilePath("rbac_model.conf"), adapter);
        }

        private string NowTestUserName { get; set; }
        private string NowTestDataName { get; set; }

        [Params(1000, 10000)]
        public int NowPolicyCount { get; set; }

        [GlobalSetup(Targets = new[] { nameof(AddPolicy), nameof(HasPolicy), nameof(RemovePolicy) })]
        public void GlobalSetup()
        {
            
            for (int i = 0; i < NowPolicyCount; i++)
            {
                _enforcer.AddPolicy($"group{i}", $"obj{i / 10}", "read");
            }
            Console.WriteLine($"// Already set {NowPolicyCount} policies.");

            NowTestUserName = $"name{NowPolicyCount / 2 + 1}";
            NowTestDataName = $"data{NowPolicyCount / 2 + 1}";
            Console.WriteLine($"// Already set user name to {NowTestUserName}.");
            Console.WriteLine($"// Already set data name to {NowTestDataName}.");
        }

        [Benchmark]
        [BenchmarkCategory("ModelManagement")]
        public void HasPolicy()
        {
            _enforcer.HasPolicy(NowTestUserName, NowTestDataName, "read");
        }

        [Benchmark]
        [BenchmarkCategory("ModelManagement")]
        public void AddPolicy()
        {
            _enforcer.AddPolicy(NowTestUserName, NowTestDataName, "read");
        }
        
        [Benchmark]
        [BenchmarkCategory("ModelManagement")]
        public void RemovePolicy()
        {
            _enforcer.RemovePolicy("group0", "obj0", "read");
        }
    }
}
