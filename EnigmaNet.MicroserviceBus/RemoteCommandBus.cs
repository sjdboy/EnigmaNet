using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using IdentityModel.Client;

using EnigmaNet.Bus;
using EnigmaNet.Bus.Impl;
using EnigmaNet.Exceptions;
using EnigmaNet.Extensions;

namespace EnigmaNet.MicroserviceBus
{
    public class RemoteCommandBus : CommandBus
    {
        class MessageModel
        {
            public string Code { get; set; }
            public string Message { get; set; }
        }

        ILogger _logger;
        RemoteCommandBusOptions _optionsValue;
        IHttpClientFactory _httpClientFactory { get; set; }

        string _accessToken;
        DateTime? _expiredDateTime;
        DiscoveryDocumentResponse _cachedDiscoveryDocumentResponse;

        bool _startTokenCheckTask = false;
        object _startTokenCheckTaskLocker = new object();

        async Task<TokenResponse> ApplyTokenAsync()
        {
            var idsClient = _httpClientFactory.CreateClient();

            _logger.LogTrace($"ApplyToken,start discovery doc,url:{_optionsValue.IdentityServerUrl}");

            var discovery = await idsClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _optionsValue.IdentityServerUrl,
                Policy = new DiscoveryPolicy { RequireHttps = false },
            });

            _logger.LogTrace("ApplyToken,finish discovery doc");

            if (discovery.IsError)
            {
                _logger.LogTrace($"ApplyToken,discovery doc error, error:{discovery.Error}");

                if (_cachedDiscoveryDocumentResponse != null)
                {
                    discovery = _cachedDiscoveryDocumentResponse;
                }
                else
                {
                    throw new Exception($"ids discovery error,url:{_optionsValue.IdentityServerUrl} error:{discovery.Error} raw:{discovery.Raw}", discovery.Exception);
                }
            }
            else
            {
                _cachedDiscoveryDocumentResponse = discovery;
            }

            _logger.LogTrace("ApplyToken,start request token");

            var tokenResponse = await idsClient.RequestClientCredentialsTokenAsync(
                new ClientCredentialsTokenRequest
                {
                    ClientId = _optionsValue.ClientId,
                    ClientSecret = _optionsValue.ClientSecret,
                    ClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader,
                    Address = discovery.TokenEndpoint,
                });

            _logger.LogTrace("ApplyToken,finish request token");

            if (tokenResponse.IsError)
            {
                _logger.LogTrace($"ApplyToken,request token error,error:{tokenResponse.Error}");

                throw new Exception($"ids request token error:{tokenResponse.Error}", tokenResponse.Exception);
            }

