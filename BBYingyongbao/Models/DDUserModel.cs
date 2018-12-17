using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBYingyongbao.Models
{
    //used in receive parameter
    public class DDUserModel
    {
        public string DDUserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ERPUserId { get; set; }

        public string ToDictionaryString(DDUserModel model) {
            return "DDUserId: " + model.DDUserId + " Username:" + model.Username + " Password:" + model.Password + " ERPUserId:" + model.ERPUserId;
        }
    }
}
