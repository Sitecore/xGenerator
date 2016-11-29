using System.Collections.Generic;

namespace Colossus
{
  public interface IVisitSimulator
  {
    IEnumerable<Visitor> GetVisitors(int count);
  }
}
