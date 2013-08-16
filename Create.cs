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

        static DateTime DateFromElement(XElement el)
        {
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
            // TODO: Overwrite
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
                DateTime startDate = DateFromElement(eventEl.Element("StartDate"));
                DateTime finishDate = DateFromElement(eventEl.Element("FinishDate"));

                Event even;
                if (eventsById.ContainsKey(eventorID))
                    even = eventsById[eventorID];
                else
                    even = new Event { EventorID = eventorID };
                even.Name = name;
                even.Url = url;
                even.StartDate = startDate;
                even.FinishDate = finishDate;

                foreach (XElement raceEl in eventEl.Elements("EventRace"))
                {
                    int raceID = IntFromElement("EventRaceId", raceEl);
                    name = raceEl.Element("Name").Value;
                    DateTime date = DateFromElement(raceEl.Element("RaceDate"));
                    bool daylight = raceEl.Attribute("raceLightCondition").Value == "Day";
                    string distance = raceEl.Attribute("raceDistance").Value;
                    XElement position = raceEl.Element("EventCenterPosition");
                    decimal? x, y;
                    if (position != null)
                    {
                        x = decimal.Parse(position.Attribute("x").Value);
                        y = decimal.Parse(position.Attribute("y").Value);
                    }

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
                    race.Daylight = daylight;
                    race.Distance = distance;
                    race.X = x;
                    race.Y = y;
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
                string shortName = clasEl.Element("ClassShortName").Value;
                int eventorID = IntFromElement("EventClassId", clasEl);

                Class clas;
                if (classesById.ContainsKey(eventorID))
                    clas = classesById[eventorID];
                else
                {
                    clas = new Class { EventorID = eventorID };
                    even.AddClass(clas);
                }
                clas.Name = shortName;
                Dictionary<int, RaceClass> raceClassById =
                    (from raceClass in session.Query<RaceClass>()
                     where raceClass.Class == clas
                     select raceClass)
                    .ToDictionary(x => x.EventorID);

                foreach (var clasInfo in clasEl.Elements("ClassRaceInfo"))
                {
                    int raceID = IntFromElement("EventRaceId", clasInfo);
                    eventorID = IntFromElement("ClassRaceInfoId", clasInfo);
                    double? len = DoubleFromElement("CourseLength", clasInfo);
                    int? length = null;
                    if (len != null)
                        length = (int)Math.Round((double)len);
                    Race race = racesById[raceID];

                    RaceClass raceClass;
                    if (raceClassById.ContainsKey(eventorID))
                        raceClass = raceClassById[eventorID];
                    else raceClass = new RaceClass {
                        EventorID = eventorID,
                        Race = race,
                        Class = clas
                    };
                    raceClass.Length = length;
                    raceClass.NoRunners = null;
                    session.SaveOrUpdate(raceClass);
                }

                session.SaveOrUpdate(clas);
            }
        }

        static void SaveResults(ISession session, XDocument xml)
        {
            int eventID = IntFromElement("EventId", xml.Element("ResultList").Element("Event"));
            Event even =
                (from eve in session.Query<Event>() where eve.EventorID == eventID select eve)
                .Single();
            Dictionary<int, Race> racesById =
                (from race in session.Query<Race>() where race.Event == even select race)
                .ToDictionary(x => x.EventorID);
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
                    int noRunners = int.Parse(classInfo.Attribute("noOfStarts").Value);
                    int raceID = IntFromElement("EventRaceId", classInfo);
                    var raceClass = raceClassRetr[System.Tuple.Create(raceID, classID)];
                    raceClass.NoRunners = noRunners;
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
                        string status = "";
                        int raceID = IntFromElement("EventRaceId", raceRes.Element("EventRace"));
                        Race race = racesById[raceID];
                        RaceClass raceClass =
                            raceClassRetr[System.Tuple.Create(race.EventorID, clas.EventorID)];

                        TimeSpan? time = null, timeDiff = null;
                        XElement resEl = raceRes.Element("Result");
                        int? position = null;
                        switch (resEl.Element("CompetitorStatus").Attribute("value").Value)
                        {
                            case "OK" : status = "OK";
                                        time = TimeFromElement("Time", resEl);
                                        timeDiff = TimeFromElement("TimeDiff", resEl);
                                        position = IntFromElement("ResultPosition", resEl);
                            break;
                            case "Cancelled" : status = "bröt"; break;
                            case "MisPunch" : status = "felst."; break;
                        }

                        Run run;
                        var runId = System.Tuple.Create(person.Id, raceClass.Id);
                        if (runsRetr.ContainsKey(runId))
                            run = runsRetr[runId];
                        else run = new Run
                        {
                            RaceClass = raceClass,
                            Person = person,
                        };

                        run.Time = time;
                        run.TimeDiff = timeDiff;
                        run.Position = position;
                        run.Status = status;
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
