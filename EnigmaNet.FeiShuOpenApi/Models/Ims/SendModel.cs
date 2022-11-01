using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Models.Ims
{
    public class SendModel
    {
        public string TenantAccessToken { get; set; }
        public ReceiveIdType ReceiveIdType { get; set; }
        public string ReceiveId { get; set; }
        public MsgType MsgType { get; set; }
        //public List<AtModel> AtList { get; set; }
        public string Uuid { get; set; }

        public InteractiveModel InteractiveInfo { get; set; }
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
