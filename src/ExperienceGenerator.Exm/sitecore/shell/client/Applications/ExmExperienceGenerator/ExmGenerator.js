define(["sitecore", "knockout", "underscore", "/-/speak/v1/exmExperienceGenerator/JobManager.js"], function (_sc, ko, _, jobManager) {
  var exmGeneratorApp = _sc.Definitions.App.extend({
    initialized: function () {
      this.generatorData = {};
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

    initPresetDataSource: function () {
      var url = "/api/xgen/exmactions/presetquery";
      var self = this;
      $.ajax({
        url: url,
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
      }).done(function (data) {
        self.DataSource.set("query", data.query);
        self.DataSource.refresh();
      });
    },

    loadPreset: function () {
      var self = this;
      var selectedItem = this.ExmPresetList.attributes.selectedItemId;
      var url = "/api/xgen/exmactions/settingspreset?id=" + selectedItem;
      $.ajax({
        url: url,
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function () { }
      }).done(function (data) {
        self.generatorData = data;
      });
    },

    save: function () {
      var self = this;
      var name = this.PresetName.attributes.text;
      if (name == "") {
        alert("Please enter preset name.");
      } else {
        if (_.any(this.DataSource.get("items"), function (item) { return item.itemName === name })) {
          var overwrite = confirm("Are you sure you want to overwrite settings?");
          if (!overwrite) return;
        }
        this.data = { spec: ko.toJS(this.generatorData), name: name, force: true };

        this.data = JSON.stringify(this.data);
        $.ajax({
          url: "/api/xgen/exmactions/SaveSettings",
          type: "POST",
          data: this.data,
          dataType: "json",
          contentType: "application/json; charset=utf-8",
          success: function () { }
        }).done(function (data) {
          self.PresetName.set("text", "");
          self.DataSource.refresh();
        }).fail(function (data) {
          alert(data.responseJSON.ExceptionMessage);
        });
      }
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
      var checkedItems = this.SentCampaignsList.get("checkedItems");
      var checkedItemsNo = checkedItems.length;
      if (checkedItems.length <= 0) {
        alert("Please select at least one campaign.");
        return;
      }
      var requestData = {};
      for (var i = 0; i < checkedItemsNo; i++) {
        var itemId = checkedItems[i].itemId;
        requestData[itemId] = this.adaptDayDistribution(this.generatorData[itemId]);
      }
      var that = this;
      $.ajax({
        url: "/api/xgen/exmjobs/",
        type: "POST",
        data: JSON.stringify(requestData),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function () { }
      }).done(function (data) {
        that.running(data);
      });
    },

    running: function (data) {
      this.jobId = data.Id;
      _sc.on("intervalCompleted:ProgressBar", this.updateJobStatus, this);
    },

    stop: function () {
      var self = this;
      jobManager.stop(this.jobId, function () {
        self.jobId = undefined;
      });
    },
    pause: function () {
      console.error("Pause isn't supported");
    },
    deleteData: function () {
      console.error("Delete isn't supported");
      //this.DelWindow.hide();
      //$.ajax({
      //  url: "/api/xgen/exmactions/flush",
      //  type: "POST"
      //});
    },
    updateJobStatus: function () {
      var jobId = this.jobId;
      var self = this;
      console.log("Update job status for job: " + jobId);
      $.ajax({
        url: "/api/xgen/exmjobs/" + jobId,
        type: "GET"
      }).done(function (data) {
        
        self.ProgressBar.set("value", data.Progress * 100);
        self.CampaignCountText.set("text", data.CampaignCountLabel);
        self.StatusText.set("text", data.Status);
        if (data.JobStatus === 6 || data.JobStatus === "Completed") {
          _sc.off("intervalCompleted:ProgressBar");
        }
      });
    },
    adaptDayDistribution: function (data) {
      var oneDay = 24 * 60 * 60 * 1000; // hours*minutes*seconds*milliseconds
      var firstDate = new Date(data.endDate);
      var secondDate = new Date(data.startDate);
      var totalDays = Math.round(Math.abs((firstDate.getTime() - secondDate.getTime()) / (oneDay)));
      var result = _.clone(data);

      if (totalDays < 6) {
        result.dayDistribution = _.first(data.dayDistribution, totalDays);
      } else {

        var interval = totalDays / 5;
        var adaptedDays = [];
        for (var i = 0; i < totalDays; i++) {
          // plus used for converting to numeric from string
          var prevWeight = +data.dayDistribution[Math.floor(i / interval)];
          var nextWeight = +data.dayDistribution[Math.ceil(i / interval)];
          var weight = prevWeight + (i - Math.floor(i / interval) * interval) * (nextWeight - prevWeight) / interval;
          adaptedDays.push(weight);
        }

        result.dayDistribution = adaptedDays;
      }

      return result;
    }
  });
  return exmGeneratorApp;
});
