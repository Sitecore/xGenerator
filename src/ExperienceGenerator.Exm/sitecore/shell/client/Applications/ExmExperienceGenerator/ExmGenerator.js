define(["sitecore", "knockout", "underscore"], function(_sc, ko, _) {
  var exmGeneratorApp = _sc.Definitions.App.extend({
    initialized: function () {
      _sc.on("campaignEditor:loaded", function (editor) {
        this.campaignEditor = editor;
      }, this);

      this.DialogLoadOnDemandPanel.refresh();
      

      this.SentCampaignsList.on("change:selectedItem", function() {
        this.campaignEditor.showDialog();
      },this);
    }
  });
  return exmGeneratorApp;
});
