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
using System.Globalization;
using System.Threading;

namespace Eventor
{
    static class Synchronize
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

        static double? DoubleFromElement(string name, XElement el)
        {
            if (el.Element(name) == null)
                return null;
            return double.Parse(el.Element(name).Value);
        }

        static DateTime? DateFromElement(XElement el)
        {
            if (el == null)
                return null;
            string dat = el.Element("Date").Value + " " + el.Element("Clock").Value;
            return DateTime.Parse(dat);
        }

        static TimeSpan TimeFromElement(string name, XElement el)
        {
            string[] val = el.Element(name).Value.Split(':');
            return TimeSpan.FromSeconds(60 * int.Parse(val[0]) + int.Parse(val[1]));
        }

        private static void SaveClubs(ISession session, XDocument clubsXml)
        {
            // TODO: Overwrite old
            foreach (var organ in clubsXml.Element("OrganisationList").Elements("Organisation")
                    .Where(x => x.Element("OrganisationTypeId").Value == "3"))
            {
                Club club = new Club {
                    EventorID = int.Parse(organ.Element("OrganisationId").Value)
                };
                club.Name = organ.Element("Name").Value;
                session.Save(club);
            }
        }

        private static void SavePeople(Club club, ISession session, XDocument peopleXml)
        {
            Dictionary<int, Person> peopleById =
                (from person in session.Query<Person>()
                 where person.Club == club && person.EventorID != null
                 select person).ToDictionary(x => (int)x.EventorID);
            Dictionary<string, Person> peopleByName =
                (from person in session.Query<Person>()
                 where person.Club == club && person.EventorID == null
                 select person).ToDictionary(x => x.Name);

            foreach (var personElement in peopleXml.Element("PersonList").Elements("Person"))
            {
                XElement nameElement = personElement.Element("PersonName");
                string name = nameElement.Element("Given").Value + " " + nameElement.Element("Family").Value;
                int eventorID = Int32.Parse(personElement.Element("PersonId").Value);

                Person person;
                if (peopleById.ContainsKey(eventorID))
                    person = peopleById[eventorID];
                else if (peopleByName.ContainsKey(name))
                    person = peopleByName[name];
                else
                    person = new Person { EventorID = eventorID };

                person.Name = name;
                person.Club = club;
                person.Address =
                    string.Join(", ", personElement.Element("Address").Attributes().Select(x => x.Value));

                XElement telEl = personElement.Element("Tele");
                if (telEl != null)
                {
                    if (telEl.Attribute("mobilePhoneNumber") != null)
                        person.Phone = telEl.Attribute("mobilePhoneNumber").Value;
                    if (telEl.Attribute("mailAddress") != null)
                        person.Email = telEl.Attribute("mailAddress").Value;
                }

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
                Event even;
                if (eventsById.ContainsKey(eventorID))
                    even = eventsById[eventorID];
                else
                    even = new Event { EventorID = IntFromElement("EventId", eventEl) };
                even.Name = eventEl.Element("Name").Value;
                even.Url = eventEl.Element("WebURL").Value;
                even.StartDate = DateFromElement(eventEl.Element("StartDate"));
                even.FinishDate = DateFromElement(eventEl.Element("FinishDate"));

                foreach (XElement raceEl in eventEl.Elements("EventRace"))
                {
                    int raceID = IntFromElement("EventRaceId", raceEl);

                    Race race;
                    if (racesById.ContainsKey(raceID))
                        race = racesById[raceID];
                    else
                    {
                        race = new Race { EventorID = raceID };
                        even.AddRace(race);
                    }
                    race.Name = raceEl.Element("Name").Value;
                    race.Date = (DateTime)DateFromElement(raceEl.Element("RaceDate"));
                    race.Daylight = raceEl.Attribute("raceLightCondition").Value == "Day";
                    race.Distance = raceEl.Attribute("raceDistance").Value;

                    XElement position = raceEl.Element("EventCenterPosition");
                    if (position != null)
                    {
                        race.X = decimal.Parse(position.Attribute("x").Value);
                        race.Y = decimal.Parse(position.Attribute("y").Value);
                    }
                    session.SaveOrUpdate(race);
                }
                session.SaveOrUpdate(even);
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
                int eventorID = int.Parse(docEl.Attribute("id").Value);

                Document document;
                if (documentsById.ContainsKey(eventorID))
                    document = documentsById[eventorID];
                else
                {
                    document = new Document { EventorID = eventorID };
                    eventsById[int.Parse(docEl.Attribute("referenceId").Value)].AddDocument(document);
                }

                document.Name = docEl.Attribute("name").Value;
                document.Url = docEl.Attribute("url").Value;
                session.SaveOrUpdate(document);
            }
        }

