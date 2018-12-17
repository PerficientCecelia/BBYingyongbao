using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBYingyongbao.Models
{
    public class UserInfoViewModel
    {
        public string ERPUserID { get; set; }
        public string DDUserID { get; set; }
        public string ERPUserName { get; set; }
        public string WorkNumber { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string IfClosed { get; set; }
        public string returnStyle { get; set; }

        public void ToUserInfoViewModel(UserInfoViewModel model,JToken obj) {
            model.ERPUserID = obj["OLAPKey"]!=null?  obj["OLAPKey"].ToString():"";
            model.DDUserID = obj["Support14"]!=null?obj["Support14"].ToString():"";
            model.ERPUserName = obj["MainDemoName"]!=null? obj["MainDemoName"].ToString():"";
            model.WorkNumber = obj["SourceKey"]!=null? obj["SourceKey"].ToString():"";
            model.DepartmentId = obj["FatherKey"]!=null? obj["FatherKey"].ToString():"";
            model.DepartmentName = obj["FatherKeyValue"]!=null? obj["FatherKeyValue"].ToString():"";
            model.IfClosed = obj["IfCloseKey"]!=null? obj["IfCloseKey"].ToString():"";  
        }
    }
}
