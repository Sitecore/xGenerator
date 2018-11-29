namespace Colossus
{
    public class Request : SimulationObject
    {
        public Visit Visit { get; set; }

        public string Url { get; set; }

        public string Method { get; set; }

        public string PostData { get; set; }
        
        public bool EndVisit { get; set; }
    }
}
