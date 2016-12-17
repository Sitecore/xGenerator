using System;
using Colossus;
using Newtonsoft.Json.Linq;

namespace ExperienceGenerator.Parsing.Factories
{
    public abstract class VariableFactory
    {
        public virtual void UpdateSegment(VisitorSegment segment, JToken definition, XGenParser parser)
        {
        }

        public virtual void SetDefaults(VisitorSegment segment, XGenParser parser)
        {
        }

        public static VariableFactory Lambda(Action<VisitorSegment, JToken, XGenParser> updateSegment = null, Action<VisitorSegment, XGenParser> setDefaults = null)
        {
            return new LambdaFactory(updateSegment, setDefaults);
        }
    }
}
