# Casbin.NET Redis Adapter

[![Actions Status](https://github.com/casbin-net/Redis-Adapter/workflows/Build/badge.svg)](https://github.com/casbin-net/Redis-Adapter/actions)
[![Coverage Status](https://coveralls.io/repos/github/casbin-net/Redis-Adapter/badge.svg?branch=master)](https://coveralls.io/github/casbin-net/Redis-Adapter?branch=master)
[![NuGet](https://buildstats.info/nuget/Casbin.NET.Adapter.Redis)](https://www.nuget.org/packages/Casbin.NET.Adapter.Redis)

Redis Adapter is the [Redis](https://redis.io/) adapter for [Casbin](https://github.com/casbin/casbin). With this library, Casbin can load policy from Redis or save policy to it.

## Installation

```
dotnet add package Casbin.NET.Adapter.Redis
```

## Simple Example

```csharp
using Casbin.Adapter.Redis;
using NetCasbin;

namespace ConsoleAppExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Initialize a Redis adapter and use it in a Casbin enforcer:
            var redisAdapter = new RedisAdapter("localhost:6379");
            var e = new Enforcer("examples/rbac_model.conf", redisAdapter);

            // Load the policy from Redis.
            e.LoadPolicy();

            // Check the permission.
            e.Enforce("alice", "data1", "read");
            
            // Modify the policy.
            // e.AddPolicy(...)
            // e.RemovePolicy(...)
    
            // Save the policy back to Redis.[README.md](..%2Fcasbin-aspnetcore%2FREADME.md)
            e.SavePolicy();
        }
    }
}
```

## Getting Help

- [Casbin.NET](https://github.com/casbin/Casbin.NET)

## License

This project is under Apache 2.0 License. See the [LICENSE](LICENSE) file for the full license text.
