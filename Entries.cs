using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Configuration;

public class Class
{
    public string name { get; set; }
    public int numOfPeople { get; set; }
    public List<Tuple<string, string>> people { get; set; }
    public Class(string _name, int _numOfPeople, List<Tuple<string, string>> _people)
    {
        name = _name;
        numOfPeople = _numOfPeople;
        people = _people;
    }
}

public class Test
{
    public static bool Validator (object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }


    public static void Main()
    {
        string ApiKey = ConfigurationManager.AppSettings["ApiKey"];
        ServicePointManager.ServerCertificateValidationCallback = Validator;
        string baseUrl = "https://eventor.orientering.se/api/";
        var client = new WebClient();
        client.Headers.Add("ApiKey", ApiKey);

        try
        {
            var bytes = client.DownloadData(baseUrl +
                "entries?organisationIds=636&eventIds=1317");
            string responseString = System.Text.Encoding.UTF8.GetString(bytes);
            System.Console.WriteLine(responseString);
        }
        catch (Exception e)
        {
        }
/*
 * 
 *         XDocument startlist = XDocument.Load("startlists.xml");
 *         string competitionName = startlist.Element("StartList").Element("Event").Element("Name").Value;
 *         System.Console.WriteLine(competitionName);
 * 
 *         Dictionary<string, List<Class>> dClass = new Dictionary<string, List<Class>> ();
 *         foreach (var elem in startlist.Element("StartList").Elements("ClassStart"))
 *         {
 *             Dictionary<string, List<Tuple<string, string>>> dict = new Dictionary<string, List<Tuple<string, string>>> ();
 * 
 *             foreach (var personSta in elem.Elements("PersonStart"))
 *             {
 *                 XElement nameEl = personSta.Element("Person").Element("PersonName");
 *                 string name = nameEl.Element("Given").Value + " " + nameEl.Element("Family").Value;
 * 
 *                 foreach (XElement raceSta in personSta.Elements("RaceStart"))
 *                 {
 *                     string res = raceSta.Element("Start").Element("StartTime").Element("Clock").Value;
 * 
 *                     string raceName = raceSta.Element("EventRace").Element("Name").Value;
 *                     if (res != "")
 *                     {
 *                         if (!dict.ContainsKey(raceName))
 *                             dict[raceName] = new List<Tuple<string, string>> ();
 *                         dict[raceName].Add(new Tuple<string, string> (name, res));
 *                     }
 *                 }
 *             }
 * 
 *             foreach (string stage in dict.Keys)
 *             {
 *                 Class clas = new Class(elem.Element("EventClass").Element("Name").Value,
 *                                        Int32.Parse(elem.Attribute("numberOfEntries").Value),
 *                                        dict[stage]);
 *                 if (!dClass.ContainsKey(stage))
 *                     dClass[stage] = new List<Class>();
 *                 dClass[stage].Add(clas);
 *             }
 *         }
 * 
 *         foreach (string stage in dClass.Keys)
 *         {
 *             System.Console.WriteLine(stage);
 *             foreach (Class clas in dClass[stage])
 *             {
 *                 System.Console.WriteLine(clas.name + " " + clas.numOfPeople);
 *                 foreach (var x in clas.people)
 *                     System.Console.WriteLine(x.Item1 + " " + x.Item2);
 *                 System.Console.WriteLine();
 *             }
 *             System.Console.WriteLine();
 *             System.Console.WriteLine();
 *         }
 */
    }
}
