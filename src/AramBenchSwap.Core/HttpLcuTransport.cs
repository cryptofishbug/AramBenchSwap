using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AramBenchSwap.Core
{
    public sealed class HttpLcuTransport : ILcuTransport
    {
        public LcuResponse Send(string method, string url, string password, string body)
        {
            try
            {
                var request = CreateRequest(method, url, password);
                if (body != null)
                {
                    var bytes = Encoding.UTF8.GetBytes(body);
                    request.ContentType = "application/json";
                    request.ContentLength = bytes.Length;
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
                else if (method == "POST" || method == "PUT" || method == "PATCH")
                {
                    request.ContentLength = 0;
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream ?? Stream.Null))
                {
                    return new LcuResponse((int)response.StatusCode, reader.ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response == null)
                {
                    return new LcuResponse(0, ex.Message);
                }

                using (response)
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream ?? Stream.Null))
                {
                    return new LcuResponse((int)response.StatusCode, reader.ReadToEnd());
                }
            }
        }

        public byte[] GetBytes(string url, string password)
        {
            var request = CreateRequest("GET", url, password);
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var memory = new MemoryStream())
            {
                if (stream != null)
                {
                    stream.CopyTo(memory);
                }

                return memory.ToArray();
            }
        }

        private static HttpWebRequest CreateRequest(string method, string url, string password)
        {
            ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | (SecurityProtocolType)3072;
            ServicePointManager.ServerCertificateValidationCallback = ValidateLocalLcuCertificate;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Timeout = 1000;
            request.ReadWriteTimeout = 1000;
            request.Headers[HttpRequestHeader.Authorization] = LcuAuth.CreateBasicAuthHeader(password);
            return request;
        }

        public static bool IsLocalLcuUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return false;
            }

            return uri.Scheme == Uri.UriSchemeHttps &&
                (uri.Host == "127.0.0.1" || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase));
        }

        private static bool ValidateLocalLcuCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            var request = sender as HttpWebRequest;
            return request != null && IsLocalLcuUrl(request.RequestUri.ToString());
        }
    }
}
