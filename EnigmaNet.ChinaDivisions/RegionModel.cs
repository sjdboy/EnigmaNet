using System;

namespace EnigmaNet.ChinaDivisions;

/// <summary>
/// 行政区域
/// </summary>
public class RegionModel
{
    [Obsolete]
    public int Id
    {
        get
        {
            return Convert.ToInt32(Code);
        }
    }

    [Obsolete]
    public int ParentId
    {
        get
        {
            return Convert.ToInt32(ParentCode);
        }
    }

    public long Code { get; set; }
    public long ParentCode { get; set; }

    public required string Name { get; set; }
    public string? ShortName { get; set; }
    public RegionLevel Level { get; set; }
}
