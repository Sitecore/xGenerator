using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExperienceGenerator.Client
{
  using Sitecore.Data;
  using Sitecore.Shell.Framework.Commands.System;

  public static class Templates
  {
    public struct Preset
    {
      public static ID ID = new ID("{C7089EBD-7AF0-4F14-BEEA-680A465231B1}");

      public struct Fields
      {
        public static readonly ID Specification = new ID("{A0EE2080-2CC8-4DE0-8EFD-8E29CD39194F}");
      }
    }

  
  }
}