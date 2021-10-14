using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnigmaNet.DouYinOpenApi
{
    public interface IOtherApi
    {
        Task SendIMMessageAsync(string openId, string accessToken, string toUserId, bool isImageMessage, string messageContent);

        Task<Models.Poi.PoiListResult> SearchPoiAsync(string clientAccessToken, string cityName, string keyword, int pageSize, long? cursor);
    }
}
