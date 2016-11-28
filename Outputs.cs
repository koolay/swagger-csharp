using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Policy;
using System.Text;

namespace SwaggerSharp
{
    public interface IOutputer
    {

        void Output(string swaggerJson);
    }

    public class Stdoutputer : IOutputer
    {
        public void Output(string swaggerJson)
        {
            Console.Write(swaggerJson);
        }
    }

    public class JsonOutputer : IOutputer
    {
        private readonly string _output;

        public JsonOutputer(string output)
        {
            _output = output;
        }

        public void Output(string swaggerJson)
        {
            var dir = Path.GetDirectoryName(_output);
            if (!Directory.Exists(dir))
            {
                Console.WriteLine(dir + "不存在");
                Environment.Exit(0);
            }
            Console.WriteLine("output to file:" + _output);
            File.WriteAllText(_output, swaggerJson);
        }
    }

    public class APIOutputer : IOutputer
    {
        private readonly string _output;
        private readonly IEnumerable<string> _headers;

        public APIOutputer(string output, IEnumerable<string> headers)
        {
            _output = output;
            _headers = headers;
        }

        public void Output(string swaggerJson)
        {

            var request = WebRequest.CreateHttp(_output);
            if (_headers != null)
            {
                foreach (var header in _headers)
                {
                    var h = header.Split('=');
                    if (h.Length != 2)
                    {
                        Console.WriteLine("header格式无效,格式应该是a=v");
                        Environment.Exit(0);
                    }
                    request.Headers.Add(h[0].Trim(), h[1].Trim());
                }
            }
            request.Method = "PUT";
            request.AllowWriteStreamBuffering = false;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "Accept=application/json";
            request.SendChunked = false;
            var payload = "swagger=" + WebUtility.UrlEncode(swaggerJson);
            var bytes = Encoding.GetEncoding("utf-8").GetBytes(payload);
            request.ContentLength = bytes.Length;
            using (var writeStream = request.GetRequestStream())
            {
                writeStream.Write(bytes, 0, bytes.Length);
            }

            WebResponse response;
            try
            {
                response = request.GetResponse();
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(0);
                return;
            }

            using (var httpResponse = (HttpWebResponse) response)
            {
                var responseValue = string.Empty;

                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    var message = $"Request failed. Received HTTP {httpResponse.StatusCode}";
                    Console.WriteLine(message);
                    Environment.Exit(0);
                }

                using (var responseStream = httpResponse.GetResponseStream())
                {
                    if (responseStream != null)
                        using (var reader = new StreamReader(responseStream))
                        {
                            responseValue = reader.ReadToEnd();
                        }
                }
                Console.Write(responseValue);
            }

        }
    }
}