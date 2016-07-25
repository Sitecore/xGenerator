using System;

namespace AnalyticsUpdater.Installer
{
  using System.Collections.Specialized;
  using System.Configuration;
  using System.Data.SqlClient;
  using System.IO;
  using System.Web.Hosting;
  using Sitecore.Install.Framework;

  public class PostStep : IPostStep
  {
    const string RemoveProcedureQuery = @"IF OBJECT_ID('sp_sc_Refresh_Analytics', 'P') is not null
BEGIN
  DROP PROCEDURE[dbo].[sp_sc_Refresh_Analytics]
END";

    public void Run(ITaskOutput output, NameValueCollection metaData)
    {
      this.ApplyScript();
    }

    public void ApplyScript()
    {
      var connectionstring = ConfigurationManager.ConnectionStrings["reporting"];
      var refreshAnalytics = this.MapPath("~/RefreshAnalytics.sql");
      var reader = new StreamReader(new FileInfo(refreshAnalytics).OpenRead());
      var query = reader.ReadToEnd();

      var connection = new SqlConnection(connectionstring.ConnectionString);

      try
      {
        connection.Open();

        var clearCommand = new SqlCommand(RemoveProcedureQuery,connection);
        clearCommand.ExecuteNonQuery();

        var command = new SqlCommand(query, connection);
        command.ExecuteNonQuery();
      }
      finally
      {
        connection.Close();
      }
    }

    protected string MapPath(string relativePath)
    {
      return HostingEnvironment.MapPath(relativePath);
    }
  }
}