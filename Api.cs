using System.Net;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace FidelizadorApiClient
{
    internal class Token {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
    }

    public class Api
    {
        private string Domain;
        private string Schema;
        private string Slug;
        private Token Token;

        private const string Grant = "grant_type=client_credentials&client_id={0}&client_secret={1}";
        private const string UrlTemplate = "{0}://{1}{2}";


        public Api(string domain="api.fidelizador.com", bool https=true)
        {
            Domain = domain;
            Schema = string.Format("http{0}", (https ? "s" : ""));
        }

        public void Autenticate(string slug, string client_id, string client_secret) {
            Slug = slug;
            string url = string.Format(UrlTemplate, Schema, Domain, "/oauth/v2/token");
            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            byte[] buffer = Encoding.ASCII.GetBytes(string.Format(Grant, client_id, client_secret));
            request.GetRequestStream().Write(buffer, 0, buffer.Length);
            string JsonToken = "";
            using (StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                JsonToken = sr.ReadToEnd();
                sr.Close();
            }

            Token = JsonConvert.DeserializeObject<Token>(JsonToken);
        }

        public string Request(string method, string path, NameValueCollection parameters = null) {
            path = (path[0] != '/') ? path = "/" + path : path;
            string url = string.Format(UrlTemplate, Schema, Domain, path);
            WebRequest request = WebRequest.Create(url);
            request.Method = method;
            request.ContentType = "application/json; charset=UTF-8";
            request.Headers.Add("X-Client-Slug", Slug);
            request.Headers.Add("Authorization", string.Format("Bearer {0}", Token.access_token));
            if (method == "POST" && parameters != null)
            {
                request.ContentType = "application/x-www-form-urlencoded";
                string sep = "";
                foreach (string key in parameters.Keys)
                {
                    var value = parameters[key];
                    byte[] buffer = Encoding.ASCII.GetBytes(string.Format("{0}{1}={2}", sep, key, WebUtility.UrlEncode(value)));
                    request.GetRequestStream().Write(buffer, 0, buffer.Length);
                    sep = "&";
                }
            }
            string JsonResponse = "";
            using (StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                JsonResponse = sr.ReadToEnd();
                sr.Close();
            }

            return JsonResponse;
        }
    }
}
