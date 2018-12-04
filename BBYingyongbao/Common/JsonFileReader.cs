using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BBYingyongbao.Common
{
    public static class JsonFileReader
    {
        public static Dictionary<string, string> ReadToDictionary(string url) {
            using (StreamReader r = new StreamReader(url))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
        }

        public static JObject ReadTOJson(string url) {
            using (StreamReader r = new StreamReader(url))
            {
                string json = r.ReadToEnd();
                return JObject.Parse(json);
            }
        }
    }
}
