using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace api.Service
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public T? GetData<T>(string key)
        {
            var data = _cache.GetString(key);
            return data == null ? default : JsonConvert.DeserializeObject<T>(data);
        }

        public void SetData<T>(string key, T value)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45)
            };
            var data = JsonConvert.SerializeObject(value);
            _cache.SetString(key, data, options);
        }

        public void RemoveData(string key)
        {
            _cache.Remove(key);
        }
    }

}