        static void SaveClasses(ISession session, XDocument xml, int eventID)
        {
            Event even =
                (from eve in session.Query<Event>() where eve.EventorID == eventID select eve)
                .Single();
            Dictionary<int, Race> racesById =
                (from race in session.Query<Race>() where race.Event == even select race)
                .ToDictionary(x => x.EventorID);
            Dictionary<int, Class> classesById =
                (from clas in session.Query<Class>() where clas.Event == even select clas)
                .ToDictionary(x => x.EventorID);

            foreach (var clasEl in xml.Element("EventClassList").Elements("EventClass"))
            {
                int eventorID = IntFromElement("EventClassId", clasEl);
                Class clas;
                if (classesById.ContainsKey(eventorID))
                    clas = classesById[eventorID];
                else
                {
                    clas = new Class { EventorID = eventorID };
                    even.AddClass(clas);
                }
                clas.Name = clasEl.Element("ClassShortName").Value;
                Dictionary<int, RaceClass> raceClassById =
                    (from raceClass in session.Query<RaceClass>()
                     where raceClass.Class == clas
                     select raceClass)
                    .ToDictionary(x => x.EventorID);

                foreach (var clasInfo in clasEl.Elements("ClassRaceInfo"))
                {
                    eventorID = IntFromElement("ClassRaceInfoId", clasInfo);

                    RaceClass raceClass;
                    if (raceClassById.ContainsKey(eventorID))
                        raceClass = raceClassById[eventorID];
                    else raceClass = new RaceClass {
                        EventorID = eventorID,
                        Race = racesById[IntFromElement("EventRaceId", clasInfo)],
                        Class = clas
                    };
                    double? len = DoubleFromElement("CourseLength", clasInfo);
                    if (len != null)
                        raceClass.Length = (int)Math.Round((double)len);
                    session.SaveOrUpdate(raceClass);
                }

                session.SaveOrUpdate(clas);
            }
        }

        static void SaveStartlist(ISession session, XDocument xml)
        {
            int eventID = IntFromElement("EventId", xml.Element("StartList").Element("Event"));
            Event even =
                (from eve in session.Query<Event>() where eve.EventorID == eventID select eve)
                .Single();
            Dictionary<int, Person> peopleById =
                (from person in session.Query<Person>()
                 where person.EventorID != null select person).ToDictionary(x => (int)x.EventorID);
            Dictionary<System.Tuple<int, int>, RaceClass> raceClassRetr =
                (from raceClass in session.Query<RaceClass>()
                 where raceClass.Race.Event == even
                 select raceClass)
                .ToDictionary(x => System.Tuple.Create(x.Race.EventorID, x.Class.EventorID));
            Dictionary<System.Tuple<int, int>, Run> runsRetr =
                (from run in session.Query<Run>()
                 where run.RaceClass.Race.Event == even
                 select run)
                .ToDictionary(x => System.Tuple.Create(x.Person.Id, x.RaceClass.Id));

            foreach (var startlist in xml.Element("StartList").Elements("ClassStart"))
            {
                int classID = IntFromElement("EventClassId", startlist.Element("EventClass"));
                foreach (var personSta in startlist.Elements("PersonStart"))
                {
                    // TODO: What if the person doesn't have an ID?
                    int personID = IntFromElement("PersonId", personSta.Element("Person"));
                    Person person = peopleById[personID];
                    foreach (XElement raceSta in personSta.Elements("RaceStart"))
                    {
                        int raceID = IntFromElement("EventRaceId", raceSta.Element("EventRace"));
                        RaceClass raceClass =
                            raceClassRetr[System.Tuple.Create(raceID, classID)];

                        Run run;
                        var runId = System.Tuple.Create(person.Id, raceClass.Id);
                        if (runsRetr.ContainsKey(runId))
                            run = runsRetr[runId];
                        else run = new Run
                        {
                            RaceClass = raceClass,
                            Person = person,
                        };

                        run.StartTime = DateFromElement(raceSta.Element("Start").Element("StartTime"));
                        session.SaveOrUpdate(run);
                    }
                }
            }
        }

