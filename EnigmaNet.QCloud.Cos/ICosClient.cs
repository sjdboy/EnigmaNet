using System;
using System.Threading.Tasks;
using System.Net.Http;
using EnigmaNet.QCloud.Cos.Models;

namespace EnigmaNet.QCloud.Cos
{
    public interface ICosClient
    {
        string GetObjectAccessUrl(string path);
        string GetObjectAccessUrl(LineType lineType, string path);
        string GetObjectAccessUrlWithAuthorization(string path, TimeSpan? expiredTimeSpan);
        string GetObjectAccessUrlWithAuthorization(LineType lineType, string path, TimeSpan? expiredTimeSpan);
        Task<UploadInfoModel> GetObjectUploadInfo(HttpMethod httpMethod, string path, TimeSpan? expiredTimeSpan = null);

        Task DeleteObjectAsync(string path);

        Task CopyObjectAsync(string sourcePath, string targetPath);

        Task UploadObjectAsync(byte[] fileContent, string path, TimeSpan? expiredTimeSpan);

        Task<long> GetObjectContentLengthAsync(string path);
    }
}
