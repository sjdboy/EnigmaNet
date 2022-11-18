using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Models.Contacts
{
    public class UserModel
    {
        public class StatusModel
        {
            public bool IsFrozen { get; set; }
            public bool IsResigned { get; set; }
            public bool IsActivated { get; set; }
            public bool IsExited { get; set; }
            public bool IsUnjoin { get; set; }
        }

        public string UnionId { get; set; }
        public string UserId { get; set; }
        public string OpenId { get; set; }
        public string Name { get; set; }
        public string EnName { get; set; }
        public string Nickname { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public bool MobileVisible { get; set; }
        public int Gender { get; set; }
        public string AvatarKey { get; set; }
        public AvatarModel Avatar { get; set; }
        public StatusModel Status { get; set; }
        public string[] DepartmentIds { get; set; }
        public string LeaderUserId { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string WorkStation { get; set; }
        public int JoinTime { get; set; }
        public bool IsTenantManager { get; set; }
        public string EmployeeNo { get; set; }
        public int EmployeeType { get; set; }
        public string EnterpriseEmail { get; set; }
        public string JobTitle { get; set; }
        public bool IsFrozen { get; set; }
    }
}
