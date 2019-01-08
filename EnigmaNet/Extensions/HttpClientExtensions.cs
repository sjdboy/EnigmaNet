using EnigmaNet.Exceptions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Extensions
{
    public static class HttpClientExtensions
    {
        class ApiErrorResultModel
        {
            public string Message { get; set; }
        }

        static async Task<TResult> GetResultByResponseAsync<TResult>(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var errorMessage = await response.Content.ReadAsAsync<ApiErrorResultModel>();
                throw new BizException(errorMessage.Message);
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<TResult>();
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception($"api 响应错误，code:{response.StatusCode.ToString("D")} content:{content}");
            }
        }

        static async Task CheckResultByResponseAsync(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var errorMessage = await response.Content.ReadAsAsync<ApiErrorResultModel>();
                throw new BizException(errorMessage.Message);
            }

            if (response.IsSuccessStatusCode)
            {
                return;
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception($"api 响应错误，code:{response.StatusCode.ToString("D")} content:{content}");
            }
        }

        public static async Task GetObjectAsync(this HttpClient httpClient, string url)
        {
            var response = await httpClient.GetAsync(url);

            await CheckResultByResponseAsync(response);
        }

        public static async Task<TResult> GetObjectAsync<TResult>(this HttpClient httpClient, string url)
        {
            var response = await httpClient.GetAsync(url);

            return await GetResultByResponseAsync<TResult>(response);
        }

        public static async Task DeleteObjectAsync(this HttpClient httpClient,string url)
        {
            var response = await httpClient.DeleteAsync(url);

            await CheckResultByResponseAsync(response);
        }

        /// <summary>
        /// Post提交数据
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="url"></param>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        public static async Task<TResult> PostObjectAsync<TRequest, TResult>(this HttpClient httpClient, string url, TRequest requestModel)
        {
            //var response = await httpClient.PostAsJsonAsync(url, requestModel);
            var response = await httpClient.PostAsync(url, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestModel), Encoding.UTF8, "application/json"));

            return await GetResultByResponseAsync<TResult>(response);
        }

        /// <summary>
        /// Post提交数据
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <param name="url"></param>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        public static async Task PostObjectAsync<TRequest>(this HttpClient httpClient, string url, TRequest requestModel)
        {
            //var response = await httpClient.PostAsJsonAsync(url, requestModel);
            var response = await httpClient.PostAsync(url, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestModel), Encoding.UTF8, "application/json"));

            await CheckResultByResponseAsync(response);
        }

        /// <summary>
        /// Post提交数据
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="url"></param>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        public static async Task<TResult> PutObjectAsync<TRequest, TResult>(this HttpClient httpClient, string url, TRequest requestModel)
        {
            //var response = await httpClient.PostAsJsonAsync(url, requestModel);
            var response = await httpClient.PutAsync(url, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestModel), Encoding.UTF8, "application/json"));

            return await GetResultByResponseAsync<TResult>(response);
        }

        /// <summary>
        /// Post提交数据
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <param name="url"></param>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        public static async Task PutObjectAsync<TRequest>(this HttpClient httpClient, string url, TRequest requestModel)
        {
            //var response = await httpClient.PostAsJsonAsync(url, requestModel);
            var response = await httpClient.PutAsync(url, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestModel), Encoding.UTF8, "application/json"));

            await CheckResultByResponseAsync(response);
        }
    }
}
