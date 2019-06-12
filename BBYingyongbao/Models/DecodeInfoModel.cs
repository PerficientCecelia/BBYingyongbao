using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBYingyongbao.Models
{
    public class DecodeInfoModel
    {
        public string encryptedDataPlain { get; set; }

        public string iv { get; set; }

        public string openId { get; set; }
    }
}
