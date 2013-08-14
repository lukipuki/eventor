using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

public class Test
{
    public static bool Validator (object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }


    public static void Main()
    {
        ServicePointManager.ServerCertificateValidationCallback = Validator;
        string baseUrl = "https://eventor.orientering.se/api/";
        var client = new WebClient();
        string ApiKey = ConfigurationManager.AppSettings["ApiKey"];
        client.Headers.Add("ApiKey", ApiKey);

        try
        {
            var bytes = client.DownloadData(baseUrl + "event/1317");
            string responseString = System.Text.Encoding.UTF8.GetString(bytes);
            System.Console.WriteLine(responseString);
        }
        catch (Exception e)
        {
        }

        // XDocument even = XDocument.Load("event.xml");

        // foreach (var document in even.Element("DocumentList").Elements("Document"))
        // {
        //     string name = document.Attribute("name").Value;
        //     string address = document.Attribute("url").Value;
        //     System.Console.WriteLine(name + " " + address);
        // }
    }
}
