using System;
using System.Collections.Generic;
using System.Security.Policy;

namespace Colossus
{
    public class Visit : SimulationObject
    {
        public Visitor Visitor { get; set; }


        public List<Request> Requests { get; set; }

        public Visit()
        {
            Requests = new List<Request>();
        }

        public Request AddRequest(string url, TimeSpan? duration = null, TimeSpan? pause = null,
            string durationVariable = "Duration", string pauseVariable = "Pause")
        {
            var request = new Request { Visit = this, Url = url };
            if (Visitor.Segment != null)
            {
                foreach (var v in Visitor.Segment.RequestVariables)
                {
                    v.SetValues(request);
                }
            }

            var lastRequest = Requests.Count > 0 ? Requests[Requests.Count - 1].End : Start;
            lastRequest += pause ?? request.GetVariable(pauseVariable, request.GetVariable("Pause", TimeSpan.Zero));

            request.Start = lastRequest;
            request.End = lastRequest + (duration ?? request.GetVariable(durationVariable, request.GetVariable("Duration", TimeSpan.Zero)));


            End = request.End;
            Visitor.End = End;

            if (Requests.Count == 0 && !request.Variables.ContainsKey("Referrer"))
            {
                var referrer = this.GetVariable<string>("Referrer");
                if (!string.IsNullOrEmpty(referrer))
                {
                    request.Variables.Add("Referrer", referrer);
                }
            }

            Requests.Add(request);

            return request;
        }
    }
}
