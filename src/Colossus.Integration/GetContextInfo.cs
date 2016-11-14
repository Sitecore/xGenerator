namespace Colossus.Integration
{
  using System.Collections.Generic;
  using System.Web;
  using Colossus.Integration.Models;
  using Colossus.Integration.Processing;
  using Colossus.Web;
  using Sitecore.Analytics;
  using Sitecore.Mvc.Pipelines.Request.RequestEnd;
  using Sitecore.Pipelines.RenderLayout;

  internal class ActionExecutor:IActionExecutor
  {
    public List<IRequestAction> RequestActions { get; set; }

    public ActionExecutor()
    {
      this.RequestActions = new List<IRequestAction>();


      this.RequestActions.Add(new TriggerEventsAction());
      this.RequestActions.Add(new TriggerOutcomesAction());
    }
    public void ExecuteActions()
    {
      var ctx = HttpContext.Current;
      if (ctx != null)
      {
        var requestInfo = HttpContext.Current.ColossusInfo();
        if (Tracker.Current != null && requestInfo != null)
        {
          foreach (var action in this.RequestActions)
          {
            action.Execute(Tracker.Current, requestInfo);
          }
        }

        //var info = SitecoreResponseInfo.FromContext();
        //if (info != null)
        //{
        //  ctx.Response.Headers.AddChunked(DataEncoding.ResponseDataKey, DataEncoding.EncodeHeaderValue(info));
        //}
      }
    }
  }

  public interface IActionExecutor
  {
    void ExecuteActions();
  }

  public class MvcExecuteActions: RequestEndProcessor 
  {
    private readonly IActionExecutor executor;

    public MvcExecuteActions():this(new ActionExecutor())
    {
      
    }
    public MvcExecuteActions(IActionExecutor executor)
    {
      this.executor = executor;
    }

    public override void Process(RequestEndArgs args)
    {
      this.executor.ExecuteActions();
    }
  }

  public class GetContextInfo : RenderLayoutProcessor
  {
    private readonly IActionExecutor executor;

    public GetContextInfo() : this(new ActionExecutor())
    {

    }

    public GetContextInfo(IActionExecutor executor)
    {
      this.executor = executor;
    }

    public override void Process(RenderLayoutArgs args)
    {
      this.executor.ExecuteActions();
    }
  }
}


