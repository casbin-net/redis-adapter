namespace Casbin.Adapter.Redis
{
    public interface IRedisAdapterOptions
    {
        public string Address { get; set; }
        public string Password { get; set; }
        public string Key { get; set; }
    }
}