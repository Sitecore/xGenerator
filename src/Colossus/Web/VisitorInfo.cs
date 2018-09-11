namespace Colossus.Web
{
    public class VisitorInfo : SimulationObjectInfo
    {
        public string SegmentName { get; set; }

    
        public static VisitorInfo FromVisitor(Visitor v)
        {
            var info = new VisitorInfo {SegmentName = v.Segment != null ? v.Segment.Name : null};
            info.SetValuesFromObject(v);
            return info;
        }            
    }
}
