using System;
using Colossus.Web;
using Sitecore.Diagnostics;

namespace Colossus.Integration.Processing
{
    public static class RequestInfoHelpers
    {
        public static bool SetIfVariablePresent(this RequestInfo requestInfo, VariableKey name, Action<string> setAction)
        {
            return requestInfo.SetIfVariablePresent<string>(name, setAction);
        }

        public static bool SetIfVariablePresent<TValue>(this RequestInfo requestInfo, VariableKey name, Action<TValue> setAction)
        {
            try
            {
                return requestInfo.Variables.SetIfPresent(name, setAction) || (requestInfo.Visit != null && requestInfo.Visit.Variables.SetIfPresent(name, setAction)) || (requestInfo.Visitor != null && requestInfo.Visitor.Variables.SetIfPresent(name, setAction));
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed setting variable: '{name}'", ex, requestInfo);
                throw;
            }
        }
    }
}
