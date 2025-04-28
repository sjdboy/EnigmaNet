using System;
using System.Collections.Generic;

namespace EnigmaNet.Amap.Models.Weathers;

public class ForecastModel
{
    public string City { get; set; }
    public string Adcode { get; set; }
    public string Province { get; set; }
    public DateTime Reporttime { get; set; }
    public List<CastModel> Casts { get; set; }
}
