using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Redis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        private readonly IDistributedCache _distributedCache;
        private const string COUNTRIES_KEY = "Countries";

        public CountryController(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        [HttpGet]
        public async Task<IActionResult> GetCountries()
        {
            var countriesObject = await _distributedCache.GetStringAsync(COUNTRIES_KEY);

            if (!String.IsNullOrWhiteSpace(countriesObject))
            {
                var response = JsonSerializer.Deserialize<List<Country>>(countriesObject, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(response);
            }
            else
            {
                const string restCountriesUrl = "https://restcountries.com/v2/all";

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(restCountriesUrl);
                    var responseData = await response.Content.ReadAsStringAsync();
                    var countries = JsonSerializer.Deserialize<List<Country>>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var memoryCacheEntryOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3600),
                        SlidingExpiration = TimeSpan.FromSeconds(1200)
                    };

                    var json = JsonSerializer.Serialize(countries);
                    await _distributedCache.SetStringAsync(COUNTRIES_KEY, json, memoryCacheEntryOptions);

                    return Ok(countries);
                }
            }
        }
    }
}
