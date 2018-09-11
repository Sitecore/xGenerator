namespace Colossus.Web
{
    public class VisitInfo : SimulationObjectInfo
    {
        public static VisitInfo FromVisit(Visit visit)
        {
            var info = new VisitInfo();
            info.SetValuesFromObject(visit);
            return info;
        }
    }
}
