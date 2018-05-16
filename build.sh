export TERM=xterm
mcs -r:NHibernate.dll,Iesi.Collections.dll,FluentNHibernate.dll,MySql.Data.dll,System.Configuration.dll,System.Xml.Linq.dll Synchronization.cs Config.cs Model.cs ModelMapping.cs Save.cs Util.cs
mcs -t:library -r:NHibernate.dll,Iesi.Collections.dll,FluentNHibernate.dll,MySql.Data.dll,System.Configuration.dll,System.Xml.Linq.dll Synchronization.cs Config.cs Model.cs ModelMapping.cs Save.cs Util.cs
