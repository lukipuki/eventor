using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

public class Test
{
    public tatic bool Validator (object ender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }


    public static void Main()
    {
        // string ApiKey = ConfigurationManager.AppSettings["ApiKey"];
        // ServicePointManager.ServerCertificateValidationCallback = Validator;
        // string baseUrl = "https://eventor.orientering.se/api/";
        // var client = new WebClient();
        // client.Headers.Add("ApiKey", ApiKey);

        // try
        // {
        //     var bytes = client.DownloadData(baseUrl + "persons/organisations/636?includeContactDetails=true");
        //     string responseString = System.Text.Encoding.UTF8.GetString(bytes);
        //     System.Console.WriteLine(responseString);
        // }
        // catch (Exception e)
        // {
        //     System.Console.WriteLine(e);
        // }

        List<Person> peopleDatabase = new List<Person> ();
        HashSet<int> noUpdate = new HashSet<int> ();
        foreach (Person person in peopleDatabase.Where(p => !p.Update && p.EventorId != null))
            noUpdate.Add((int)person.EventorId);

        XDocument peopleXml = XDocument.Load("people.xml");
        List<Person> peopleEventor = new List<Person> ();
        foreach (var personElement in peopleXml.Element("PersonList").Elements("Person"))
        {
            XElement nameElement = personElement.Element("PersonName");
            string name = nameElement.Element("Given").Value + " " + nameElement.Element("Family").Value;
            int? eventorId = null;
            if (personElement.Element("PersonId") != null)
                eventorId = Int32.Parse(personElement.Element("PersonId").Value);

            Person person = new Person {
                    Name = name,
                    EventorId = eventorId};
            peopleEventor.Add(person);
        }

        var peopleToImport = peopleEventor.
            Where(p => p.EventorId != null && !noUpdate.Contains((int)p.EventorId));

        foreach (Person person in peopleToImport)
        {
            System.Console.WriteLine(person.Name + " " + person.EventorId);
        }
    }
}
