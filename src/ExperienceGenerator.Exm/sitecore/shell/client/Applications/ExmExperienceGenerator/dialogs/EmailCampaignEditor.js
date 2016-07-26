define(["sitecore", "knockout", "underscore"], function (sc, ko, _) {
  var dialog = _sc.Definitions.App.extend({
    initialized: function () {
      sc.trigger("campaignEditor:loaded", this);
    },
    showDialog:function(editingParameters) {
      this.EditCampaignDialog.show();
    }
  });
  return dialog;
});
