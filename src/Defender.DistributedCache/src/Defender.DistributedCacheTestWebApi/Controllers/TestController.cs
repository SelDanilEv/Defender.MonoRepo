using Defender.DistributedCache;
using Defender.DistributedCacheTestWebApi.Model;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace Defender.DistributedCacheTestWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CacheController : ControllerBase
    {
        private readonly IDistributedCache _distributedCache;

        public CacheController(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] TestModel model)
        {
            await _distributedCache.Add((model) => $"TEST_MODEL_{model.Name}", model, TimeSpan.FromMinutes(10));
            return Ok();
        }

        [HttpGet("get/{name}")]
        public async Task<IActionResult> Get(string name)
        {
            var key = $"TEST_MODEL_{name}";
            var result = await _distributedCache.Get<TestModel>(key);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpDelete("invalidate/{name}")]
        public async Task<IActionResult> Invalidate(string name)
        {
            var key = $"TEST_MODEL_{name}";
            await _distributedCache.Invalidate(key);
            return Ok();
        }

        [HttpGet("getByFields")]
        public async Task<IActionResult> GetByFields([FromQuery] string name, [FromQuery] int age)
        {
            var expressions = new List<Expression<Func<TestModel, bool>>>
            {
                x => x.Name == name,
                x => x.Age == age
            };
            var result = await _distributedCache.Get<TestModel>(expressions);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpDelete("invalidateByFields")]
        public async Task<IActionResult> InvalidateByFields([FromQuery] string name, [FromQuery] int age)
        {
            var expressions = new List<Expression<Func<TestModel, bool>>>
            {
                x => x.Name == name,
                x => x.Age == age
            };
            await _distributedCache.Invalidate(expressions);
            return Ok();
        }
    }
}