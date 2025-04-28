using System;

namespace EnigmaNet.Amap.Models.Weathers;

public class CastModel
{
    public DateTime Date { get; set; }
    public int Week { get; set; }
    public string Dayweather { get; set; }
    public string Nightweather { get; set; }
    public int Daytemp { get; set; }
    public int Nighttemp { get; set; }
    public string Daywind { get; set; }
    public string Nightwind { get; set; }
    public string Daypower { get; set; }
    public string Nightpower { get; set; }
    public float DaytempFloat { get; set; }
    public float NighttempFloat { get; set; }
}
