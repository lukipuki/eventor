using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Conventions.Helpers;
using NHibernate;
using NHibernate.Tool.hbm2ddl;

namespace Eventor
{
    public class NHibernateHelper
    {
        /**
         * Configuration of NHibernate
         */

        private static ISessionFactory _sessionFactory;

        private static ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                    InitializeSessionFactory();
                return _sessionFactory;
            }
        }

        private static void InitializeSessionFactory()
        {
            _sessionFactory = Fluently.Configure()
                .Database(MySQLConfiguration.Standard
                          .ConnectionString(c => c.FromAppSetting("ConnectionString"))
                         )
                .Mappings(m =>
                          m.FluentMappings.AddFromAssemblyOf<Club>()
                          .Conventions
                          .Add(ForeignKey.EndsWith("Id"), Table.Is(x => x.EntityType.Name))
                        // DefaultCascade.DeleteOrphan())
                         )
                .ExposeConfiguration(cfg => new SchemaExport(cfg).Create(false, false))
                .BuildSessionFactory();
        }

        // TODO: Create tables

        public static ISession OpenSession()
        {
            return SessionFactory.OpenSession();
        }
    }
}
