using System.Collections.Generic;

namespace Colossus
{
    public interface IVisitorBehavior
    {
        IEnumerable<Visit> Commit(Visitor visitor);
    }
}
