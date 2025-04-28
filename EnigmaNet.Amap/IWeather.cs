using System;
using System.Threading.Tasks;

using EnigmaNet.Amap.Models.Weathers;

namespace EnigmaNet.Amap;

public interface IWeather
{
    Task<WeatherModel> GetWeatherAsync(string key, string cityAdcode, QueryType queryType = QueryType.Live);
}
