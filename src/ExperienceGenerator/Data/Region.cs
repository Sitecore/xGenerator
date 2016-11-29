namespace ExperienceGenerator.Data
{
    public class Region
    {
        public static Region FromCsv(string[] values)
        {
            var index = 0;
            return new Region
            {
                CountryCode = values[index++],
                RegionCode = values[index++],
                Name = values[index],
            };
        }

        public string Name { get; set; }

        public string RegionCode { get; set; }

        public string CountryCode { get; set; }
    }
}
