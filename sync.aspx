<%@ Page language="C#" MasterPageFile="~/Site.master" %>
<%@ Mastertype VirtualPath="~/Site.master" %>
<%@ Import Namespace = "Eventor" %>
<%@ Import Namespace = "MySql.Data.MySqlClient" %>
<%@ Import Namespace = "System" %>
<%@ Import Namespace = "System.Data" %>
<%@ Import Namespace = "System.Collections.Generic" %>
<script RunAt="server">
void Page_Load(object sender, EventArgs args)
{
    int begOffset = -7, endOffset = 20;
    int.TryParse(Request.QueryString["beginning"], out begOffset);
    int.TryParse(Request.QueryString["end"], out endOffset);

    var events = new List<Eventor.EventInformation> ();
    using (MySqlConnection dbcon =
        new MySqlConnection(ConfigurationManager.AppSettings["ConnectionStringWP"]))
    {
        dbcon.Open();
        DateTime beginning = DateTime.Today.AddDays(begOffset), end = DateTime.Today.AddDays(endOffset);
        using (IDbCommand dbcmd = dbcon.CreateCommand())
        {
            dbcmd.CommandText = string.Format("CALL events_between('{0}', '{1}');",
                beginning.ToString("yyyyMMdd"), end.ToString("yyyyMMdd"));
            IDataReader reader = dbcmd.ExecuteReader();

            while (reader.Read())
            {
                int eventID;
                if (int.TryParse((string)reader["EventID"], out eventID))
                    events.Add(new Eventor.EventInformation(eventID, (ulong)reader["PostID"]));
            }
            reader.Close();
        }
    }

    Eventor.Synchronization.SynchronizeEvents(events, Request.QueryString["minimal"] == "true");
}
</script>
