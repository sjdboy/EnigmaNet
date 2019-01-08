using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.QCloud.CMQ.Utils
{
    class Actions
    {
        public const string CreateQueue = "CreateQueue";
        public const string SendMessage = "SendMessage";
        public const string ReceiveMessage = "ReceiveMessage";
        public const string DeleteMessage = "DeleteMessage";

        public const string CreateTopic = "CreateTopic";
        public const string Subscribe = "Subscribe";
        public const string PublishMessage = "PublishMessage";
    }
}
