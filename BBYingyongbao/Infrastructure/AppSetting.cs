using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBYingyongbao.Infrastructure
{
    public class AppSetting
    {
        public ERPAPI api { get; set; }
    }

    public class ERPAPI {
        public string Url { get; set; }
    }
}
