using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBYingyongbao.Models
{
    public class WeiXinUserInfo
    {
        public string UnionId { get; set; }

        public string OpenId { get; set; }

        public string ErrorMessage { get; set; }

        public string SessionKey { get; set; }
        
        public dynamic DecodedData { get; set; }

        public WeiXinUserInfo Convert(string Unionid, string OpenId,string SessionKey, string ErrorMessage) {
            return new WeiXinUserInfo()
            {
                UnionId = Unionid,
                OpenId = OpenId,
                ErrorMessage=ErrorMessage,
                SessionKey= SessionKey
            };
        }

        public WeiXinUserInfo Convert(object DecodedData)
        {
            return new WeiXinUserInfo()
            {
                DecodedData = DecodedData
            };
        }
    }
}
