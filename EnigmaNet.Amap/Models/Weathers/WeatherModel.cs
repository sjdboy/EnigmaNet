using System;
using System.Collections.Generic;

namespace EnigmaNet.Amap.Models.Weathers;

public class WeatherModel
{
    public List<LiveModel> Lives { get; set; }
    public List<ForecastModel> Forecasts { get; set; }
}
