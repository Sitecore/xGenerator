define(["sitecore", "knockout", "underscore"], function (_sc, ko, _) {
  var exmGeneratorApp = _sc.Definitions.App.extend({
    initialized: function () {
      this.generatorData = [];
      _sc.on("campaignEditor:loaded", function (editor) {
        this.campaignEditor = editor;
        this.campaignEditor.on("campaignEditor:submit", this.updateCampaignData, this);
        this.campaignEditor.on("campaignEditor:cancel", function() {
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
    }
  });
  return exmGeneratorApp;
});