            return tokenResponse;
        }

        void CheckToken()
        {
            _logger.LogTrace("start CheckToken");

            while (true)
            {
                if (string.IsNullOrEmpty(_optionsValue?.IdentityServerUrl) ||
                    string.IsNullOrEmpty(_optionsValue?.ClientId) ||
                    string.IsNullOrEmpty(_optionsValue?.ClientSecret))
                {
                    _logger.LogTrace("CheckToken, options is empty, wait a moment and continue");

                    Thread.Sleep(1000 * 5);
                    continue;
                }

                if (_expiredDateTime.HasValue)
                {
                    var seconds = (_expiredDateTime.Value - DateTime.Now).TotalSeconds;

                    if (seconds > _optionsValue.RefreshTokenRemainingSeconds)
                    {
                        var waitSeconds = Convert.ToInt32(Math.Ceiling(seconds - 5)).IfLessOrEqualBecome(5, 5);

                        _logger.LogTrace($"CheckToken, timeGap more than {_optionsValue.RefreshTokenRemainingSeconds}s, wait a moment({waitSeconds}s) and continue");

                        Thread.Sleep(1000 * waitSeconds);
                        continue;
                    }
                }

                _logger.LogTrace("CheckToken, no token or timeGap less than 60, start apply token");

                try
                {
                    var token = ApplyTokenAsync().Result;

                    _accessToken = token.AccessToken;
                    _expiredDateTime = DateTime.Now.AddSeconds(token.ExpiresIn);

                    _logger.LogTrace($"CheckToken, apply token success, save token, accessToken:{token.AccessToken} expiresIn:{token.ExpiresIn}");

                    Thread.Sleep(1000 * 10);

                    //var waitSecond = (token.ExpiresIn - 20).IfLessThanBecome(10, 10);
                    //_logger.LogTrace($"CheckToken, wait:{waitSecond}s");
                    //Thread.CurrentThread.Join(1000 * waitSecond);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CheckToken, apply token error, wait a moment and continue");
                    Thread.Sleep(1000 * 5);
                }
            }
        }

        async Task<string> GetTokenAsync()
        {
            StartTokenCheckIfNot();

            if (string.IsNullOrEmpty(_optionsValue.IdentityServerUrl) ||
                string.IsNullOrEmpty(_optionsValue.ClientId) ||
                string.IsNullOrEmpty(_optionsValue.ClientSecret))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(_accessToken))
            {
                return _accessToken;
            }

            _logger.LogTrace("GetTokenAsync,token is empty and start apply token");

            var token = await ApplyTokenAsync();

            _logger.LogTrace($"GetTokenAsync,apply token success and save token, accessToken:{token.AccessToken} expiresIn:{token.ExpiresIn}");

            _accessToken = token.AccessToken;
            _expiredDateTime = DateTime.Now.AddSeconds(token.ExpiresIn);

            return _accessToken;
        }

        string GetGatewayUrl()
        {
            var millionSecond = DateTime.Now.Millisecond;

            if (!(_optionsValue.GamewayUrls?.Count > 0))
            {
                throw new ArgumentNullOrEmptyException(nameof(_optionsValue.GamewayUrls));
            }

            if (_optionsValue.GamewayUrls.Count == 1)
            {
                return _optionsValue.GamewayUrls.First();
            }

            var index = millionSecond % _optionsValue.GamewayUrls.Count;

            return _optionsValue.GamewayUrls[index];
        }

        string GetServiceName(Type commandType)
        {
            var assemblyName = commandType.Assembly.GetName().Name;

            string serviceNameSuffix = "Service"; ;
            string commandsSuffix = ".Commands";
            if (assemblyName.EndsWith(commandsSuffix))
            {
                return assemblyName.Substring(0,assemblyName.Length- commandsSuffix.Length ) + serviceNameSuffix;
            }
            else
            {
                return assemblyName + serviceNameSuffix;
            }
        }

        async Task<TResult> ExecuteRemoteAsync<TResult>(ICommand<TResult> command)
        {
            var gatewayUrl = GetGatewayUrl();

            var token = await GetTokenAsync();

            var commandType = command.GetType();

            var serviceName = GetServiceName(commandType);

            var typeString = commandType.FullName;

            var url = $"{gatewayUrl}/{serviceName}/{typeString}";

            var requestId = Guid.NewGuid().ToString();

            HttpClient httpClient;

            if (!string.IsNullOrEmpty(_optionsValue.HttpClientName))
            {
                httpClient = _httpClientFactory.CreateClient(_optionsValue.HttpClientName);
            }
            else
            {
                httpClient = _httpClientFactory.CreateClient();
            }

            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            }

            var commandString = JsonConvert.SerializeObject(command, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, });

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace($"start execute remote command,requestId:{requestId} url:{url} token:{token} con:{commandString}");
            }

            var response = await httpClient.PostAsync(url, new StringContent(commandString));

            //var response = await Policy
            //    .Handle<HttpRequestException>()
            //    //.RetryAsync(1, (ex, count) => { })
            //    .OrResult<HttpResponseMessage>(msg =>
            //    {
            //        return msg.StatusCode == HttpStatusCode.InternalServerError;
            //    })
            //    .RetryAsync(1, (msg, retryCount) =>
            //    {

            //    })
            //    .ExecuteAsync(async () =>
            //    {
            //        return await httpClient.PostAsync(url, new StringContent(commandString));
            //    });

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace($"execute remote command response,requestId:{requestId} statusCode:{response.StatusCode} ReasonPhrase:{response.ReasonPhrase} mediaType:{response.Content?.Headers?.ContentType?.MediaType}");

                var content = await response.Content.ReadAsStringAsync();

                _logger.LogTrace($"execute remote command response,requestId:{requestId} con:{content}");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                if (typeof(TResult) == typeof(Empty))
                {
                    return default;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return await response.Content.ReadAsAsync<TResult>();
                }
                else
                {
                    return default;
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                string messageText;

                if (response.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    var message = await response.Content.ReadAsAsync<MessageModel>();
                    messageText = message.Message;
                }
                else
                {
                    messageText = response.ReasonPhrase;
                }

                throw new BizException(messageText);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                string messageText;

                if (response.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    var message = await response.Content.ReadAsAsync<MessageModel>();
                    messageText = message.Message;
                }
                else
                {
                    messageText = response.ReasonPhrase;
                }

                throw new ArgumentException(messageText);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                string messageText;

                if (response.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    var message = await response.Content.ReadAsAsync<MessageModel>();
                    messageText = message.Message;
                }
                else if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    messageText = response.ReasonPhrase;
                }
                else
                {
                    messageText = $"remote service internal server error,requestId:{requestId} command type:{typeString} http code:{response.StatusCode}";
                }

                _logger.LogError($"remote service internal server error,requestId:{requestId} command type:{typeString} http code:{response.StatusCode} msg:{messageText} url:{url}");

                throw new Exception(messageText);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError($"remote service response Unauthorized,requestId:{requestId} command type:{typeString} http code:{response.StatusCode} url:{url}");

                throw new Exception($"remote server response unauthorized,requestId:{requestId} command type:{typeString} http code:{response.StatusCode}");
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogError($"remote service response Forbidden,requestId:{requestId} command type:{typeString} http code:{response.StatusCode} url:{url}");

                throw new Exception($"remote server response forbidden,requestId:{requestId} command type:{typeString} http code:{response.StatusCode}");
            }
            else
            {
                _logger.LogError($"remote service response error,requestId:{requestId} command type:{typeString} http code:{response.StatusCode} url:{url}");

                throw new Exception($"remote command handler error,requestId:{requestId} command type:{typeString} http code:{response.StatusCode}");
            }
        }

        bool CheckAllowRemote(string commandTypeName)
        {
            if (_optionsValue.ForbidRemoteCommands?.Count > 0)
            {
                if (_optionsValue.ForbidRemoteCommands.Any(m => m == "*"))
                {
                    return false;
                }

                if (_optionsValue.ForbidRemoteCommands.Any(m => m == commandTypeName))
                {
                    return false;
                }

                foreach (var item in _optionsValue.ForbidRemoteCommands.Where(m => m.EndsWith("*")))
                {
                    var forbidTypePrefix = item.TrimEnd('*');
                    if (commandTypeName.StartsWith(forbidTypePrefix))
                    {
                        return false;
                    }
                }
            }

            if (_optionsValue.AllowRemoteCommands?.Count > 0)
            {
                if (_optionsValue.AllowRemoteCommands.Any(m => m == "*"))
                {
                    return true;
                }

                if (_optionsValue.AllowRemoteCommands.Any(m => m == commandTypeName))
                {
                    return true;
                }

                foreach (var item in _optionsValue.AllowRemoteCommands.Where(m => m.EndsWith("*")))
                {
                    var allowTypePrefix = item.TrimEnd('*');
                    if (commandTypeName.StartsWith(allowTypePrefix))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public RemoteCommandBus(
            ILoggerFactory loggerFactory,
            IOptionsMonitor<RemoteCommandBusOptions> options,
            IHttpClientFactory httpClientFactory) :
            base(loggerFactory.CreateLogger<CommandBus>())
        {
            _logger = loggerFactory.CreateLogger<RemoteCommandBus>();
            _httpClientFactory = httpClientFactory;
            _optionsValue = options.CurrentValue;
            options.OnChange(newOptions =>
            {
                if (_optionsValue.ClientId != newOptions.ClientId ||
                    _optionsValue.ClientSecret != newOptions.ClientSecret ||
                    _optionsValue.IdentityServerUrl != newOptions.IdentityServerUrl)
                {
                    //clear token
                    _accessToken = null;
                    _expiredDateTime = null;
                    _cachedDiscoveryDocumentResponse = null;
                }

                _optionsValue = newOptions;
            });
        }

        public override Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command)
        {
            var type = command.GetType();

            Handlers.TryGetValue(type, out object handler);

            if (handler != null)
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace($"match handler,command type:{type} handler:{handler.GetType()}");
                }

                var methodInfo = HandlerMethods.GetValueOrDefault(type);

                var task = (Task<TResult>)(methodInfo.Invoke(handler, new object[] { command }));

                return task;
            }

            if (!CheckAllowRemote(command.GetType().FullName))
            {
                _logger.LogTrace($"not allow execute remote command , type:{type}");

                throw new NotImplementedException($"no handler for '{type}'");
            }
            else
            {
                _logger.LogTrace($"start execute remote command , type:{type}");
                return ExecuteRemoteAsync(command);
            }
        }

        public void StartTokenCheckIfNot()
        {
            if (_startTokenCheckTask)
            {
                return;
            }

            lock (_startTokenCheckTaskLocker)
            {
                if (_startTokenCheckTask)
                {
                    return;
                }

                _startTokenCheckTask = true;
            }

            var thread = new Thread(CheckToken);
            thread.IsBackground = true;
            thread.Start();
        }
    }
}
