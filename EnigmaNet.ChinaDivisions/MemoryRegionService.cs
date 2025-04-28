using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace EnigmaNet.ChinaDivisions;

/// <summary>
/// 内存版行政区域服务
/// </summary>
public class MemoryRegionService : IRegionService
{
    class JsonRegionModel
    {
        public string id { get; set; }
        public string pid { get; set; }
        public string name { get; set; }
        public string deep { get; set; }
        public string ext_name { get; set; }
    }

    readonly List<JsonRegionModel> _regions = new List<JsonRegionModel>();

    List<RegionModel> CopyRegions(List<JsonRegionModel> regions)
    {
        if (regions == null)
        {
            return null;
        }

        var result = new List<RegionModel>();
        foreach (var region in regions)
        {
            var data = TurnRegion(region);
            result.Add(data);
        }
        return result;
    }

    RegionLevel GetRegionLevel(string deep)
    {
        return deep switch
        {
            "0" => RegionLevel.Province,
            "1" => RegionLevel.City,
            "2" => RegionLevel.District,
            _ => throw new ArgumentOutOfRangeException(nameof(deep), "Invalid region depth")
        };
    }

    string GetDeep(RegionLevel level)
    {
        return level switch
        {
            RegionLevel.Province => "0",
            RegionLevel.City => "1",
            RegionLevel.District => "2",
            _ => throw new ArgumentOutOfRangeException(nameof(level), "Invalid region level")
        };
    }

    RegionModel TurnRegion(JsonRegionModel region)
    {
        if (region == null)
        {
            return null;
        }

        var level = GetRegionLevel(region.deep);

        var idString = region.id;
        // if (level == RegionLevel.District && region.id.Length > 6)
        // {
        //     idString = region.id.Substring(0, 6);
        // }

        var data = new RegionModel
        {
            Id = Convert.ToInt32(idString),
            ParentId = Convert.ToInt32(region.pid),
            Name = region.name,
            Level = level,
            ShortName = region.ext_name,
        };
        return data;
    }

    public MemoryRegionService()
    {
        //读取嵌入式资源
        //<EmbeddedResource Include="Resources\regions.json" />
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "EnigmaNet.ChinaDivisions.Resources.regions.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded resource {resourceName} not found");

        var regions = JsonSerializer.Deserialize<List<JsonRegionModel>>(stream);

        if (regions == null)
        {
            throw new JsonException($"Failed to deserialize {resourceName}");
        }

        for (var i = 0; i < regions.Count; i++)
        {
            var region = regions[i];
            // if (region == null)
            // {
            //     throw new JsonException($"Invalid region data at index {i} in {resourceName}");
            // }

            if (region.id.Length > 6)
            {
                //兼容特殊区如万宁 二级为469006 三级为 469006000 的情况，都转为6位
                region.id = region.id.Substring(0, 6);
            }
        }

        _regions = regions;
    }

    public Task<List<RegionModel>> GetCityDistrictsAsync(int cityId)
    {
        var cityIdString = cityId.ToString();
        var districts = _regions.FindAll(r => r.pid == cityIdString && r.deep == "2");

        var result = CopyRegions(districts);

        return Task.FromResult(result);
    }

    public Task<List<RegionModel>> GetProvicesAsync()
    {
        var provinces = _regions.FindAll(r => r.pid == "0" && r.deep == "0");

        var result = CopyRegions(provinces);

        return Task.FromResult(result);
    }

    public Task<List<RegionModel>> GetProvinceCitiesAsync(int provinceId)
    {
        var provinceIdString = provinceId.ToString();
        var cities = _regions.FindAll(r => r.pid == provinceIdString && r.deep == "1");

        var result = CopyRegions(cities);

        return Task.FromResult(result);
    }

    public Task<RegionModel> GetRegionAsync(int id, RegionLevel? level = null)
    {
        var idString = id.ToString();

        var region = level.HasValue
            ? _regions.Find(r => r.id == idString && r.deep == GetDeep(level.Value))
            : _regions.Find(r => r.id == idString);

        // var region = _regions.Find(r => r.id == idString);

        if (region == null)
        {
            return null;
        }

        var data = TurnRegion(region);

        return Task.FromResult(data);
    }

    public Task<List<RegionModel>> GetRegionsAsync()
    {
        var result = CopyRegions(_regions);

        return Task.FromResult(result);
    }

    public Task<List<RegionModel>> GetRegionsAsync(int parentId)
    {
        var parentIdString = parentId.ToString();
        var regions = _regions.FindAll(r => r.pid == parentIdString);

        var result = CopyRegions(regions);

        return Task.FromResult(result);
    }
}
