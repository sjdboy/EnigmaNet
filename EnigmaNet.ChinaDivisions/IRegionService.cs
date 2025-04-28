using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnigmaNet.ChinaDivisions;

/// <summary>
/// 行政区域服务
/// </summary>
public interface IRegionService
{
    /// <summary>
    /// 获取所有省份
    /// </summary>
    /// <returns></returns>
    Task<List<RegionModel>> GetProvicesAsync();

    /// <summary>
    /// 获取省份的所有城市
    /// </summary>
    /// <param name="provinceId">省份Id</param>
    /// <returns></returns>
    Task<List<RegionModel>> GetProvinceCitiesAsync(int provinceId);

    /// <summary>
    /// 获取城市的所有区县
    /// </summary>
    /// <param name="cityId"></param>
    /// <returns></returns>
    Task<List<RegionModel>> GetCityDistrictsAsync(int cityId);

    /// <summary>
    /// 获取指定区域
    /// </summary>
    /// <param name="id"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    Task<RegionModel> GetRegionAsync(int id, RegionLevel? level = null);

    /// <summary>
    /// 获取所有区域
    /// </summary>
    /// <returns></returns>
    Task<List<RegionModel>> GetRegionsAsync();

    /// <summary>
    /// 获取指定区域的所有子区域
    /// </summary>
    /// <param name="parentId"></param>
    /// <returns></returns>
    Task<List<RegionModel>> GetRegionsAsync(int parentId);
}
