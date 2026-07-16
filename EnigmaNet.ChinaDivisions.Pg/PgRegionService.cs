using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EnigmaNet.ChinaDivisions.Pg;

public class PgRegionService : IRegionService
{
    private readonly DivisionDbContext _dbContext;

    private static RegionModel ToRegionModel(AreaEntity entity)
    {
        return new RegionModel
        {
            Code = entity.Code,
            ParentCode = entity.ParentCode,
            Name = entity.Name,
            Level = entity.Level,
            ShortName = null
        };
    }

    public PgRegionService(DivisionDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<RegionModel>> GetCityDistrictsAsync(int cityId)
    {
        var entities = await _dbContext.AreaCodes
            .AsNoTracking()
            .Where(x => x.ParentCode == cityId && x.Level == RegionLevel.District)
            .OrderBy(x => x.Code)
            .ToListAsync();

        return entities.Select(ToRegionModel).ToList();
    }

    public async Task<List<RegionModel>> GetProvicesAsync()
    {
        var entities = await _dbContext.AreaCodes
            .AsNoTracking()
            .Where(x => x.ParentCode == 0 && x.Level == RegionLevel.Province)
            .OrderBy(x => x.Code)
            .ToListAsync();

        return entities.Select(ToRegionModel).ToList();
    }

    public async Task<List<RegionModel>> GetProvinceCitiesAsync(int provinceId)
    {
        var entities = await _dbContext.AreaCodes
            .AsNoTracking()
            .Where(x => x.ParentCode == provinceId && x.Level == RegionLevel.City)
            .OrderBy(x => x.Code)
            .ToListAsync();

        return entities.Select(ToRegionModel).ToList();
    }

    public async Task<RegionModel?> GetRegionAsync(int id, RegionLevel? level = null)
    {
        var query = _dbContext.AreaCodes.AsNoTracking().Where(x => x.Code == id);

        if (level.HasValue)
        {
            query = query.Where(x => x.Level == level.Value);
        }

        var entity = await query.FirstOrDefaultAsync();
        return entity == null ? null : ToRegionModel(entity);
    }

    public async Task<List<RegionModel>> GetRegionsAsync()
    {
        throw new NotSupportedException();
    }

    public async Task<List<RegionModel>> GetRegionsAsync(int parentId)
    {
        var entities = await _dbContext.AreaCodes
            .AsNoTracking()
            .Where(x => x.ParentCode == parentId)
            .OrderBy(x => x.Code)
            .ToListAsync();

        return entities.Select(ToRegionModel).ToList();
    }
}
