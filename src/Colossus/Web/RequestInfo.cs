namespace Colossus.Web
{
    public class RequestInfo : SimulationObjectInfo
    {
        public VisitorInfo Visitor { get; set; }

        public VisitInfo Visit { get; set; }

        public bool EndVisit { get; set; }

        public static RequestInfo FromVisit(Request request)
        {
            var info = new RequestInfo();
            info.SetValuesFromObject(request);
            info.EndVisit = request.EndVisit;
            info.Visit = VisitInfo.FromVisit(request.Visit);
            info.Visitor = VisitorInfo.FromVisitor(request.Visit.Visitor);

            return info;
        }       
    }
}
