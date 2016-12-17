using System;
using System.Collections.Generic;

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

        public Request AddRequest(string url, TimeSpan? duration = null, TimeSpan? pause = null)
        {
            var request = new Request
                          {
                              Visit = this,
                              Url = url
                          };
            if (Visitor.Segment != null)
            {
                foreach (var v in Visitor.Segment.RequestVariables)
                {
                    v.SetValues(request);
                }
            }

            var lastRequest = Requests.Count > 0 ? Requests[Requests.Count - 1].End : Start;
            lastRequest += request.GetVariable(VariableKey.Pause, request.GetVariable(VariableKey.Pause, TimeSpan.Zero));

            request.Start = lastRequest;
            request.End = lastRequest + (request.GetVariable(VariableKey.Duration, request.GetVariable(VariableKey.Duration, TimeSpan.Zero)));

            End = request.End;
            Visitor.End = End;

            if (Requests.Count == 0 && !request.Variables.ContainsKey(VariableKey.Referrer))
            {
                var referrer = this.GetVariable<string>(VariableKey.Referrer);
                if (!string.IsNullOrEmpty(referrer))
                {
                    request.Variables.Add(VariableKey.Referrer, referrer);
                }
            }

            Requests.Add(request);

            return request;
        }
    }
}
