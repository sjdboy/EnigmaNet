using EnigmaNet.Exceptions;
using EnigmaNet.Extensions;
using EnigmaNet.FeiShuOpenApi.Models.Ims;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EnigmaNet.FeiShuOpenApi.Impls
{
    public class ImApiImpl : ApiBase, IImApi
    {
        const string SendApi = "https://open.feishu.cn/open-apis/im/v1/messages";

        string GetReceiveIdTypeValue(ReceiveIdType type)
        {
            switch (type)
            {
                case ReceiveIdType.Email:
                    return "email";
                case ReceiveIdType.OpenId:
                    return "open_id";
                case ReceiveIdType.UserId:
                    return "user_id";
                case ReceiveIdType.UnionId:
                    return "union_id";
                case ReceiveIdType.ChatId:
                    return "chat_id";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        string GetMsgTypeValue(MsgType type)
        {
            switch (type)
            {
                case MsgType.Text:
                    return "text";
                case MsgType.Post:
                    return "post";
                case MsgType.Image:
                    return "image";
                case MsgType.File:
                    return "file";
                case MsgType.Audio:
                    return "audio";
                case MsgType.Media:
                    return "media";
                case MsgType.Sticker:
                    return "sticker";
                case MsgType.Interactive:
                    return "interactive";
                case MsgType.ShareChat:
                    return "share_chat";
                case MsgType.ShareUser:
                    return "share_user";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public async Task<SendResultModel> SendAsync(SendModel model)
        {
            var logger = LoggerFactory.CreateLogger<ImApiImpl>();

            var url = SendApi.AddQueryParam("receive_id_type", GetReceiveIdTypeValue(model.ReceiveIdType));

            string messageContent;

            //发送消息 Content https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/im-v1/message/create_json
            switch (model.MsgType)
            {
                case MsgType.Text:
                    {
                        var messageData = new
                        {
                            Text = model.TextInfo.Text,
                        };

                        messageContent = SerializeObject(messageData);
                    }
                    break;
                case MsgType.Interactive:
                    {
                        var interactiveInfo = model.InteractiveInfo;

                        var data = new
                        {
                            Config = new
                            {
                                EnableForward = interactiveInfo.EnableForward,
                            },
                            Header = new
                            {
                                Title = new
                                {
                                    Tag = InteractiveModel.TextTagType.PlainText,
                                    Content = interactiveInfo.HeaderTitle,
                                },
                                Template = interactiveInfo.HeaderTemplate ?? InteractiveModel.HeaderTemplateType.Blue,
                            },
                            Elements = interactiveInfo.Elements.Select(ele =>
                            {
                                object obj;

                                switch (ele.Type)
                                {
                                    case InteractiveModel.ElementType.Hr:
                                        obj = new
                                        {
                                            Tag = "hr"
                                        };

                                        break;
                                    case InteractiveModel.ElementType.Div:
                                        obj = new
                                        {
                                            Tag = "div",
                                            Text = new
                                            {
                                                Tag = ele.DivInfo.Tag,
                                                Content = ele.DivInfo.Text,
                                            },
                                        };

                                        break;
                                    case InteractiveModel.ElementType.Action:
                                        obj = new
                                        {
                                            Tag = "action",
                                            Layout = ele.ActionInfo.Layout,
                                            Actions = ele.ActionInfo.Actions.Select(a =>
                                            {
                                                switch (a.Type)
                                                {
                                                    case InteractiveModel.ActionElementType.Button:
                                                        return new
                                                        {
                                                            Tag = "button",
                                                            Text = new
                                                            {
                                                                Tag = a.ButtonInfo.Text.Tag,
                                                                Content = a.ButtonInfo.Text.Text,
                                                            },
                                                            Url = a.ButtonInfo.Url,
                                                            Type = a.ButtonInfo.Type ?? InteractiveModel.ButtonType.Default,
                                                        };
                                                    default:
                                                        throw new ArgumentOutOfRangeException(nameof(a.Type));
                                                }
                                            })
                                        };

                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException(nameof(ele.Type));
                                }

                                return obj;
                            })
                        };

                        messageContent = SerializeObject(data);
                    }
                    break;
                default:
                    throw new BizException("还不支持发送该类型消息");
            }

            var requestContent = BuildContent(new
            {
                ReceiveId = model.ReceiveId,
                MsgType = model.MsgType,
                Content = messageContent,
                Uuid = model.Uuid,
            });

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"SendAsync,url:{url} messageContent:{messageContent}");

                var text = await requestContent.ReadAsStringAsync();
                logger.LogTrace($"SendAsync,url:{url} requestContent:{text}");
            }

            var httpClient = GetClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", model.TenantAccessToken);

            var response = await httpClient.PostAsync(url, requestContent);
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"SendAsync,url:{url} statusCode:{response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"SendAsync,url:{url} responseContent:{responseContent}");
            }

            var result = ReadDataResult<SendResultModel>(responseContent);

            ThrowExceptionIfError(result);

            return result.Data;
        }

        public Task DeleteAsync(string messageId)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> DownloadFileAsync(string fileKey)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> DownloadImageAsync(string fileKey)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> DownloadMessageFileAsync(string messageId, string fileKey)
        {
            throw new NotImplementedException();
        }

        public Task<SendResultModel> ReplyAsync(ReplyModel model)
        {
            throw new NotImplementedException();
        }

        public Task<string> UpladImageAsync(byte[] file)
        {
            throw new NotImplementedException();
        }

        public Task<string> UploadFileAsync(UploadFileType fileType, string fileName, byte[] file, int? duration)
        {
            throw new NotImplementedException();
        }
    }
}
