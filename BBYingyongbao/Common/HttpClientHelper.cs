using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BBYingyongbao.Common
{
    public static class HttpClientHelper
    {
        public static HttpClient GetClient(Uri url, bool requestWithPatch = false)
        {
            // var authValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
            var client = new HttpClient()
            {
                BaseAddress = url
            };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json",1.0));
            if (requestWithPatch)
                client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "PATCH");
            return client;
        }
    }
}
