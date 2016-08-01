define([
  "sitecore",
  "knockout",
  "underscore",
  "/-/speak/v1/exmExperienceGenerator/DataRepository.js",
  "/-/speak/v1/exmExperienceGenerator/uiUtils.js"], function (sc, ko, _, dataRepository, uiUtils) {
  var dialog = _sc.Definitions.App.extend({
    initialized: function () {
      this.cleanUI();
      this.intitializeDevices();
      this.intitializeLocations();
      sc.trigger("campaignEditor:loaded", this);
    },
    showDialog:function(editingParameters) {
      this.EditCampaignDialog.show();
    },
    cleanUI: function () {
      var now = new Date();
      this.StartDate.viewModel.setDate(new Date(now.getFullYear() - 1, now.getMonth(), now.getDate()));
      this.EndDate.viewModel.setDate(now);
    },
    intitializeDevices: function() {
      dataRepository.getDevices(function (devices) {
        var output = uiUtils.renderSliders(devices);
        $("div[data-sc-id='DeviceListInnerPanel']").append(output);
      });
    },
    intitializeLocations: function () {
      dataRepository.getLocations(function (locations) {
        var output = uiUtils.renderSliders(locations);
        $("div[data-sc-id='LocationPanel']").append(output);
      });
    },
    addLandingPages: function () {
      var pages = this.LandingPageTreeView.viewModel.checkedItemIds();
      var nodes = this.LandingPageTreeView.viewModel.getRoot().tree;
      var that = this;
      var $target = $("div[data-sc-id='LandingPagesListInnerPanel']");
      var weights = [];
      $target.find("input.sc-ds-slider").map(function (idx, el) { weights[el.getAttribute('data-sc-id')] = el.value });

      $target.empty();
      nodes.visit(function (childNode) {
        var id = childNode.data.key;
        if (pages.indexOf(id) > -1) {
          var output = uiUtils.renderSliderWithDelete(childNode.data.title, "", id, weights[id], true);
          $target.append(output);
        }
      });
      this.AddLandingPagesDialog.hide();
      this.EditCampaignDialog.show();
    }
  });
  return dialog;
});
