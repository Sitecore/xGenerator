// -----------------------------------------------------------------------
// <copyright file="WizardStatusForm.cs" company="">
// Wizard form with updatable status
// </copyright>
// -----------------------------------------------------------------------

namespace AnalyticsUpdater.sitecore.shell.Applications.Dialogs
{
  using System;
  using System.Globalization;
  using Sitecore;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Jobs;
  using Sitecore.Web.UI.HtmlControls;
  using Sitecore.Web.UI.Pages;
  using Sitecore.Web.UI.Sheer;

  /// <summary>
  /// Wizard form with updatable status.
  /// </summary>
  public class WizardStatusForm : WizardForm
  {
    protected Memo ResultText;
    protected Literal Status;
    protected DatePicker BaseDate;

    protected string JobHandle
    {
      get
      {
        return StringUtil.GetString(this.ServerProperties["JobHandle"]);
      }
      set
      {
        Assert.ArgumentNotNullOrEmpty(value, "value");
        this.ServerProperties["JobHandle"] = value;
      }
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      if (!Context.ClientPage.IsEvent && BaseDate != null)
      {
        BaseDate.Value = DateUtil.ToIsoDate(DateTime.Now);
      }
    }

    protected override void ActivePageChanged(string page, string oldPage)
    {
      base.ActivePageChanged(page, oldPage);
      if (page == "Processing")
      {
        base.NextButton.Disabled = true;
        base.BackButton.Disabled = true;
        base.CancelButton.Disabled = true;
        SheerResponse.Timer("StartJob", 10);
      }
      if (page == "Settings")
      {
        base.NextButton.Header = "Run";
      }
      else
      {
        base.NextButton.Header = "Next >";
      }
    }

    protected void CheckStatus()
    {
      var job = JobManager.GetJob(Handle.Parse(this.JobHandle));
      if (job == null)
      {
        throw new Exception("Job interrupted");
      }
      var status = job.Status;

      if (status.Failed)
      {
        this.Active = "Retry";
        this.NextButton.Disabled = true;
        this.BackButton.Disabled = false;
        this.CancelButton.Disabled = false;
      }
      else
      {
        string str;
        switch (status.State)
        {
          case JobState.Running:
            str = string.Format("Processed {0}", status.Processed);
            break;
          case JobState.Initializing:
            str = Translate.Text("Initializing.");
            break;
          default:
            str = Translate.Text("Queued.");
            break;
        }
        if (status.State == JobState.Finished)
        {
          this.Status.Text = Translate.Text("Items processed: {0}.", new object[] { status.Processed.ToString(CultureInfo.InvariantCulture) });
          this.Active = "LastPage";
          this.BackButton.Disabled = true;
          var str2 = StringUtil.StringCollectionToString(status.Messages, "\n");
          if (!string.IsNullOrEmpty(str2))
          {
            this.ResultText.Value = str2;
          }
        }
        else
        {
          SheerResponse.SetInnerHtml("PublishingTarget", str);
          SheerResponse.Timer("CheckStatus", 500);
        }
      }
    }
  }
}
