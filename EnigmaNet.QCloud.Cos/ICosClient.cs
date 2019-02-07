using System;
using System.Threading.Tasks;

namespace EnigmaNet.QCloud.Cos
{
    public interface ICosClient
    {
        string GetObjectAccessUrl(string path);
        string GetObjectAccessUrl(LineType lineType, string path);
        string GetObjectAccessUrlWithAuthorization(string path, TimeSpan? expiredTimeSpan);
        string GetObjectAccessUrlWithAuthorization(LineType lineType, string path, TimeSpan? expiredTimeSpan);
        void GetObjectUploadInfo(string path, out string putUrl, out string authorization);

        Task DeleteObjectAsync(string path);

        Task CopyObjectAsync(string sourcePath, string targetPath);

        Task UploadObjectAsync(byte[] fileContent, string path, TimeSpan? expiredTimeSpan);

        Task<long> GetObjectContentLengthAsync(string path);
    }
}
