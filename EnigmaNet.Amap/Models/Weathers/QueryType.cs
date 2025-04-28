using System;

namespace EnigmaNet.Amap.Models.Weathers;

/// <summary>
/// 天气查询类型
/// </summary>
public enum QueryType
{
    /// <summary>
    /// 实况天气
    /// </summary>
    Live = 0,

    /// <summary>
    /// 预报天气
    /// </summary>
    Forecast = 1,
}
