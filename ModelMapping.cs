using FluentNHibernate.Mapping;

namespace Eventor
{
    public class CarMap : ClassMap<Car>
    {
        public CarMap()
        {
            Id(x => x.Id);
            Map(x => x.Title);
            Map(x => x.Description);
            References(x => x.Make);
            References(x => x.Model);
        }
    }

    public class MakeMap : ClassMap<Make>
    {
        public MakeMap()
        {
            Id(x => x.Id);
            Map(x => x.Name);
            HasMany(x => x.Models);
        }
    }

   public class ModelMap : ClassMap<Model>
   {
       public ModelMap()
       {
           Id(x => x.Id);
           Map(x => x.Name);
           References(x => x.Make);
       }
   }

   public class ClubMap : ClassMap<Club>
   {
       public ClubMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID).Not.Nullable();
           Map(x => x.Name);
           HasMany(x => x.People);
       }
   }

   public class PersonMap : ClassMap<Person>
   {
       public PersonMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID);
           Map(x => x.Name);
           Map(x => x.Address);
           Map(x => x.Phone);
           Map(x => x.Email);
           References(x => x.Club);
       }
   }

   public class EventMap : ClassMap<Event>
   {
       public EventMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID).Not.Nullable();
           Map(x => x.Name);
           Map(x => x.Url);
           HasMany(x => x.Classes);
           HasMany(x => x.Races);
           HasMany(x => x.Documents);
       }
   }

   public class ClassMap : ClassMap<Class>
   {
       public ClassMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID).Not.Nullable();
           Map(x => x.Name);
           References(x => x.Event).Not.Nullable();
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

   public class RaceMap : ClassMap<Race>
   {
       public RaceMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID).Not.Nullable();
           Map(x => x.Date);
           Map(x => x.Name);
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
           References(x => x.Race).Not.Nullable();
           References(x => x.Class).Not.Nullable();
           HasMany(x => x.Runs);
       }
   }

   public class RunMap : ClassMap<Run>
   {
       public RunMap()
       {
           Id(x => x.Id);
           Map(x => x.EventorID).Not.Nullable();
           References(x => x.Person).Not.Nullable();
           References(x => x.RaceClass).Not.Nullable();
           Map(x => x.StartTime);
           Map(x => x.Time);
           Map(x => x.Position);
       }
   }
}
