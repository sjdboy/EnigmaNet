using System;

namespace EnigmaNet.ChinaDivisions;

/// <summary>
/// 行政区域
/// </summary>
public class RegionModel
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    // public int Deep { get; set; }
    public RegionLevel Level { get; set; }
}
