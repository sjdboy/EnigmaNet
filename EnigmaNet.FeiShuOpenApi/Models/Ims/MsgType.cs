using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Models.Ims
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/im-v1/message/create_json
    /// </remarks>
    public enum MsgType
    {
        Text=1,
        Post=2,
        Image=3,
        File=4,
        Audio=5,
        Media=6,
        Sticker=7,
        Interactive=8,
        ShareChat=9,
        ShareUser=10,
    }
}
