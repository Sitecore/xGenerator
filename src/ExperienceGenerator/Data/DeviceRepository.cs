using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ExperienceGenerator.Data
{
    public class DeviceRepository
    {
        private static readonly object lockObject = new object();

        public DeviceRepository()
        {
            if (DeviceCache.Count != 0)
                return;
            lock (lockObject)
            {
                if (DeviceCache.Count == 0)
                {
                    LoadDevices().ToList().ForEach(x => DeviceCache.TryAdd(x.Name, x));
                }
            }
        }

        public IEnumerable<Device> GetAll()
        {
            return DeviceCache.Values;
        }

        private static IEnumerable<Device> LoadDevices()
        {
            return FileHelpers.ReadLinesFromResource<Device>("ExperienceGenerator.Data.devices.txt").Select(x => x.Trim()).Where(x => !x.StartsWith("#") && !string.IsNullOrWhiteSpace(x)).Select(Device.FromCsv);
        }

        private static readonly ConcurrentDictionary<string, Device> DeviceCache = new ConcurrentDictionary<string, Device>();
    }
}
