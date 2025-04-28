using System;

namespace EnigmaNet.Amap.Models.Weathers;

public class LiveModel
{
    public string Province { get; set; }
    public string City { get; set; }
    public string Adcode { get; set; }
    public string Weather { get; set; }
    public int Temperature { get; set; }
    public int Humidity { get; set; }
    public string Winddirection { get; set; }
    public string Windpower { get; set; }
    public DateTime Reporttime { get; set; }
    public float TemperatureFloat { get; set; }
    public float HumidityFloat { get; set; }
}
