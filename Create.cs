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

        static int IntFromElement(string name, XElement el)
        {
            return int.Parse(el.Element(name).Value);
        }

        static DateTime DateFromElement(XElement el)
        {
            string dat = el.Element("Date").Value + " " + el.Element("Clock").Value;
            return DateTime.Parse(dat);
        }

        private static void SavePeople(Club club, ISession session, XDocument peopleXml)
        {
            Dictionary<int?, Person> peopleById =
                (from person in session.Query<Person>()
                 where person.Club == club && person.EventorID != null
                 select person).ToDictionary(x => x.EventorID);
            Dictionary<string, Person> peopleByName =
                (from person in session.Query<Person>()
                 where person.Club == club && person.EventorID == null
                 select person).ToDictionary(x => x.Name);

            foreach (var personElement in peopleXml.Element("PersonList").Elements("Person"))
            {
                XElement nameElement = personElement.Element("PersonName");
                string name = nameElement.Element("Given").Value + " " + nameElement.Element("Family").Value;
                int eventorID = Int32.Parse(personElement.Element("PersonId").Value);
                string address =
                    string.Join(", ", personElement.Element("Address").Attributes().Select(x => x.Value));
                string phone = null, email = null;

                XElement telEl = personElement.Element("Tele");
                if (telEl != null)
                {
                    if (telEl.Attribute("mobilePhoneNumber") != null)
                        phone = telEl.Attribute("mobilePhoneNumber").Value;
                    if (telEl.Attribute("mailAddress") != null)
                        email = telEl.Attribute("mailAddress").Value;
                }

                Person person;
                if (peopleById.ContainsKey(eventorID))
                    person = peopleById[eventorID];
                else if (peopleByName.ContainsKey(name))
                    person = peopleByName[name];
                else
                    person = new Person { EventorID = eventorID };

                person.Name = name;
                person.Club = club;
                person.Address = address;
                person.Phone = phone;
                person.Email = email;
                session.SaveOrUpdate(person);
            }
        }

        static void SaveEvents(ISession session, XDocument eventXml)
        {
            Dictionary<int, Event> eventsById =
                (from even in session.Query<Event>() select even).ToDictionary(x => x.EventorID);
            Dictionary<int, Race> racesById =
                (from race in session.Query<Race>() select race).ToDictionary(x => x.EventorID);

            foreach (XElement eventEl in eventXml.Element("EventList").Elements("Event"))
            {
                int eventorID = IntFromElement("EventId", eventEl);
                string name = eventEl.Element("Name").Value;
                string url = eventEl.Element("WebURL").Value;

                Event even;
                if (eventsById.ContainsKey(eventorID))
                    even = eventsById[eventorID];
                else
                    even = new Event { EventorID = eventorID };
                even.Name = name;
                even.Url = url;

                foreach (XElement raceEl in eventEl.Elements("EventRace"))
                {
                    int raceID = IntFromElement("EventRaceId", raceEl);
                    name = raceEl.Element("Name").Value;
                    DateTime date = DateFromElement(raceEl.Element("RaceDate"));

                    Race race;
                    if (racesById.ContainsKey(raceID))
                        race = racesById[raceID];
                    else
                    {
                        race = new Race { EventorID = raceID };
                        even.AddRace(race);
                    }
                    race.Name = name;
                    race.Date = date;
                }
                session.Save(even);
            }
        }

        static void SaveDocuments(ISession session, XDocument xml)
        {
            Dictionary<int, Event> eventsById =
                (from even in session.Query<Event>() select even).ToDictionary(x => x.EventorID);
            Dictionary<int, Document> documentsById =
                (from document in session.Query<Document>() select document)
                .ToDictionary(x => x.EventorID);

            foreach (var docEl in xml.Element("DocumentList").Elements("Document"))
            {
                string name = docEl.Attribute("name").Value;
                string url = docEl.Attribute("url").Value;
                int eventID = int.Parse(docEl.Attribute("referenceId").Value);
                int eventorID = int.Parse(docEl.Attribute("id").Value);

                Document document;
                if (documentsById.ContainsKey(eventorID))
                    document = documentsById[eventorID];
                else
                {
                    document = new Document { EventorID = eventorID };
                    eventsById[eventID].AddDocument(document);
                }

                document.Name = name;
                document.Url = url;
                session.Save(document);
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

                // var bytes = client.DownloadData(baseUrl + "events?eventIds=5113");
                // responseString = System.Text.Encoding.UTF8.GetString(bytes);
                // System.Console.WriteLine(responseString);
                // XDocument eventXml = XDocument.Load(new MemoryStream(UTF8Encoding.Default.GetBytes(responseString)));
                XDocument eventXml = XDocument.Load("events.xml");

                // var bytes = client.DownloadData(baseUrl + "events/documents?eventIds=5113");
                // responseString = System.Text.Encoding.UTF8.GetString(bytes);
                // System.Console.WriteLine(responseString);
                // XDocument documentXml = XDocument.Load(new MemoryStream(UTF8Encoding.Default.GetBytes(responseString)));
                XDocument documentXml = XDocument.Load("documents.xml");

                using (var session = NHibernateHelper.OpenSession())
                {
                    // using (var transaction = session.BeginTransaction())
                    // {
                    //     foreach (var organ in clubsXml.Element("OrganisationList").Elements("Organisation")
                    //             .Where(x => x.Element("OrganisationTypeId").Value == "3"))
                    //     {
                    //         Club club = new Club {
                    //             Name = organ.Element("Name").Value,
                    //             EventorID = int.Parse(organ.Element("OrganisationId").Value)
                    //         };
                    //         session.Save(club);
                    //     }
                    // }

                    // var centrum = (from club in session.Query<Club>()
                    //                where club.EventorID == 636
                    //                select club).Single();

                    // using (var transaction = session.BeginTransaction())
                    // {
                    //     SavePeople(centrum, session, peopleXml);
                    //     transaction.Commit();
                    // }

                    // using (var transaction = session.BeginTransaction())
                    // {
                    //     SaveEvents(session, eventXml);
                    //     transaction.Commit();
                    // }

                    using (var transaction = session.BeginTransaction())
                    {
                        SaveDocuments(session, documentXml);
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
