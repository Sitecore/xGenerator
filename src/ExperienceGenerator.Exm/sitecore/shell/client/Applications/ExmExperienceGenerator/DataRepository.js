define(["sitecore", "jquery", "underscore"], function (sc, $, _) {
  var urls = {
    devicesUrl: "/api/xgen/devices",
    locationsUrl: "/api/xgen/locations"
  };

  var dataRepository = {
    getDevices: function (callback) {
      $.ajax({
        url:urls.devicesUrl
      }).success(function (data) {
        _.each(data, function (item) {
          item.Label = item.Name;
        });

        var groupedDevices = _.map(_.groupBy(data, "Type"), function (item, idx) {
          return {
            Label: idx,
            Options: item
          }
        });
        callback(groupedDevices);
      });
    },
    getLocations: function (callback) {
      $.ajax({
        url: urls.locationsUrl
      }).success(callback);
    }
  };

  return dataRepository;
});