using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBYingyongbao.Models
{
    public class OrderCalendarViewModel
    {
        public string Day { get; set; }

        public int TodoCount { get; set; }

        public List<OrderDetailViewModel> list { get; set; }
    }

}
