using System;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;

namespace Eventor
{
    class Synchronize
    {
        public static bool Validator (object sender, X509Certificate certificate, X509Chain chain,
                SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static void SavePeople(Club club, ISession session, XDocument peopleXml)
        {
            Dictionary<int?, Person> peopleEvId =
                (from person in session.Query<Person>()
                 where person.Club == club
                 select person).ToDictionary(x => x.EventorID);
            foreach (var personElement in peopleXml.Element("PersonList").Elements("Person"))
            {
                XElement nameElement = personElement.Element("PersonName");
                string name = nameElement.Element("Given").Value + " " + nameElement.Element("Family").Value;
                int? eventorID = null;
                if (personElement.Element("PersonId") != null)
                    eventorID = Int32.Parse(personElement.Element("PersonId").Value);
                string address =
                    string.Join(", ", personElement.Element("Address").Attributes().Select(x => x.Value));

                Person person;
                if (peopleEvId.ContainsKey(eventorID))
                    person = peopleEvId[eventorID];
                else
                    person = new Person();
                person.Name = name;
                person.EventorID = eventorID;
                person.Club = club;
                person.Address = address;
                session.SaveOrUpdate(person);
            }
        }

        public static void Main()
        {
            // ServicePointManager.ServerCertificateValidationCallback = Validator;
            // string ApiKey = ConfigurationManager.AppSettings["ApiKey"];
            // string baseUrl = "https://eventor.orientering.se/api/";
            // var client = new WebClient();
            // client.Headers.Add("ApiKey", ApiKey);

            string responseString;
            try
            {
                // var bytes = client.DownloadData(baseUrl + "organisations");
                // responseString = System.Text.Encoding.UTF8.GetString(bytes);
                // System.Console.WriteLine(responseString);
                XDocument clubsXml = XDocument.Load("clubs.xml");

                // var bytes = client.DownloadData(baseUrl + "persons/organisations/636?includeContactDetails=true");
                // responseString = System.Text.Encoding.UTF8.GetString(bytes);
                // System.Console.WriteLine(responseString);
                // XDocument peopleXml = XDocument.Load(new MemoryStream(UTF8Encoding.Default.GetBytes(responseString)));
                XDocument peopleXml = XDocument.Load("people.xml");

                using (var session = NHibernateHelper.OpenSession())
                {
                    using (var transaction = session.BeginTransaction())
                    {
                        foreach (var organ in clubsXml.Element("OrganisationList").Elements("Organisation")
                                .Where(x => x.Element("OrganisationTypeId").Value == "3"))
                        {
                            Club club = new Club {
                                Name = organ.Element("Name").Value,
                                EventorID = int.Parse(organ.Element("OrganisationId").Value)
                            };
                            session.Save(club);
                        }
                    }

                    var centrum = (from club in session.Query<Club>()
                                   where club.EventorID == 636
                                   select club).Single();

                    using (var transaction = session.BeginTransaction())
                    {
                        SavePeople(centrum, session, peopleXml);
                        transaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
    }
}
