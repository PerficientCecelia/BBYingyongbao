using BBYingyongbao.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBYingyongbao.Models
{
    public class OrderDetailViewModel
    {
        public string ContractNumber { get; set; }

        public string OrderNumber { get; set; }

        public string ProjectNumber { get; set; }

        public string ClientName { get; set; }

        public string ProjectName { get; set; }

        public DateTime PlanedStartTime { get; set; }

        public string PlanedStartTimeDay { get { return DatetimeUtil.ConvertDateTime( PlanedStartTime); } }

        public DateTime PlanedEndTime { get; set; }

        public DateTime RealStartTime { get; set; }

        public DateTime RealEndTime { get; set; }

        public string WorkerId { get; set; }

        public string WorkerName { get; set; }

        public string ServiceReport { get; set; }

        public string Rators { get; set; }

        public bool Passed { get; set; }

        public string DepartmentId { get; set; }

        public string DepartmentName { get; set; }
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public DateTime GenerateTime { get; set; }
    }
}
