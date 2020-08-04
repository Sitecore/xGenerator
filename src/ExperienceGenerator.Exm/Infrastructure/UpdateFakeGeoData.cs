using System;
using System.Linq;
using ExperienceGenerator.Repositories;
using Sitecore.Analytics.Pipelines.CommitSession;

namespace ExperienceGenerator.Exm.Infrastructure
{
    public class UpdateFakeGeoData : CommitSessionProcessor
    {
        private readonly GeoDataRepository _geoDataRepository;

        public UpdateFakeGeoData()
        {
            _geoDataRepository = new GeoDataRepository();
        }

        public override void Process(CommitSessionPipelineArgs args)
        {
            if (args.Session?.Interaction?.UserAgent == null)
                return;
            var userAgent = args.Session?.Interaction?.UserAgent;
            var substrings = userAgent.Split(new [] {';', ')', '('}, StringSplitOptions.RemoveEmptyEntries);
            var cityComment = substrings.FirstOrDefault(s => s.Trim().StartsWith("city:"));
            if (cityComment == null)
                return;
            var cityIdString = cityComment.Substring(5);
            int cityId;
            if (!int.TryParse(cityIdString, out cityId))
                return;
            var city = _geoDataRepository.CityByID(cityId);
            if (city == null)
                return;
            args.Session.Interaction.SetWhoIsInformation(city.ToWhoIsInformation());
            args.Session.Interaction.UpdateLocationReference();
        }
    }
}
