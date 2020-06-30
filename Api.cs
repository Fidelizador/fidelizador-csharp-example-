using System.Net;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using Newtonsoft.Json;
using System;
using System.Globalization;

namespace FidelizadorApiClient
{
    internal class Token {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
    }

    public class CsvFile
    {
        public string Filename { get; set; }
        public string ContentType { get; set; }
        public string Body { get; set; }

        public CsvFile(){}
        public CsvFile(string file) {
            ContentType = "text/csv";
            Filename = Path.GetFileName(file);
            Stream fd = File.Open(file, FileMode.Open);
            using (StreamReader sr = new StreamReader(fd)) {
                Body = sr.ReadToEnd();
            }
        }

        public new string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
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

        public string Request(string method, string path, NameValueCollection parameters = null, string[] files = null) {
            path = (path[0] != '/') ? path = "/" + path : path;
            string url = string.Format(UrlTemplate, Schema, Domain, path);
            WebRequest request = WebRequest.Create(url);
            request.Method = method;
            request.ContentType = "application/json; charset=UTF-8";
            request.Headers.Add("X-Client-Slug", Slug);
            request.Headers.Add("Authorization", string.Format("Bearer {0}", Token.access_token));
            if (method == "POST" && parameters != null)
            {
                if (files != null)
                {
                    MultiPartFormData(request, parameters, files);
                }
                else
                {
                    FormUrlEncode(request, parameters);
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

        void FormUrlEncode(WebRequest request, NameValueCollection parameters) {
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

        void MultiPartFormData(WebRequest request, NameValueCollection parameters, string[] files) {
            string file_attribute = "; filename=\"{0}\"";
            string file_content_type = "Content-Type: {0}\n";
            string formdata = "{0}-----------------------------{1}\nContent-Disposition: form-data; name=\"{2}\"{3}\n{4}\n{5}";
            string sep = "";
            string boundary = Guid.NewGuid().ToString().Replace('-', '0').ToUpper();
            request.ContentType = "multipart/form-data; boundary=---------------------------" + boundary;
            string body = "";
            byte[] buffer;
            foreach (string key in parameters.Keys)
            {
                string content_type = "";
                string filename = "";
                var value = parameters[key];
                if (Array.IndexOf(files, key) > -1)
                {
                    CsvFile csv = JsonConvert.DeserializeObject<CsvFile>(value);
                    content_type = string.Format(file_content_type, csv.ContentType);
                    filename = string.Format(file_attribute, csv.Filename);
                    value = csv.Body;
                }
                body += string.Format(formdata, sep, boundary, key, filename, content_type, value);
                sep = "\n";
            }
            body += "\n-----------------------------" + boundary + "--";

            buffer = Encoding.ASCII.GetBytes(body);
            request.GetRequestStream().Write(buffer, 0, buffer.Length);
            body = "";
        }
    }
}
