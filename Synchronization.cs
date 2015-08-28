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
    public static partial class Synchronization
    {
        private static int ourClubID;
        public static void SynchronizeEvents(IEnumerable<EventInformation> eventInfos,
                bool full = false, bool offline = false, bool save = false)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            string eventstring = string.Join(",", eventInfos.Select(x => x.EventorID));
            ourClubID = int.Parse(ConfigurationManager.AppSettings["ClubId"]);

            using (var session = NHibernateHelper.OpenSession())
            {
                // Only download clubs and people if full is true, since they don't change often
                if (full)
                {
                    try {
                        XDocument clubsXml = offline ? XDocument.Load("XML/clubs.xml") :
                            Util.DownloadXml("organisations");
                        if (!offline && save) clubsXml.Save("XML/clubs.xml");
                        using (var transaction = session.BeginTransaction())
                        {
                            SaveClubs(session, clubsXml);
                            transaction.Commit();
                        }

                        Club club = session.Query<Club>().Single(x => x.EventorID == ourClubID);

                        XDocument peopleXml = offline ? XDocument.Load("XML/people.xml") :
                            Util.DownloadXml(
                                string.Format("persons/organisations/{0}?includeContactDetails=true", ourClubID));
                        if (!offline && save) peopleXml.Save("XML/people.xml");
                        using (var transaction = session.BeginTransaction())
                        {
                            SavePeople(club, session, peopleXml);
                            transaction.Commit();
                        }
                        // Successfully saved clubs and people
                    }
                    catch (Exception e)
                    {
                        // Failed to save clubs and people
                    }
                }

                // No events to download, time to go home
                if (eventInfos.Count() == 0) return;

                try {
                    XDocument eventXml = offline ? XDocument.Load("XML/events.xml") :
                        Util.DownloadXml("events?eventIds=" + eventstring + "&includeEntryBreaks=true");
                    if (!offline && save) eventXml.Save("XML/events.xml");

                    using (var transaction = session.BeginTransaction())
                    {
                        Dictionary<int, ulong> WordPressIDs = new Dictionary<int, ulong> ();
                        foreach (var x in eventInfos.Where(y => y.WordPressID != null))
                            WordPressIDs[x.EventorID] = (ulong)x.WordPressID;
                        SaveEvents(session, eventXml, WordPressIDs);
                        transaction.Commit();
                    }
                    // Successfully saved events
                }
                catch (Exception e)
                {
                    // Failed to save events
                }

                try {
                    XDocument documentXml = offline ? XDocument.Load("XML/documents.xml") :
                        Util.DownloadXml("events/documents?eventIds=" + eventstring);
                    if (!offline && save) documentXml.Save("XML/documents.xml");

                    using (var transaction = session.BeginTransaction())
                    {
                        SaveDocuments(session, documentXml, eventInfos.Select(x => x.EventorID));
                        transaction.Commit();
                    }
                }
                catch (Exception e)
                {
                    // Failed to load documents
                }

                List<Event> events = new List<Event> ();
                foreach (var eventInfo in eventInfos)
                {
                    int eventID = eventInfo.EventorID;
                    // Event doesn't exist
                    if (!session.Query<Event>().Any(x => x.EventorID == eventID))
                    {
                        // Event number eventID doesn't exist
                        continue;
                    }

                    Event even = session.Query<Event> ().Single(x => x.EventorID == eventID);
                    // TODO: Use even.EntryBreak instead
                    if (DateTime.Now <= even.FinishDate)
                        events.Add(even);

                    try {
                        XDocument classesXml = offline ? XDocument.Load("XML/classes-" + eventID + ".xml")
                            : Util.DownloadXml("eventclasses?eventId=" + eventID);
                        if (!offline && save)
                            classesXml.Save("XML/classes-" + eventID + ".xml");

                        using (var transaction = session.BeginTransaction())
                        {
                            SaveClasses(session, classesXml, eventID);
                            transaction.Commit();
                        }
                    }
                    catch (Exception e)
                    {
                        // Failed to load classes for event.EventName, not trying to load startlists
                        // or results
                        continue;
                    }

                    if (DateTime.Now <= even.FinishDate)
                    {
                        try {
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
                            // Successfully loaded startlist for event event.Name
                        }
                        catch (Exception e)
                        {
                            // Failed to load startlist for event event.Name
                        }
                    }

                    if (DateTime.Now >= even.StartDate)
                    {
                        try {
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
                            // Successfully loaded results for event event.Name
                        }
                        catch (Exception e)
                        {
                            // Failed to load results for event even.Name
                        }
                    }
                }

                try {
                    string entriesUrl =
                        String.Format("entries?eventIds={0}&organisationIds={1}",
                                      string.Join(",", events.Select(x => x.EventorID)), ourClubID);
                    XDocument entriesXml = offline ? XDocument.Load("XML/entries.xml")
                        : Util.DownloadXml(entriesUrl);
                    if (!offline && save)
                        entriesXml.Save("XML/entries.xml");

                    using (var transaction = session.BeginTransaction())
                    {
                        SaveEntries(session, entriesXml, events);
                        transaction.Commit();
                    }
                }
                catch (Exception e)
                {
                    // Failed to load entries
                }
            }
        }

        public static void Main()
        {
            // Testing the code
            SynchronizeEvents(
                new EventInformation[] {
                    // new EventInformation(5113, 19421),
                    // new EventInformation(3303, null),
                    // new EventInformation(7496, null),
                    // new EventInformation(7497, null),
                    new EventInformation(10711, null)
                    },
                    // offline : false, full : true, save : true);
                    offline : true, full : true, save : false);
        }
    }
}
