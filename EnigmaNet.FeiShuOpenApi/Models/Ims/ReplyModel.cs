using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Models.Ims
{
    public class ReplyModel
    {
        public string MessageId { get; set; }
        public MsgType MsgType { get; set; }
        public MediaInfoModel MediaInfo { get; set; }
        public AuditInfoModel AuditInfo { get; set; }
        public ShareUserInfoModel ShareUserInfo { get; set; }
        public ShareChatInfoModel ShareChatInfo { get; set; }
        public ImageInfoModel ImageInfo { get; set; }
        public TextInfoModel TextInfo { get; set; }
        public FileInfoModel FileInfo { get; set; }
        public StickerInfoModel StickerInfo { get; set; }

    }
}
