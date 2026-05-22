using System;
using System.Text;

namespace AramBenchSwap.Core
{
    public static class LcuAuth
    {
        public static string CreateBasicAuthHeader(string password)
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes("riot:" + password));
            return "Basic " + token;
        }
    }
}
