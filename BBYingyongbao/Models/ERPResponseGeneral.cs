using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBYingyongbao.Models
{
    public class ERPResponseGeneral
    {
        public string statusCode { get; set; }

        public string errorMessage { get; set; }

        public string totalCount { get; set; }

        public string totalPage { get; set; }

        public string ReturnKey { get; set; }

        public ERPResponseGeneral serializeRequestDataResponse(JObject obj) {
            return new ERPResponseGeneral() {
                statusCode = obj["statusCode"].ToString(),
                errorMessage=obj["errorMessage"].ToString(),
                totalCount=obj["totalCount"].ToString(),
                totalPage=obj["totalPage"].ToString()
            };
        }

        public ERPResponseGeneral serializeSaveDataResponse(JObject obj) {
            return new ERPResponseGeneral()
            {
                statusCode = obj["statusCode"].ToString(),
                errorMessage = obj["errorMessage"].ToString(),
                totalCount = obj["returnkey"].ToString()
            };
        }

    }

    public enum ERPStatusCode {
        IncorrectParameter=-9,
        NoData=0,
        Success=1
    }
}