        static void SaveResults(ISession session, XDocument xml)
        {
            int eventID = IntFromElement("EventId", xml.Element("ResultList").Element("Event"));
            Event even =
                (from eve in session.Query<Event>() where eve.EventorID == eventID select eve)
                .Single();
            Dictionary<int, Class> classesById =
                (from clas in session.Query<Class>() where clas.Event == even select clas)
                .ToDictionary(x => x.EventorID);
            Dictionary<int, Person> peopleById =
                (from person in session.Query<Person>()
                 where person.EventorID != null select person).ToDictionary(x => (int)x.EventorID);
            Dictionary<System.Tuple<int, int>, RaceClass> raceClassRetr =
                (from raceClass in session.Query<RaceClass>()
                 where raceClass.Race.Event == even
                 select raceClass)
                .ToDictionary(x => System.Tuple.Create(x.Race.EventorID, x.Class.EventorID));
            Dictionary<System.Tuple<int, int>, Run> runsRetr =
                (from run in session.Query<Run>()
                 where run.RaceClass.Race.Event == even
                 select run)
                .ToDictionary(x => System.Tuple.Create(x.Person.Id, x.RaceClass.Id));

            foreach (var result in xml.Element("ResultList").Elements("ClassResult"))
            {
                int classID = IntFromElement("EventClassId", result.Element("EventClass"));
                foreach (var classInfo in result.Element("EventClass").Elements("ClassRaceInfo"))
                {
                    int raceID = IntFromElement("EventRaceId", classInfo);
                    var raceClass = raceClassRetr[System.Tuple.Create(raceID, classID)];
                    raceClass.NoRunners = int.Parse(classInfo.Attribute("noOfStarts").Value);
                    session.Update(raceClass);
                }

                Class clas = classesById[classID];
                foreach (var personRes in result.Elements("PersonResult"))
                {
                    // TODO: What if the person doesn't have an ID?
                    int personID = IntFromElement("PersonId", personRes.Element("Person"));
                    Person person = peopleById[personID];
                    foreach (XElement raceRes in personRes.Elements("RaceResult"))
                    {
                        int raceID = IntFromElement("EventRaceId", raceRes.Element("EventRace"));
                        RaceClass raceClass =
                            raceClassRetr[System.Tuple.Create(raceID, clas.EventorID)];

                        Run run;
                        var runId = System.Tuple.Create(person.Id, raceClass.Id);
                        if (runsRetr.ContainsKey(runId))
                            run = runsRetr[runId];
                        else run = new Run
                        {
                            RaceClass = raceClass,
                            Person = person,
                        };

                        XElement resEl = raceRes.Element("Result");
                        switch (resEl.Element("CompetitorStatus").Attribute("value").Value)
                        {
                            case "OK" : run.Status = "OK";
                                        run.Time = TimeFromElement("Time", resEl);
                                        run.TimeDiff = TimeFromElement("TimeDiff", resEl);
                                        run.Position = IntFromElement("ResultPosition", resEl);
                            break;
                            case "Cancelled" : run.Status = "bröt"; break;
                            case "MisPunch" : run.Status = "felst."; break;
                        }

                        session.SaveOrUpdate(run);
                    }
                }
            }
        }

        public static void Main()
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            // ServicePointManager.ServerCertificateValidationCallback = Validator;
            // string ApiKey = ConfigurationManager.AppSettings["ApiKey"];
            // string baseUrl = "https://eventor.orientering.se/api/";
            // var client = new WebClient();
            // client.Headers.Add("ApiKey", ApiKey);
            // string responseString;

