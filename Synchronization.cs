using NHibernate;
using NHibernate.Linq;
using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Threading;

namespace Eventor
{
    public static class Synchronization
    {
        private static void SaveClubs(ISession session, XDocument clubsXml)
        {
            Dictionary<int, Club> clubsById =
                session.Query<Club>().ToDictionary(x => x.EventorID);
            foreach (XElement organ in clubsXml.Element("OrganisationList").Elements("Organisation")
                    .Where(x => x.Element("OrganisationTypeId").Value == "3"))
            {
                int eventorID = Util.IntFromElement("OrganisationId", organ);
                Club club;
                if (clubsById.ContainsKey(eventorID))
                    club = clubsById[eventorID];
                else
                    club = new Club { EventorID = eventorID };
                club.Name = organ.Element("ShortName").Value;
                session.SaveOrUpdate(club);
            }
        }

        private static void SavePerson(XElement personElement, Person person,
                Club club, ISession session)
        {
            person.GivenName = string.Join(" ",
                personElement.Element("PersonName").Elements("Given").Select(x => x.Value));
            person.FamilyName = Util.StringFrom(personElement.Element("PersonName").Element("Family"));
            person.Club = club;
            if (personElement.Element("Address") != null)
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

        private static void SavePeople(Club club, ISession session, XDocument peopleXml)
        {
            Dictionary<int, Person> peopleById = club.People.Where(x => x.EventorID != null)
                .ToDictionary(x => (int)x.EventorID);
            Dictionary<string, Person> peopleByName = club.People.Where(x => x.EventorID == null)
                .ToDictionary(x => x.Name);

            foreach (XElement personElement in peopleXml.Element("PersonList").Elements("Person"))
            {
                XElement nameElement = personElement.Element("PersonName");
                string name = nameElement.Element("Given").Value + " " + nameElement.Element("Family").Value;
                int eventorID = Util.IntFromElement("PersonId", personElement);

                Person person;
                if (peopleById.ContainsKey(eventorID))
                    person = peopleById[eventorID];
                else if (peopleByName.ContainsKey(name))
                    person = peopleByName[name];
                else
                    person = new Person { EventorID = eventorID };

                SavePerson(personElement, person, club, session);
            }
        }

        static void SaveEvents(ISession session, XDocument eventXml)
        {
            Dictionary<int, Event> eventsById =
                session.Query<Event>().ToDictionary(x => x.EventorID);
            Dictionary<int, Race> racesById =
                session.Query<Race>().ToDictionary(x => x.EventorID);

            foreach (XElement eventEl in eventXml.Element("EventList").Elements("Event"))
            {
                int eventorID = Util.IntFromElement("EventId", eventEl);
                Event even;
                if (eventsById.ContainsKey(eventorID))
                    even = eventsById[eventorID];
                else
                    even = new Event { EventorID = eventorID };

                even.Name = eventEl.Element("Name").Value;
                even.Url = Util.StringFrom(eventEl.Element("WebURL"));
                even.StartDate = Util.DateFromElement(eventEl.Element("StartDate"));
                even.FinishDate = Util.DateFromElement(eventEl.Element("FinishDate"));
                even.EntryBreak = eventEl.Elements("EntryBreak")
                    .Where(x => x.Element("ValidFromDate") != null)
                    .Select(x => Util.DateFromElement(x.Element("ValidFromDate")))
                    .DefaultIfEmpty(even.StartDate.AddDays(-7)).Max();
                session.SaveOrUpdate(even);

                foreach (XElement raceEl in eventEl.Elements("EventRace"))
                {
                    int raceID = Util.IntFromElement("EventRaceId", raceEl);

                    Race race;
                    if (racesById.ContainsKey(raceID))
                        race = racesById[raceID];
                    else
                    {
                        race = new Race { EventorID = raceID };
                        even.AddRace(race);
                    }
                    race.Name = raceEl.Element("Name").Value;
                    race.Date = Util.DateFromElement(raceEl.Element("RaceDate"));
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
            }
        }

        static void SaveDocuments(ISession session, XDocument xml)
        {
            Dictionary<int, Event> eventsById =
                session.Query<Event>().ToDictionary(x => x.EventorID);
            Dictionary<int, Document> documentsById =
                session.Query<Document>().ToDictionary(x => x.EventorID);

            foreach (XElement docEl in xml.Element("DocumentList").Elements("Document"))
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
                session.Query<Event>().Where(x => x.EventorID == eventID).Single();
            Dictionary<int, Race> racesById = even.Races.ToDictionary(x => x.EventorID);
            Dictionary<int, Class> classesById = even.Classes.ToDictionary(x => x.EventorID);

            foreach (XElement clasEl in xml.Element("EventClassList").Elements("EventClass"))
            {
                int eventorID = Util.IntFromElement("EventClassId", clasEl);
                Class clas;
                if (classesById.ContainsKey(eventorID))
                {
                    clas = classesById[eventorID];
                    classesById.Remove(eventorID);
                }
                else
                {
                    clas = new Class { EventorID = eventorID };
                    even.AddClass(clas);
                }
                clas.Name = clasEl.Element("ClassShortName").Value;
                session.SaveOrUpdate(clas);

                Dictionary<int, RaceClass> raceClassById = session.Query<RaceClass>()
                    .Where(x => x.Class == clas).ToDictionary(x => x.EventorID);

                foreach (XElement clasInfo in clasEl.Elements("ClassRaceInfo"))
                {
                    eventorID = Util.IntFromElement("ClassRaceInfoId", clasInfo);

                    RaceClass raceClass;
                    if (raceClassById.ContainsKey(eventorID))
                    {
                        raceClass = raceClassById[eventorID];
                        raceClassById.Remove(eventorID);
                    }
                    else raceClass = new RaceClass
                    {
                        EventorID = eventorID,
                        Race = racesById[Util.IntFromElement("EventRaceId", clasInfo)],
                        Class = clas
                    };
                    double? len = Util.DoubleFromElement("CourseLength", clasInfo);
                    if (len != null)
                        raceClass.Length = (int)Math.Round((double)len);
                    session.SaveOrUpdate(raceClass);
                }

                // TODO: Delete, check cascading
                // foreach (var raceClass in raceClassById)
                //     session.Delete(raceClass);

            }

            // foreach (Class clas in classesById.Values)
            //     session.Delete(clas);
        }

        static void SaveStartlist(ISession session, XDocument xml)
        {
            if (xml.Element("StartList").Element("Event") == null) return;

            int eventID = Util.IntFromElement("EventId", xml.Element("StartList").Element("Event"));
            Event even = session.Query<Event>().Where(x => x.EventorID == eventID).Single();

            Dictionary<int, Person> peopleById = session.Query<Person>()
                .Where(x => x.EventorID != null).ToDictionary(x => (int)x.EventorID);
            Dictionary<System.Tuple<int, int>, RaceClass> raceClassRetr =
                session.Query<RaceClass>().Where(x => x.Race.Event == even)
                .ToDictionary(x => System.Tuple.Create(x.Race.EventorID, x.Class.EventorID));
            Dictionary<System.Tuple<int, int>, Run> runsRetr =
                session.Query<Run>().Where(x => x.RaceClass.Race.Event == even)
                .ToDictionary(x => System.Tuple.Create(x.Person.Id, x.RaceClass.Id));
            bool singleDay =
                xml.Element("StartList").Element("Event").Attribute("eventForm").Value == "IndSingleDay";

            int raceID = 0;
            if (singleDay)
                raceID = even.Races[0].EventorID;

            foreach (var startlist in xml.Element("StartList").Elements("ClassStart"))
            {
                int classID = Util.IntFromElement("EventClassId", startlist.Element("EventClass"));
                foreach (var personSta in startlist.Elements("PersonStart"))
                {
                    // TODO: What if the person doesn't have an ID?
                    if (personSta.Element("Person").Element("PersonId") == null)
                        continue;
                    int personID = Util.IntFromElement("PersonId", personSta.Element("Person"));
                    Person person = peopleById[personID];

                    foreach (XElement raceSta in
                            singleDay ? new XElement[] {personSta} : personSta.Elements("RaceStart"))
                    {
                        if (!singleDay)
                            raceID = Util.IntFromElement("EventRaceId", raceSta.Element("EventRace"));
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

                        XElement staEl = raceSta.Element("Start");
                        run.StartTime = Util.DateFromElementNullable(staEl.Element("StartTime"));
                        if (staEl.Element("CCardId") != null)
                            run.SI = Util.IntFromElement("CCardId", staEl);
                        session.SaveOrUpdate(run);
                    }
                }
            }
        }

        static void SaveResults(ISession session, XDocument xml)
        {
            //TODO support relays
            if (xml.Element("ResultList").Element("Event").Attribute("eventForm").Value ==
                "RelaySingleDay")
                return;

            int eventID = Util.IntFromElement("EventId", xml.Element("ResultList").Element("Event"));
            Event even = session.Query<Event>().Where(x => x.EventorID == eventID).First();
            Dictionary<int, Club> clubsById =
                session.Query<Club>().ToDictionary(x => (int)x.EventorID);
            Dictionary<int, Person> peopleById = session.Query<Person>()
                .Where(x => x.EventorID != null).ToDictionary(x => (int)x.EventorID);
            Dictionary<string, Person> peopleByName = session.Query<Person>()
                .Where(x => x.EventorID == null).ToDictionary(x => x.Name);
            Dictionary<System.Tuple<int, int>, RaceClass> raceClassRetr = session.Query<RaceClass>()
                .Where(x => x.Race.Event == even)
                .ToDictionary(x => System.Tuple.Create(x.Race.EventorID, x.Class.EventorID));
            Dictionary<System.Tuple<int, int>, Run> runsRetr = session.Query<Run>()
                .Where(x => x.RaceClass.Race.Event == even)
                .ToDictionary(x => System.Tuple.Create(x.Person.Id, x.RaceClass.Id));
            bool singleDay =
                xml.Element("ResultList").Element("Event").Attribute("eventForm").Value
                == "IndSingleDay";

            foreach (var result in xml.Element("ResultList").Elements("ClassResult"))
            {
                int classID = Util.IntFromElement("EventClassId", result.Element("EventClass"));
                int raceID = 0;
                foreach (var classInfo in result.Element("EventClass").Elements("ClassRaceInfo"))
                {
                    raceID = Util.IntFromElement("EventRaceId", classInfo);
                    var raceClass = raceClassRetr[System.Tuple.Create(raceID, classID)];
                    if (classInfo.Attribute("noOfStarts") != null)
                    {
                        raceClass.NoRunners = int.Parse(classInfo.Attribute("noOfStarts").Value);
                        session.Update(raceClass);
                    }
                }

                bool ok = result.Elements("PersonResult").Any(
                    x => Util.IntFromElementNullable("OrganisationId", x.Element("Organisation"))
                         == ourClubID
                    );

                if (ok) foreach (var personRes in result.Elements("PersonResult"))
                {
                    int? personID = Util.IntFromElementNullable("PersonId", personRes.Element("Person"));
                    XElement nameElement = personRes.Element("Person").Element("PersonName");
                    string name = nameElement.Element("Given").Value + " " + nameElement.Element("Family").Value;

                    int? clubID = Util.IntFromElementNullable("OrganisationId", personRes.Element("Organisation"));
                    Club club = null;
                    if (clubID != null && clubsById.ContainsKey((int)clubID))
                        club = clubsById[(int)clubID];
                    Person person;
                    if (personID != null)
                    {
                        if (!peopleById.ContainsKey((int)personID))
                        {
                            peopleById[(int)personID] = new Person { EventorID = personID };
                            SavePerson(personRes.Element("Person"), peopleById[(int)personID],
                                    club, session);
                        }
                        person = peopleById[(int)personID];
                    }
                    else
                    {
                        if (!peopleByName.ContainsKey(name))
                            peopleByName[name] = new Person();
                        person = peopleByName[name];
                        SavePerson(personRes.Element("Person"), person, club, session);
                    }

                    foreach (XElement raceRes in
                            singleDay ? new XElement[] {personRes} : personRes.Elements("RaceResult"))
                    {
                        if (!singleDay)
                            raceID = Util.IntFromElement("EventRaceId", raceRes.Element("EventRace"));
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

                        XElement resEl = raceRes.Element("Result");
                        switch (resEl.Element("CompetitorStatus").Attribute("value").Value)
                        {
                            case "OK" : run.Status = "OK";
                                        run.Time = Util.TimeFromElement("Time", resEl);
                                        run.TimeDiff = Util.TimeFromElement("TimeDiff", resEl);
                                        run.Position = Util.IntFromElement("ResultPosition", resEl);
                            break;
                            case "Cancelled" : run.Status = "bröt"; break;
                            case "MisPunch" : run.Status = "felst."; break;
                            case "DidNotStart" : run.Status = "ej start"; break;
                        }
                        run.StartTime = Util.DateFromElementNullable(resEl.Element("StartTime"));
                        if (resEl.Element("CCardId") != null)
                            run.SI = Util.IntFromElement("CCardId", resEl);

                        session.SaveOrUpdate(run);
                    }
                }
            }
        }

        private static int ourClubID;
        public static void SynchronizeEvents(IEnumerable<int> eventIDs, bool minimal = false,
                bool offline = false, bool save = false)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            string events = string.Join(",", eventIDs);
            ourClubID = int.Parse(ConfigurationManager.AppSettings["ClubId"]);

            using (var session = NHibernateHelper.OpenSession())
            {
                if (!minimal)
                {
                    XDocument clubsXml = offline ? XDocument.Load("XML/clubs.xml") :
                        Util.DownloadXml("organisations");
                    if (!offline && save) clubsXml.Save("XML/clubs.xml");
                    using (var transaction = session.BeginTransaction())
                    {
                        SaveClubs(session, clubsXml);
                        transaction.Commit();
                    }

                    Club club = session.Query<Club>().Where(x => x.EventorID == ourClubID).Single();

                    XDocument peopleXml = offline ? XDocument.Load("XML/people.xml") :
                        Util.DownloadXml(
                            string.Format("persons/organisations/{0}?includeContactDetails=true", ourClubID));
                    if (!offline && save) peopleXml.Save("XML/people.xml");
                    using (var transaction = session.BeginTransaction())
                    {
                        SavePeople(club, session, peopleXml);
                        transaction.Commit();
                    }
                }

                XDocument eventXml = offline ? XDocument.Load("XML/events.xml") :
                    Util.DownloadXml("events?eventIds=" + events + "&includeEntryBreaks=true");
                if (!offline && save) eventXml.Save("XML/events.xml");

                using (var transaction = session.BeginTransaction())
                {
                    SaveEvents(session, eventXml);
                    transaction.Commit();
                }

                XDocument documentXml = offline ? XDocument.Load("XML/documents.xml") :
                    Util.DownloadXml("events/documents?eventIds=" + events);
                if (!offline && save) documentXml.Save("XML/documents.xml");

                using (var transaction = session.BeginTransaction())
                {
                    SaveDocuments(session, documentXml);
                    transaction.Commit();
                }

                foreach (int eventID in eventIDs)
                {
                    // Event doesn't exist
                    if (!session.Query<Event>().Any(x => x.EventorID == eventID)) continue;

                    Event even = session.Query<Event> ().Where(x => x.EventorID == eventID).Single();

                    if (minimal &&
                       (DateTime.Now < even.StartDate.AddDays(-7) ||
                        DateTime.Now > even.FinishDate.AddDays(4)))
                        continue;

                    XDocument classesXml = offline ? XDocument.Load("XML/classes-" + eventID + ".xml")
                        : Util.DownloadXml("eventclasses?eventId=" + eventID);
                    if (!offline && save)
                        classesXml.Save("XML/classes-" + eventID + ".xml");

                    using (var transaction = session.BeginTransaction())
                    {
                        SaveClasses(session, classesXml, eventID);
                        transaction.Commit();
                    }

                    if (DateTime.Now <= even.FinishDate)
                    {
                        string startlistUrl =
                            String.Format("starts/organisation?eventId={0}&organisationIds={1}",
                                          eventID, ourClubID);
                        XDocument startlistXml = offline ? XDocument.Load("XML/startlists-" + eventID + ".xml")
                            : Util.DownloadXml(startlistUrl);
                        if (!offline && save)
                            startlistXml.Save("XML/startlists-" + eventID + ".xml");

                        using (var transaction = session.BeginTransaction())
                        {
                            SaveStartlist(session, startlistXml);
                            transaction.Commit();
                        }
                    }

                    if (DateTime.Now >= even.StartDate)
                    {
                        string resultUrl =
                            String.Format("results/organisation?eventId={0}&organisationIds={1}&top=1",
                                          eventID, ourClubID);
                        XDocument resultsXml = offline ? XDocument.Load("XML/results-" + eventID + ".xml")
                            : Util.DownloadXml(resultUrl);
                        if (!offline && save)
                            resultsXml.Save("XML/results-" + eventID + ".xml");

                        using (var transaction = session.BeginTransaction())
                        {
                            SaveResults(session, resultsXml);
                            transaction.Commit();
                        }
                    }
                }
            }
        }

        public static void Main()
        {
            // SynchronizeEvents(new int[] {5113, 7344, 4511, 4512, 4515, 6545, 7524, 7525, 4517, 4518, 3932}, true);
            SynchronizeEvents(new int[] {4507, 3826, 4168}, offline : true, save : true);
        }
    }
}
