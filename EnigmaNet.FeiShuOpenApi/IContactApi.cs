using EnigmaNet.FeiShuOpenApi.Models;
using EnigmaNet.FeiShuOpenApi.Models.Contacts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi
{
    public interface IContactApi
    {
        /// <summary>
        /// 获取子部门列表
        /// </summary>
        /// <param name="tenantOrUserAccessToken"></param>
        /// <param name="departmentIdType"></param>
        /// <param name="parentDepartmentId">上级部门id,顶级部门为0</param>
        /// <param name="fetchChild"></param>
        /// <param name="pageSize">页大小，最大50</param>
        /// <param name="pageToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/department/children
        /// </remarks>
        Task<PagedList<DepartmentModel>> GetChildDepartmentsAsync(string tenantOrUserAccessToken, DepartmentIdType? departmentIdType, string? parentDepartmentId, bool? fetchChild, int? pageSize, string? pageToken);

        /// <summary>
        /// 获取部门直属用户列表
        /// </summary>
        /// <param name="tenantOrUserAccessToken"></param>
        /// <param name="departmentIdType"></param>
        /// <param name="departmentId"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/user/find_by_department
        /// </remarks>
        Task<PagedList<UserModel>> FindUserByDepartmentAsync(string tenantOrUserAccessToken, DepartmentIdType? departmentIdType, string? departmentId, int? pageSize, string? pageToken);

        /// <summary>
        /// 获取单个用户信息
        /// </summary>
        /// <param name="tenantOrUserAccessToken"></param>
        /// <param name="userIdType"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/user/get
        /// </remarks>
        Task<UserModel> GetUserAsync(string tenantOrUserAccessToken, UserIdType? userIdType, string userId);
    }
}
