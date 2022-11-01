using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnigmaNet.FeiShuOpenApi.Models.Ims;

namespace EnigmaNet.FeiShuOpenApi
{
    public interface IImApi
    {
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/message/create
        /// </remarks>
        Task<SendResultModel> SendAsync(SendModel model);

        /// <summary>
        /// 回复消息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/message/reply
        /// </remarks>
        Task<SendResultModel> ReplyAsync(ReplyModel model);

        /// <summary>
        /// 撤回消息
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/message/delete
        /// </remarks>
        Task DeleteAsync(string messageId);

        Task<string> UpladImageAsync(byte[] file);

        Task<byte[]> DownloadImageAsync(string fileKey);

        Task<string> UploadFileAsync(UploadFileType fileType,string fileName, byte[] file, int? duration);

        Task<byte[]> DownloadFileAsync(string fileKey);

        Task<byte[]> DownloadMessageFileAsync(string messageId, string fileKey);
    }
}
