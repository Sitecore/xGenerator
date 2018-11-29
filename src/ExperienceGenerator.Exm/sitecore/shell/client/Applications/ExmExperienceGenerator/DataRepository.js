define(["sitecore", "jquery", "underscore"], function (sc, $, _) {
  var urls = {
    devicesUrl: "/clientapi/xgen/devices",
    locationsUrl: "/clientapi/xgen/exmactions/locations"
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
    },

    getItem:function(id, database, callback) {
      database = database || "master";
      var db = new sc.Definitions.Data.Database(new sc.Definitions.Data.DatabaseUri(database));
      db.getItem(id, callback);
    }
  };

  return dataRepository;
});
