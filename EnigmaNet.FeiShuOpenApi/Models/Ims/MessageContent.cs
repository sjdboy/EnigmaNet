using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Models.Ims
{
    public class TextInfoModel
    {
        public string Text { get; set; }
    }

    public class ImageInfoModel
    {
        public string ImageKey { get; set; }
    }

    public class ShareChatInfoModel
    {
        public string ChatId { get; set; }
    }

    public class ShareUserInfoModel
    {
        public string UserId { get; set; }
    }

    public class AuditInfoModel
    {
        public string FileKey { get; set; }
    }

    public class MediaInfoModel
    {
        public string FileKey { get; set; }
        public string ImageKey { get; set; }
    }

    public class FileInfoModel
    {
        public string FileKey { get; set; }
    }

    public class StickerInfoModel
    {
        public string FileKey { get; set; }
    }

}
