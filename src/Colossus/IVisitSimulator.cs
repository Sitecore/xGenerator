using System.Collections.Generic;

namespace Colossus
{
  public interface IVisitSimulator
  {
    Visitor NextVisitor();
    IEnumerable<Visitor> NextVisitors(int count, bool sort = true);
  }
}