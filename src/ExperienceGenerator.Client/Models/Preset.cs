using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExperienceGenerator.Client.Models
{
  using Newtonsoft.Json.Linq;

  public class Preset
  {
    public string Name { get; set; }

    public JObject Spec { get; set; }
  }
}