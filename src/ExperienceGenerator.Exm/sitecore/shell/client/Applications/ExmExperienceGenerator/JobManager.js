define(["sitecore", "jquery", "underscore"], function (_sc, $, _) {
  var urls = {
    exmJobUrl: "/api/xgen/exmjobs/"
  };

  var jobManager = {
    stop: function (jobId, callback) {
      if (jobId === "") return;

      $.ajax({
        url: urls.exmJobUrl + jobId,
        type: "DELETE",
        success: callback
      });
    },

    start: function (data, callback, errorCallback) {
      $.ajax({
        url: urls.exmJobUrl,
        type: "POST",
        data: JSON.stringify(data),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: callback,
        error: errorCallback
      });
    },
    getStatus: function(jobId, callback) {
      $.ajax({
        url: urls.exmJobUrl + jobId,
        type: "GET",
        success: callback
      });
    }
  };

  return jobManager;
});