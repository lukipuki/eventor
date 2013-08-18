using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Linq;

namespace Eventor
{
    public class Club
    {
        public virtual int Id { get; protected set; }
        public virtual int EventorID { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<Person> People { get; protected set; }

        public virtual void AddPerson(Person person)
        {
            person.Club = this;
            People.Add(person);
        }
    }

    public class Person
    {
        public virtual int Id { get; protected set; }
        public virtual int? EventorID { get; set; }
        public virtual string Name { get; set; }
        public virtual Club Club { get; set; }
        public virtual string Address { get; set; }
        public virtual string Phone { get; set; }
        public virtual string Email { get; set; }
    }

    public class Event
    {
        public virtual int Id { get; protected set; }
        public virtual int EventorID { get; set; }
        public virtual string Name { get; set; }
        public virtual string Url { get; set; }
        public virtual DateTime? StartDate { get; set; }
        public virtual DateTime? FinishDate { get; set; }

        public virtual IList<Class> Classes { get; protected set; }
        public virtual IList<Race> Races { get; protected set; }
        public virtual IList<Document> Documents { get; protected set; }

        public Event()
        {
            Classes = new List<Class> ();
            Races = new List<Race> ();
            Documents = new List<Document> ();
        }

        public virtual void AddClass(Class clas)
        {
            clas.Event = this;
            Classes.Add(clas);
        }

        public virtual void AddRace(Race race)
        {
            race.Event = this;
            Races.Add(race);
        }

        public virtual void AddDocument(Document document)
        {
            document.Event = this;
            Documents.Add(document);
        }
    }

    public class Document
    {
        public virtual int Id { get; protected set; }
        public virtual int EventorID { get; set; }
        public virtual Event Event { get; set; }
        public virtual string Name { get; set; }
        public virtual string Url { get; set; }
    }

    public class Class
    {
        public virtual int Id { get; protected set; }
        public virtual int EventorID { get; set; }
        public virtual string Name { get; set; }
        public virtual Event Event { get; set; }
        public virtual IList<RaceClass> RaceClasses { get; protected set; }
    }

    public class Race
    {
        public virtual int Id { get; protected set; }
        public virtual int EventorID { get; set; }
        public virtual Event Event { get; set; }
        public virtual string Name { get; set; }
        public virtual string Distance { get; set; }
        public virtual bool Daylight { get; set; }
        public virtual DateTime Date { get; set; }
        public virtual decimal? X { get; set; }
        public virtual decimal? Y { get; set; }
        public virtual IList<RaceClass> RaceClasses { get; protected set; }
    }

    public class RaceClass
    {
        public virtual int Id { get; protected set; }
        public virtual int EventorID { get; set; }
        public virtual Race Race { get; set; }
        public virtual Class Class { get; set; }
        public virtual int? Length { get; set; }
        public virtual int? NoRunners { get; set; }
        public virtual IList<Run> Runs { get; protected set; }
    }

    public class Run
    {
        public virtual int Id { get; protected set; }
        public virtual Person Person { get; set; }
        public virtual RaceClass RaceClass { get; set; }
        public virtual DateTime? StartTime { get; set; }
        public virtual TimeSpan? Time { get; set; }
        public virtual TimeSpan? TimeDiff { get; set; }
        public virtual int? Position { get; set; }
        public virtual string Status { get; set; }
    }

    public class TotalResult
    {
        public virtual int Id { get; protected set; }
        public virtual Person Person { get; set; }
        public virtual Class Class { get; set; }
        public virtual TimeSpan? Time { get; set; }
        public virtual TimeSpan? TimeDiff { get; set; }
        public virtual int? Position { get; set; }
        public virtual string Status { get; set; }
    }
}
