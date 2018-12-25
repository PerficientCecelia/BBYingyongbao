using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBYingyongbao.Common
{
    public static class DatetimeUtil
    {
        public static string ConvertDateTime(DateTime time) {
            return time.ToString("yyyy-MM-dd");
        }
    }
}
