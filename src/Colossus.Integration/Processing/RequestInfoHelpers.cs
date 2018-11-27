using System;
using Colossus.Web;

namespace Colossus.Integration.Processing
{
    public static class RequestInfoHelpers
    {
        public static bool SetIfVariablePresent(this RequestInfo requestInfo, string name,
            Action<string> setAction)
        {
            return requestInfo.SetIfVariablePresent<string>(name, setAction);
        }

        public static bool SetIfVariablePresent<TValue>(this RequestInfo requestInfo, string name, Action<TValue> setAction)
        {
            return requestInfo.Variables.SetIfPresent(name, setAction)
                   || (requestInfo.Visit != null && requestInfo.Visit.Variables.SetIfPresent(name, setAction))
                   || (requestInfo.Visitor != null && requestInfo.Visitor.Variables.SetIfPresent(name, setAction));

        }
    }
}
