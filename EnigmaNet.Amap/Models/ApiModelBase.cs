using System;

namespace EnigmaNet.Amap.Models;

public abstract class ApiModelBase
{
    public int Status { get; set; }
    public int Count { get; set; }
    public string Info { get; set; }
    public string Infocode { get; set; }
}
