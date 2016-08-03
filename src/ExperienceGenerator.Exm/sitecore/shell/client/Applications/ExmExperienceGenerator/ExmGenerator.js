define(["sitecore", "knockout", "underscore", "/-/speak/v1/exmExperienceGenerator/JobManager.js"], function (_sc, ko, _, jobManager) {
  var exmGeneratorApp = _sc.Definitions.App.extend({
    initialized: function () {
      this.generatorData = [];
      _sc.on("campaignEditor:loaded", function (editor) {
        this.campaignEditor = editor;
        this.campaignEditor.on("campaignEditor:submit", this.updateCampaignData, this);
        this.campaignEditor.on("campaignEditor:cancel", function () {
          this.SentCampaignsList.unset("selectedItemId");
        }, this);
      }, this);

      this.DialogLoadOnDemandPanel.refresh();

      this.SentCampaignsList.on("change:selectedItem", function (list, selectedItem) {
        if (!selectedItem) return;

        this.campaignEditor.showDialog(this.generatorData[selectedItem.get("itemId")]);
      }, this);
    },
    updateCampaignData: function (data) {
      var selectedItem = this.SentCampaignsList.get("selectedItem");
      if (!selectedItem) return;

      var campaignId = selectedItem.get("itemId");
      this.generatorData[campaignId] = data;
      this.SentCampaignsList.unset("selectedItemId");
    },

    // Run exm jobs
    start: function () {
      var self = this;
      jobManager.start(this.generatorData, function (data) {
        self.jobId = data.Id;
        _sc.on("intervalCompleted:ProgressBar", this.updateJobStatus, this);
      },function (error) {
        alert(error);
      });
    },
    stop: function () {
      var self = this;
      jobManager.stop(this.jobId, function() {
        self.jobId = undefined;
      });
    },
    pause: function () {
      console.error("Pause isn't supported");
    },
    updateJobStatus: function() {
      var jobId = this.jobId;
      var self = this;
      jobManager.getStatus(jobId, function(data) {
        

        //self.ProgressBar.set("value", data.CompletedVisitors / total * 100);
        //self.NumberVisitsValue.set("text", data.CompletedVisits);
        if (data.JobStatus != "Running" && data.JobStatus != "Pending" && data.JobStatus != "Paused") {
          _sc.off("intervalCompleted:ProgressBar");
        }
      });
    }
  });
  return exmGeneratorApp;
});
