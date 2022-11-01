using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnigmaNet.Extensions;

namespace EnigmaNet.FeiShuOpenApi.Models.Ims
{
    public class SendResultModel
    {
        public class SenderModel
        {
            public string Id { get; set; }
            public string IdType { get; set; }
            public string SenderType { get; set; }
            public string TenantKey { get; set; }
        }

        public class BodyModel
        {
            public string Content { get; set; }
        }

        public class MentionModel
        {
            public string Key { get; set; }
            public string Id { get; set; }
            public string IdType { get; set; }
            public string Name { get; set; }
            public string TenantKey { get; set; }
        }

        public string MessageId { get; set; }
        public string RootId { get; set; }
        public string ParentId { get; set; }
        public MsgType MsgType { get; set; }
        public string CreateTime { get; set; }
        public string UpdateTime { get; set; }
        public bool Deleted { get; set; }
        public bool Updated { get; set; }
        public string ChatId { get; set; }
        public SenderModel Sender { get; set; }
        public BodyModel Body { get; set; }
        public List<MentionModel> Mentions { get; set; }
        public string UpperMessageId { get; set; }

        public DateTime GetCreateTime()
        {
            return Convert.ToInt64(this.CreateTime).FromUnixTimeMilliseconds();
        }

        public DateTime? GetUpdateTime()
        {
            if (string.IsNullOrEmpty(this.UpdateTime))
            {
                return null;
            }

            return Convert.ToInt64(this.UpdateTime).FromUnixTimeMilliseconds();
        }
    }
}