            try
            {
                // var bytes = client.DownloadData(baseUrl + "organisations");
                // responseString = System.Text.Encoding.UTF8.GetString(bytes);
                // System.Console.WriteLine(responseString);
                XDocument clubsXml = XDocument.Load("XML/clubs.xml");

                // var bytes = client.DownloadData(baseUrl + "persons/organisations/636?includeContactDetails=true");
                // responseString = System.Text.Encoding.UTF8.GetString(bytes);
                // System.Console.WriteLine(responseString);
                // XDocument peopleXml = XDocument.Load(new MemoryStream(UTF8Encoding.Default.GetBytes(responseString)));
                XDocument peopleXml = XDocument.Load("XML/people.xml");

                // var bytes = client.DownloadData(baseUrl + "events?eventIds=5113");
                // responseString = System.Text.Encoding.UTF8.GetString(bytes);
                // System.Console.WriteLine(responseString);
                // XDocument eventXml = XDocument.Load(new MemoryStream(UTF8Encoding.Default.GetBytes(responseString)));
                XDocument eventXml = XDocument.Load("XML/events.xml");

                // var bytes = client.DownloadData(baseUrl + "events/documents?eventIds=5113");
                // responseString = System.Text.Encoding.UTF8.GetString(bytes);
                // System.Console.WriteLine(responseString);
                // XDocument documentXml = XDocument.Load(new MemoryStream(UTF8Encoding.Default.GetBytes(responseString)));
                XDocument documentXml = XDocument.Load("XML/documents.xml");

                // var bytes = client.DownloadData(baseUrl + "eventclasses?eventId=5113");
                // responseString = System.Text.Encoding.UTF8.GetString(bytes);
                // System.Console.WriteLine(responseString);
                // XDocument classesXml = XDocument.Load(new MemoryStream(UTF8Encoding.Default.GetBytes(responseString)));
                XDocument classesXml = XDocument.Load("XML/classes.xml");

                // var bytes = client.DownloadData(baseUrl + "results/organisation?eventId=5113&organisationIds=636&top=1");
                // var bytes = client.DownloadData(baseUrl + "results/organisation?eventId=5113&organisationIds=636");
                // responseString = System.Text.Encoding.UTF8.GetString(bytes);
                // System.Console.WriteLine(responseString);
                // XDocument resultsXml = XDocument.Load(new MemoryStream(UTF8Encoding.Default.GetBytes(responseString)));
                XDocument resultsXml = XDocument.Load("XML/results.xml");

                // var bytes = client.DownloadData(baseUrl + "starts/organisation?eventId=5113&organisationIds=636");
                // responseString = System.Text.Encoding.UTF8.GetString(bytes);
                // System.Console.WriteLine(responseString);
                // XDocument startlistXml = XDocument.Load(new MemoryStream(UTF8Encoding.Default.GetBytes(responseString)));
                XDocument startlistXml = XDocument.Load("XML/startlist.xml");

                using (var session = NHibernateHelper.OpenSession())
                {
                    // using (var transaction = session.BeginTransaction())
                    // {
                    //     SaveClubs(session, clubsXml);
                    //     transaction.Commit();
                    // }

                    var centrum = (from club in session.Query<Club>()
                                   where club.EventorID == 636
                                   select club).Single();

                    using (var transaction = session.BeginTransaction())
                    {
                        SavePeople(centrum, session, peopleXml);
                        transaction.Commit();
                    }

                    using (var transaction = session.BeginTransaction())
                    {
                        SaveEvents(session, eventXml);
                        transaction.Commit();
                    }

                    using (var transaction = session.BeginTransaction())
                    {
                        SaveDocuments(session, documentXml);
                        transaction.Commit();
                    }

                    using (var transaction = session.BeginTransaction())
                    {
                        SaveClasses(session, classesXml, 5113);
                        transaction.Commit();
                    }

                    using (var transaction = session.BeginTransaction())
                    {
                        SaveStartlist(session, startlistXml);
                        transaction.Commit();
                    }

                    using (var transaction = session.BeginTransaction())
                    {
                        SaveResults(session, resultsXml);
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
