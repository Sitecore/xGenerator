using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Colossus.Web;

namespace Colossus.Integration.Processing
{
    public static class GetSetHelpers
    {

        public static RequestInfo ColossusInfo(this HttpContext ctx)
        {
            if (ctx == null) return null;

            var info = ctx.Items[DataEncoding.RequestDataKey] as RequestInfo;
            

            if (info == null)
            {
                var data = ctx.Request.Headers.GetChunked(DataEncoding.RequestDataKey);
                if (data != null)
                {
                    ctx.Items[DataEncoding.RequestDataKey] = info = DataEncoding.DecodeHeaderValue<RequestInfo>(data);
                }
            }

            return info;
        }

        
        public static TValue TryGetValue<TObject, TValue>(this TObject o, Func<TObject, TValue> getter,
            TValue defaultValue = default(TValue))
        {
            return o == null ? defaultValue : getter(o);
        }

        public static bool SetIfPresent(this IDictionary<string, object> dict, string key, Action<string> action)
        {
            return dict.SetIfPresent<string>(key, action);
        }

        public static bool SetIfPresent<TValue>(this IDictionary<string, object> dict, string key, Action<TValue> action)
        {
            object val;
            if (dict.TryGetValue(key, out val))
            {
                if (val is TValue)
                {
                    action((TValue) val);
                    return true;
                }
            }

            return false;
        }
    }
}
