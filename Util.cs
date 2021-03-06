using System;
using System.Net;
using System.Net.Security;
using System.Linq;
using System.Xml.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;

namespace Eventor
{
    /**
     * Various functions helping parsing XML.
     */
    public static class Util
    {
        public static int? IntFromElementNullable(string name, XElement el)
        {
            if (el == null || el.Element(name) == null) return null;
            try
            {
                return IntFromElement(name, el);
            }
            catch (Exception) {
                return null;
            }
        }

        public static int IntFromElement(string name, XElement el)
        {
            return int.Parse(el.Element(name).Value);
        }

        public static double? DoubleFromElement(string name, XElement el)
        {
            if (el == null || el.Element(name) == null)
                return null;
            double res = 0;
            double.TryParse(el.Element(name).Value.Split()[0], out res);
            return res;
        }

        public static DateTime DateFromElement(XElement el)
        {
            return DateTime.Parse(el.Element("Date").Value + " " + el.Element("Clock").Value);
        }

        public static DateTime? DateFromElementNullable(XElement el)
        {
            if (el == null)
                return null;
            return DateFromElement(el);
        }

        public static TimeSpan TimeFromElement(string name, XElement el)
        {
            if (el.Element(name) == null)
                return TimeSpan.FromSeconds(0);
            string[] val = el.Element(name).Value.Split(':');
            return TimeSpan.FromSeconds(60 * int.Parse(val[0]) + int.Parse(val[1]));
        }

        public static string StringFrom(XElement el)
        {
            if (el == null)
                return null;
            return el.Value.Trim();
        }

        public static Tuple<string, string> NameFrom(XElement nameEl)
        {
            string givenName = string.Join(" ", nameEl.Elements("Given").Select(x => x.Value.Trim()));
            string familyName = Util.StringFrom(nameEl.Element("Family"));
            return Tuple.Create(givenName, familyName);
        }

        static bool Validator (object sender, X509Certificate certificate, X509Chain chain,
                SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static WebClient _client = null;
        private static WebClient Client
        {
            get
            {
                if (_client == null)
                {
                    ServicePointManager.ServerCertificateValidationCallback = Validator;
                    string ApiKey = ConfigurationManager.AppSettings["ApiKey"];
                    _client = new WebClient();
                    _client.Headers.Add("ApiKey", ApiKey);
                }
                return _client;
            }
        }

        private static string baseUrl = "https://eventor.orientering.se/api/";
        public static XDocument DownloadXml(string url)
        {
            string xml = Client.DownloadString(baseUrl + url);
            return XDocument.Parse(xml);
        }
    }
}
