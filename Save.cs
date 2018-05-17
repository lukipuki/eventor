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
    public class EventInformation
    {
        public int EventorID;
        public ulong? WordPressID;
        public EventInformation(int eventorID, ulong? wordPressID)
        {
            EventorID = eventorID;
            WordPressID = wordPressID;
        }
    }

    /**
     * Contains the SaveXXXX part of Synchronization
     */
    public static partial class Synchronization
    {
        /**
         * Save all the clubs
         */
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

        /**
         * Save one person
         */
        private static void SavePerson(XElement personElement, Person person,
                Club club, ISession session)
        {
            var nameTuple = Util.NameFrom(personElement.Element("PersonName"));
            person.GivenName = nameTuple.Item1;
            person.FamilyName = nameTuple.Item2;
            person.Club = club;
            if (personElement.Element("Address") != null)
                person.Address =
                    string.Join(", ", personElement.Element("Address").Attributes()
                            .Where(x => x.Value.Trim() != "")
                            .Select(x => x.Name == "careOf" ? "c/o " + x.Value : x.Value));

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

        /**
         * Save people from some club, together with all of their information
         */
        private static void SavePeople(Club club, ISession session, XDocument peopleXml)
        {
            Dictionary<int, Person> peopleById = session.Query<Person>()
                .Where(x => x.EventorID != null)
                .ToDictionary(x => (int)x.EventorID);
            Dictionary<string, Person> peopleByName = club.People.Where(x => x.EventorID == null)
                .ToDictionary(x => x.Name);

            foreach (XElement personElement in peopleXml.Element("PersonList").Elements("Person"))
            {
                var nameTuple = Util.NameFrom(personElement.Element("PersonName"));
                string name = nameTuple.Item1 + " " + nameTuple.Item2;
                int eventorID = Util.IntFromElement("PersonId", personElement);

                Person person;
                if (peopleById.ContainsKey(eventorID))
                    person = peopleById[eventorID];
                else if (peopleByName.ContainsKey(name))
                {
                    person = peopleByName[name];
                    person.EventorID = eventorID;
                }
                else
                    person = new Person { EventorID = eventorID };

                SavePerson(personElement, person, club, session);
            }
        }

        /**
         * Save event information
         */
        static void SaveEvents(ISession session, XDocument eventXml, Dictionary<int, ulong> WordPressIDs)
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
                if (WordPressIDs.ContainsKey(eventorID))
                    even.WordPressID = WordPressIDs[eventorID];
                even.Form = eventEl.Attribute("eventForm").Value;
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

        /**
         * Save documents for all given events
         */
        static void SaveDocuments(ISession session, XDocument xml, IEnumerable<int> eventIDs)
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
                {
                    document = documentsById[eventorID];
                    documentsById.Remove(eventorID);
                }
                else
                {
                    document = new Document { EventorID = eventorID };
                    eventsById[int.Parse(docEl.Attribute("referenceId").Value)].AddDocument(document);
                }
                document.Name = docEl.Attribute("name").Value;
                document.Url = docEl.Attribute("url").Value;
                session.SaveOrUpdate(document);
            }

            foreach (Event even in eventIDs
                     .Where(x => eventsById.ContainsKey(x)).Select(x => eventsById[x]))
            {
                List<Document> toDelete = new List<Document> ();
                foreach (Document document in even.Documents
                        .Where(x => documentsById.ContainsKey(x.EventorID)))
                    toDelete.Add(document);
                foreach (Document document in toDelete)
                    session.Delete(document);
            }
        }

        static void SaveClasses(ISession session, XDocument xml, int eventID)
        {
            Event even =
                session.Query<Event>().Single(x => x.EventorID == eventID);
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

                Dictionary<int, RaceClass> raceClassById =
                    clas.RaceClasses.ToDictionary(x => x.EventorID);

                foreach (XElement clasInfo in clasEl.Elements("ClassRaceInfo"))
                {
                    eventorID = Util.IntFromElement("ClassRaceInfoId", clasInfo);

                    RaceClass raceClass;
                    if (raceClassById.ContainsKey(eventorID))
                    {
                        raceClass = raceClassById[eventorID];
                        raceClassById.Remove(eventorID);
                    }
                    else
                    {
                        raceClass = new RaceClass
                        {
                            EventorID = eventorID,
                            Race = racesById[Util.IntFromElement("EventRaceId", clasInfo)],
                        };
                        clas.AddRaceClass(raceClass);
                    }
                    double? len = Util.DoubleFromElement("CourseLength", clasInfo);
                    if (len != null)
                        raceClass.Length = (int)Math.Round((double)len);
                    session.SaveOrUpdate(raceClass);
                }
            }
        }

        /**
         * Check which information is available for the event and take note.
         */
        private static void HasInformation(Event even, ISession session)
        {
            foreach (Race race in even.Races)
            {
                HasInformation(race, session);
                if (race.HasResults) even.HasResults = true;
                if (race.HasStartlist) even.HasStartlist = true;
            }
            session.Update(even);
        }

        /**
         * Check which information is available for the race and take note.
         */
        private static void HasInformation(Race race, ISession session)
        {
            race.HasResults = session.Query<Run>()
                .Any(x => x.Status != null && x.RaceClass.Race == race);
            race.HasStartlist = session.Query<Run>()
                .Any(x => x.StartTime != null && x.RaceClass.Race == race);
            session.Update(race);
        }

        static void SaveEntries(ISession session, XDocument xml, List<Event> events)
        {
            Dictionary<int, Person> peopleById = session.Query<Person>()
                .Where(x => x.EventorID != null).ToDictionary(x => (int)x.EventorID);

            List<RaceClass> raceClasses = new List<RaceClass> ();
            List<Run> runs = new List<Run> ();
            Dictionary<int, List<int>> raceIds = new Dictionary<int, List<int>> ();
            foreach (Event even in events.Where(x => x.Form.StartsWith("Ind")))
            {
                raceClasses.AddRange(session.Query<RaceClass>().Where(x => x.Race.Event == even));
                runs.AddRange(session.Query<Run>().Where(x => x.RaceClass.Race.Event == even));
                raceIds[even.EventorID] = even.Races.Select(x => x.EventorID).ToList();
            }
            Dictionary<System.Tuple<int, int>, RaceClass> raceClassRetr =
                raceClasses.ToDictionary(x => System.Tuple.Create(x.Race.EventorID, x.Class.EventorID));
            Dictionary<System.Tuple<int, int>, Run> runsRetr =
                runs.ToDictionary(x => System.Tuple.Create(x.Person.Id, x.RaceClass.Id));

            foreach (var entry in xml.Element("EntryList").Elements("Entry"))
            {
                int eventID = Util.IntFromElement("EventId", entry);
                int classID = Util.IntFromElement("EventClassId", entry.Element("EntryClass"));
                // TODO: what if no PersonId?
                int personID = Util.IntFromElement("PersonId", entry.Element("Competitor"));

                foreach (int raceID in raceIds[eventID])
                {
                    if (!peopleById.ContainsKey(personID)) continue;
                    Person person = peopleById[personID];
                    RaceClass raceClass = raceClassRetr[System.Tuple.Create(raceID, classID)];

                    Run run;
                    var runID = System.Tuple.Create(person.Id, raceClass.Id);
                    if (runsRetr.ContainsKey(runID))
                    {
                        run = runsRetr[runID];
                        runsRetr.Remove(runID);
                    }
                    else run = new Run
                    {
                        RaceClass = raceClass,
                        Person = person
                    };

                    run.SI = Util.IntFromElementNullable("CCardId", entry.Element("Competitor").Element("CCard"));
                    session.SaveOrUpdate(run);
                }
            }

            foreach (Run run in runsRetr.Values)
                session.Delete(run);

            foreach (Event even in events.Where(x => x.Form.StartsWith("Ind")))
                HasInformation(even, session);
        }

        /**
         * Finds a person based on the given person XML element.
         */
        static Person FindPerson(XElement personElement, Dictionary<int, Person> peopleById,
                Dictionary<string, Person> peopleByName)
        {
            int? personID = Util.IntFromElementNullable("PersonId", personElement);
            var nameTuple = Util.NameFrom(personElement.Element("PersonName"));
            string name = nameTuple.Item1 + " " + nameTuple.Item2;

            if (personID != null && peopleById.ContainsKey((int)personID))
                return peopleById[(int)personID];
            if (peopleByName.ContainsKey(name))
                return peopleByName[name];
            return null;
        }

        static void SaveStartlist(ISession session, XDocument xml)
        {
            if (xml.Element("StartList").Element("Event") == null) return;

            int eventID = Util.IntFromElement("EventId", xml.Element("StartList").Element("Event"));
            Event even = session.Query<Event>().Single(x => x.EventorID == eventID);
            if (!even.Form.StartsWith("Ind")) return; // TODO: support for other forms

            Dictionary<int, Person> peopleById = session.Query<Person>()
                .Where(x => x.EventorID != null).ToDictionary(x => (int)x.EventorID);
            Dictionary<string, Person> peopleByName = session.Query<Person>()
                .Where(x => x.EventorID == null).ToDictionary(x => x.Name);
            Dictionary<System.Tuple<int, int>, RaceClass> raceClassRetr =
                session.Query<RaceClass>().Where(x => x.Race.Event == even)
                .ToDictionary(x => System.Tuple.Create(x.Race.EventorID, x.Class.EventorID));
            Dictionary<System.Tuple<int, int>, Run> runsRetr =
                session.Query<Run>().Where(x => x.RaceClass.Race.Event == even)
                .ToDictionary(x => System.Tuple.Create(x.Person.Id, x.RaceClass.Id));

            bool singleDay = even.Form.EndsWith("SingleDay");
            int raceID = singleDay ? even.Races[0].EventorID : 0;
            foreach (var startlist in xml.Element("StartList").Elements("ClassStart"))
            {
                int classID = Util.IntFromElement("EventClassId", startlist);
                foreach (var personSta in startlist.Elements("PersonStart"))
                {
                    XElement personEl = personSta.Element("Person");
                    Person person = FindPerson(personEl, peopleById, peopleByName);

                    foreach (XElement raceSta in
                            singleDay ? new XElement[] {personSta} : personSta.Elements("RaceStart"))
                    {
                        if (!singleDay)
                            raceID = Util.IntFromElement("EventRaceId", raceSta.Element("EventRace"));
                        RaceClass raceClass =
                            raceClassRetr[System.Tuple.Create(raceID, classID)];

                        Run run;
                        var runID = System.Tuple.Create(person.Id, raceClass.Id);
                        if (runsRetr.ContainsKey(runID))
                        {
                            run = runsRetr[runID];
                            runsRetr.Remove(runID);
                        }
                        else
                        {
                            run = new Run { RaceClass = raceClass, Person = person };
                        }

                        XElement staEl = raceSta.Element("Start");
                        run.StartTime = Util.DateFromElementNullable(staEl.Element("StartTime"));
                        run.SI = Util.IntFromElementNullable("CCardId", staEl);
                        session.SaveOrUpdate(run);
                    }
                }
            }

            // Delete obsolete runs
            foreach (Run run in runsRetr.Values)
                session.Delete(run);
            HasInformation(even, session);
        }

        static void SaveResults(ISession session, XDocument xml)
        {
            int eventID = Util.IntFromElement("EventId", xml.Element("ResultList").Element("Event"));
            Event even = session.Query<Event>().Where(x => x.EventorID == eventID).First();
            if (!even.Form.StartsWith("Ind")) return; // TODO: support for other forms

            Dictionary<int, Club> clubsById =
                session.Query<Club>().ToDictionary(x => (int)x.EventorID);

            Dictionary<int, Person> peopleById = session.Query<Person>()
                .Where(x => x.EventorID != null).ToDictionary(x => (int)x.EventorID);

            Dictionary<string, Person> peopleByName = session.Query<Person>()
                .Where(x => x.EventorID == null).ToDictionary(x => x.Name);
            Club ourClub = session.Query<Club>().Single(x => x.EventorID == ourClubID);
            foreach (Person person in ourClub.People) peopleByName[person.Name] = person;

            Dictionary<System.Tuple<int, int>, RaceClass> raceClassRetr = session.Query<RaceClass>()
                .Where(x => x.Race.Event == even)
                .ToDictionary(x => System.Tuple.Create(x.Race.EventorID, x.Class.EventorID));
            Dictionary<System.Tuple<int, int>, Run> runsRetr = session.Query<Run>()
                .Where(x => x.RaceClass.Race.Event == even)
                .ToDictionary(x => System.Tuple.Create(x.Person.Id, x.RaceClass.Id));

            bool singleDay = even.Form.EndsWith("SingleDay");
            foreach (var result in xml.Element("ResultList").Elements("ClassResult"))
            {
                int classID = Util.IntFromElement("EventClassId", result);
                if (!singleDay)
                {
                    foreach (var classInfo in result.Element("EventClass").Elements("ClassRaceInfo"))
                    {
                        int raceID = Util.IntFromElement("EventRaceId", classInfo);
                        var raceClass = raceClassRetr[System.Tuple.Create(raceID, classID)];
                        if (classInfo.Attribute("noOfStarts") != null)
                        {
                            raceClass.NoRunners = int.Parse(classInfo.Attribute("noOfStarts").Value);
                            session.Update(raceClass);
                        }
                    }
                }

                // Sometimes classes with noone from our club show up, skip those
                bool ok = result.Elements("PersonResult").Any(
                    x => Util.IntFromElementNullable("OrganisationId", x.Element("Organisation"))
                        == ourClubID
                    );
                if (!ok) continue;

                foreach (var personRes in result.Elements("PersonResult"))
                {
                    XElement personElement = personRes.Element("Person");
                    Person person = FindPerson(personElement, peopleById, peopleByName);
                    if (person == null) person = new Person();

                    int? clubID = Util.IntFromElementNullable("OrganisationId",
                            personRes.Element("Organisation"));
                    Club club = null;
                    if (clubID != null && clubsById.ContainsKey((int)clubID))
                        club = clubsById[(int)clubID];
                    SavePerson(personElement, person, club, session);

                    foreach (XElement raceRes in
                            singleDay ? new XElement[] {personRes} : personRes.Elements("RaceResult"))
                    {
                        int raceID = singleDay ? even.Races[0].EventorID :
                            Util.IntFromElement("EventRaceId", raceRes.Element("EventRace"));
                        RaceClass raceClass =
                            raceClassRetr[System.Tuple.Create(raceID, classID)];

                        Run run;
                        var runID = System.Tuple.Create(person.Id, raceClass.Id);
                        if (runsRetr.ContainsKey(runID))
                        {
                            run = runsRetr[runID];
                            runsRetr.Remove(runID);
                        }
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
                                        run.Position = Util.IntFromElementNullable("ResultPosition", resEl);
                                        break;
                            case "Cancelled" : run.Status = "br&ouml;t"; break;
                            case "MisPunch" : run.Status = "felst."; break;
                            case "DidNotStart" : run.Status = "ej start"; break;
                            case "DidNotFinish" : run.Status = "&aring;terbud"; break;
                            case "Disqualified" : run.Status = "diskv."; break;
                        }
                        run.StartTime = Util.DateFromElementNullable(resEl.Element("StartTime"));
                        if (resEl.Element("CCardId") != null)
                            run.SI = Util.IntFromElement("CCardId", resEl);

                        session.SaveOrUpdate(run);
                    }
                }
            }

            foreach (Run run in runsRetr.Values)
                session.Delete(run);

            HasInformation(even, session);
        }
    }
}
