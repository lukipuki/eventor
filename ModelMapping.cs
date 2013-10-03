using FluentNHibernate.Mapping;

namespace Eventor
{
   public class ClubMap : ClassMap<Club>
   {
       public ClubMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID).Not.Nullable();
           Map(x => x.Name);
           HasMany(x => x.People).Inverse();
       }
   }

   public class PersonMap : ClassMap<Person>
   {
       public PersonMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID);
           Map(x => x.GivenName);
           Map(x => x.FamilyName);
           Map(x => x.Address);
           Map(x => x.Phone);
           Map(x => x.Email);
           References(x => x.Club);
           HasMany(x => x.Runs).Inverse();
       }
   }

   public class EventMap : ClassMap<Event>
   {
       public EventMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID).Not.Nullable();
           Map(x => x.WordPressID).Not.Nullable();
           Map(x => x.Name);
           Map(x => x.Url);
           Map(x => x.StartDate).Not.Nullable();
           Map(x => x.FinishDate).Not.Nullable();
           Map(x => x.EntryBreak);
           Map(x => x.HasResults);
           Map(x => x.HasStartlist);
           Map(x => x.Form);
           HasMany(x => x.Classes);
           HasMany(x => x.Races);
           HasMany(x => x.Documents);
       }
   }

   public class DocumentMap : ClassMap<Document>
   {
       public DocumentMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID).Not.Nullable();
           Map(x => x.Name);
           Map(x => x.Url);
           References(x => x.Event).Not.Nullable();
       }
   }

   public class ClassMap : ClassMap<Class>
   {
       public ClassMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID).Not.Nullable();
           Map(x => x.Name);
           HasMany(x => x.RaceClasses).Inverse();
           References(x => x.Event).Not.Nullable();
       }
   }

   public class RaceMap : ClassMap<Race>
   {
       public RaceMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID).Not.Nullable();
           Map(x => x.Date);
           Map(x => x.Name);
           Map(x => x.Distance);
           Map(x => x.Daylight);
           Map(x => x.X);
           Map(x => x.Y);
           Map(x => x.HasResults);
           Map(x => x.HasStartlist);
           HasMany(x => x.RaceClasses).Inverse();
           References(x => x.Event).Not.Nullable();
       }
   }

   public class RaceClassMap : ClassMap<RaceClass>
   {
       public RaceClassMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID).Not.Nullable();
           Map(x => x.Length);
           Map(x => x.NoRunners);
           HasMany(x => x.Runs).Inverse();
           References(x => x.Race).Not.Nullable();
           References(x => x.Class).Not.Nullable();
       }
   }

   public class RunMap : ClassMap<Run>
   {
       public RunMap()
       {
           Id(x => x.Id);
           References(x => x.Person).Not.Nullable();
           References(x => x.RaceClass).Not.Nullable();
           Map(x => x.StartTime);
           Map(x => x.Time);
           Map(x => x.TimeDiff);
           Map(x => x.Position);
           Map(x => x.SI);
           Map(x => x.Status);
       }
   }

   public class TotalResultMap : ClassMap<TotalResult>
   {
       public TotalResultMap()
       {
           Id(x => x.Id);
           References(x => x.Person).Not.Nullable();
           References(x => x.Class).Not.Nullable();
           Map(x => x.Time);
           Map(x => x.TimeDiff);
           Map(x => x.Position);
           Map(x => x.Status);
       }
   }
}
