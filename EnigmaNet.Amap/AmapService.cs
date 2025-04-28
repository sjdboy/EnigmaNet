using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using EnigmaNet.Amap.Models.Weathers;
using EnigmaNet.Extensions;
using EnigmaNet.Amap.Models;
using EnigmaNet.Amap.Exceptions;

namespace EnigmaNet.Amap;

public class AmapService : IWeather
{
    class ApiModels
    {
        public class WeatherModel : ApiModelBase
        {
            public List<LiveModel> Lives { get; set; }
            public List<ForecastModel> Forecasts { get; set; }
        }
    }

    const string WeatherUrl = "https://restapi.amap.com/v3/weather/weatherInfo";

    static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy(),
        }
    };

    IHttpClientFactory _httpClientFactory;

    public const string AmapWeatherClientName = "AmapWeatherClient";

    public AmapService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WeatherModel> GetWeatherAsync(string key, string cityAdcode, QueryType queryType = QueryType.Live)
    {
        string extensions;
        switch (queryType)
        {
            case QueryType.Live:
                extensions = "base";
                break;
            case QueryType.Forecast:
                extensions = "all";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(queryType), queryType, null);
        }

        var url = WeatherUrl
            .AddQueryParam("key", key)
            .AddQueryParam("city", cityAdcode)
            .AddQueryParam("extensions", extensions);

        var client = _httpClientFactory.CreateClient(AmapWeatherClientName);
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new AmapException($"Error: {response.StatusCode} - {response.ReasonPhrase}");
        }

        var content = await response.Content.ReadAsStringAsync();

        var weatherModel = JsonConvert.DeserializeObject<ApiModels.WeatherModel>(content, _jsonSerializerSettings);

        if (weatherModel == null)
        {
            throw new AmapException("Failed to deserialize weather data.");
        }

        if (weatherModel.Status != 1)
        {
            throw new AmapException($"Error: {weatherModel.Info} - {weatherModel.Infocode}");
        }

        return new WeatherModel
        {
            Lives = weatherModel.Lives,
            Forecasts = weatherModel.Forecasts,
        };
    }
}
