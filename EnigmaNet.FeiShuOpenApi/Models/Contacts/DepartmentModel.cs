using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Models.Contacts
{
    public class DepartmentModel
    {
        public class StatusModel
        {
            public bool IsDeleted { get; set; }
        }

        public class LeaderModel
        {
            public int LeaderType { get; set; }
            public string LeaderID { get; set; }
        }

        public string Name { get; set; }
        public string ParentDepartmentId { get; set; }
        public string DepartmentId { get; set; }
        public string OpenDepartmentId { get; set; }
        public string LeaderUserId { get; set; }
        public string ChatId { get; set; }
        public string Order { get; set; }
        public int MemberCount { get; set; }
        public StatusModel Status { get; set; }
        public bool CreateGroupChat { get; set; }
        public List<LeaderModel> Leaders { get; set; }
    }
}
