using EnigmaNet.Extensions;
using EnigmaNet.FeiShuOpenApi.Models;
using EnigmaNet.FeiShuOpenApi.Models.Contacts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Impls
{
    public class ContactApiImpl : ApiBase, IContactApi
    {
        class UserResultModel
        {
            public UserModel User { get; set; }
        }

        const string FindUserByDepartmentApi = "https://open.feishu.cn/open-apis/contact/v3/users/find_by_department";
        const string GetChildDepartmentsApi = "https://open.feishu.cn/open-apis/contact/v3/departments/:department_id/children";
        const string UserApi = "https://open.feishu.cn/open-apis/contact/v3/users/:user_id";

        string GetDepartmentIdTypeValue(DepartmentIdType? type)
        {
            if (!type.HasValue)
            {
                return null;
            }

            switch (type.Value)
            {
                case DepartmentIdType.OpenDepartmentId:
                    return "open_department_id";
                case DepartmentIdType.DepartmentId:
                    return "department_id";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        string GetUserIdTypeValue(UserIdType? type)
        {
            if (!type.HasValue)
            {
                return null;
            }

            switch (type.Value)
            {
                case UserIdType.OpenId:
                    return "open_id";
                case UserIdType.UnionId:
                    return "union_id";
                case UserIdType.UserId:
                    return "user_id";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public async Task<PagedList<UserModel>> FindUserByDepartmentAsync(string accessToken, DepartmentIdType? departmentIdType, string? departmentId, int? pageSize, string? pageToken)
        {
            var logger = LoggerFactory.CreateLogger<ContactApiImpl>();

            var url = FindUserByDepartmentApi
                .AddQueryParamIf("department_id_type", GetDepartmentIdTypeValue(departmentIdType), departmentIdType.HasValue)
                .AddQueryParamIf("department_id", departmentId, !string.IsNullOrEmpty(departmentId))
                .AddQueryParamIf("page_size", pageSize?.ToString(), pageSize.HasValue)
                .AddQueryParamIf("page_token", pageToken, !string.IsNullOrEmpty(pageToken));

            var httpClient = GetClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync(url);
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"FindUserByDepartment,url:{url} statusCode:{response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"FindUserByDepartment,url:{url} responseContent:{responseContent}");
            }

            var result = ReadDataResult<PagedList<UserModel>>(responseContent);

            ThrowExceptionIfError(result);

            return result.Data;
        }

        public async Task<PagedList<DepartmentModel>> GetChildDepartmentsAsync(string accessToken, DepartmentIdType? departmentIdType, string? parentDepartmentId, bool? fetchChild, int? pageSize, string? pageToken)
        {
            var logger = LoggerFactory.CreateLogger<ContactApiImpl>();

            var url = GetChildDepartmentsApi.Replace(":department_id", parentDepartmentId)
                .AddQueryParamIf("department_id_type", GetDepartmentIdTypeValue(departmentIdType), departmentIdType.HasValue)
                .AddQueryParamIf("fetch_child", fetchChild?.ToString().ToLower(), fetchChild.HasValue)
                .AddQueryParamIf("page_size", pageSize?.ToString(), pageSize.HasValue)
                .AddQueryParamIf("page_token", pageToken, !string.IsNullOrEmpty(pageToken));

            var httpClient = GetClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync(url);
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetChildDepartments,url:{url} statusCode:{response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetChildDepartments,url:{url} responseContent:{responseContent}");
            }

            var result = ReadDataResult<PagedList<DepartmentModel>>(responseContent);

            ThrowExceptionIfError(result);

            return result.Data;
        }

        public async Task<UserModel> GetUserAsync(string tenantOrUserAccessToken, UserIdType? userIdType, string userId)
        {
            var logger = LoggerFactory.CreateLogger<ContactApiImpl>();

            var url = UserApi.Replace(":user_id", userId)
                .AddQueryParam("user_id_type", GetUserIdTypeValue(userIdType));

            var httpClient = GetClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tenantOrUserAccessToken);

            var response = await httpClient.GetAsync(url);
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserAsync,url:{url} statusCode:{response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserAsync,url:{url} responseContent:{responseContent}");
            }

            var result = ReadDataResult<UserResultModel>(responseContent);

            ThrowExceptionIfError(result);

            return result.Data.User;
        }
    }
}
