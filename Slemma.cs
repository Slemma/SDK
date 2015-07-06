using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using AppExample;
using System.Web.Script.Serialization;
using System.Collections.Specialized;

namespace Slemma
{
    public class API
    {

        public static string SlemmaURL = "https://perm.slemma.com";

        public string Token;

        public int AppKey;

        public string AppSecret;

        public API(int appKey, string appSecret, string token)
        {
            Token = token;
            AppKey = appKey;
            AppSecret = appSecret;
        }

        public string CalculateSHA1(string text, Encoding enc)
        {
            byte[] buffer = enc.GetBytes(text);
            SHA1CryptoServiceProvider cryptoTransformSHA1 = new SHA1CryptoServiceProvider();
            return BitConverter.ToString(cryptoTransformSHA1.ComputeHash(buffer)).Replace("-", "").ToLower();
        }

        public string Call(string service, string method, object[] parameters)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SlemmaURL + "/api?s=" + service + "&m=" + method);
            request.Headers.Add("sig", CalculateSHA1(AppSecret + Token, Encoding.UTF8));
            request.Headers.Add("appid", AppKey.ToString());
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";


            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i] == null)
                        parameters[i] = "null";
                    else if (parameters[i] is string)
                        parameters[i] = "\"" + ((string)parameters[i]).Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
                }

                byte[] data = Encoding.UTF8.GetBytes("[" + string.Join(",", parameters) + "]");
                request.ContentLength = data.Length;
                Stream rqStream = request.GetRequestStream();
                rqStream.Write(data, 0, data.Length);
            }
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader responseReader = new StreamReader(response.GetResponseStream());
                string fullResponse = responseReader.ReadToEnd();
                response.Close();
                return fullResponse;
            }
            catch (System.Net.WebException e)
            {
                using (StreamReader responseReader = new StreamReader(e.Response.GetResponseStream()))
                {
                    throw new Exception(responseReader.ReadToEnd());
                }
            }
        }

        public string Import(string fullFileName, ImportSettings settings)
        {
            return Import(new StringBuilder(File.ReadAllText(fullFileName)), settings);
        }

        public string Import(StringBuilder csvContent, ImportSettings settings)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SlemmaURL + "/api?s=ImportService&" + settings.getUrlParams());
            request.Headers.Add("sig", CalculateSHA1(AppSecret + Token, Encoding.UTF8));
            request.Headers.Add("appid", AppKey.ToString());
            request.Method = "POST";
            string boundary = "----WebKitFormBoundaryehqugrHf3XnbBJib";
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            StringBuilder text = new StringBuilder();
            if (settings.Fields != null && settings.Fields.Count > 0)
            {
                JavaScriptSerializer s = new JavaScriptSerializer();

                text.Append("------WebKitFormBoundaryehqugrHf3XnbBJib\r\n");
                text.Append("Content-Disposition: form-data; name=\"fields\"\r\n\r\n");

                text.Append(s.Serialize(settings.Fields) + "\r\n");
            }

            text.Append("--" + boundary + "\r\n" +
                "Content-Disposition: form-data; name=\"fileInput_0\"; filename=\"file.csv\"\r\n" +
                "Content-Type: application/vnd.ms-excel\r\n\r\n");

            text.Append(csvContent);
            text.Append("\r\n--" + boundary + "--");

            byte[] data = Encoding.UTF8.GetBytes(text.ToString());
            request.ContentLength = data.Length;
            Stream rqStream = request.GetRequestStream();
            rqStream.Write(data, 0, data.Length);

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader responseReader = new StreamReader(response.GetResponseStream());
                string fullResponse = responseReader.ReadToEnd();
                response.Close();
                return fullResponse;
            }
            catch (System.Net.WebException e)
            {
                using (StreamReader responseReader = new StreamReader(e.Response.GetResponseStream()))
                {
                    throw new Exception(responseReader.ReadToEnd());
                }
            }
        }
    }

    public class ImportSettings
    {
        public string Name;

        public string Delimiter = ",";

        public string DecimalDelimiter = ".";

        public bool WithHeader = true;

        public string NullStr = "";

        public string SheetId;

        public string Charset;

        public int? SchemaKey;

        public int? FolderKey;

        /// <summary>
        /// DMY OR MDY
        /// </summary>
        public string DateStyle;

        public List<Field> Fields;

        public bool AppendData = false;

        public string Escape;

        public string Quota;

        public string AppData;

        public int? TeamKey;

        public string getUrlParams()
        {
            List<string> res = new List<string>();

            if (!string.IsNullOrEmpty(Name))
                res.Add("name=" + Name);

            if (!string.IsNullOrEmpty(Delimiter))
                res.Add("delimiter=" + Delimiter);

            if (WithHeader)
                res.Add("withheader=1");

            if (!string.IsNullOrEmpty(DecimalDelimiter))
                res.Add("decimal_delimiter=" + DecimalDelimiter);

            if (!string.IsNullOrEmpty(NullStr))
                res.Add("nullstr=" + NullStr);

            if (!string.IsNullOrEmpty(Charset))
                res.Add("charset=" + Charset);

            if (!string.IsNullOrEmpty(SheetId))
                res.Add("sheetid=" + SheetId);

            if (!string.IsNullOrEmpty(DateStyle))
                res.Add("ds=" + DateStyle);

            if (SchemaKey != null)
                res.Add("schemakey=" + SchemaKey);

            if (FolderKey != null)
                res.Add("folder=" + FolderKey);

            if (TeamKey != null)
                res.Add("teamkey=" + TeamKey);

            if (AppendData && SchemaKey != null)
                res.Add("append=1");

            if (!string.IsNullOrEmpty(Escape))
                res.Add("escape=" + Escape);

            if (!string.IsNullOrEmpty(Quota))
                res.Add("quota=" + Quota);

            if (!string.IsNullOrEmpty(AppData))
                res.Add("appdata=" + AppData);

            return string.Join("&", res);
        }
    }

    public class Field
    {
        /// <summary>
        /// text,double,timestamp
        /// </summary>
        public string TypeName;

        public string Name;

        /// <summary>
        /// Frequency for calendar dimension hour,daily,weekly,monthly,quarterly,annual
        /// </summary>
        public string Frequency;

        public string Description;
    }

}