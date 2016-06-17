define(["underscore"], function (_) {
  var currItr;
  var host;
  var emptyCampaign;
  var self = this;

  self.itemToListElement = function (item) {
    return {
      itemId: item.itemId,
      $displayName: item.$displayName
    }
  };
  self.updateControls = function (itr) {
    host.PagesInVisitList.unset("selectedItemId", { silent: true });
    host.PagesInVisitList.unset('items', { silent: true });
    host.PagesInVisitList.set('items', itr.pages);
    host.OutcomeList.set('items', itr.outcomes);
    host.GoalList.unset('items');

    var geoId = itr.geoData.GeoNameId;
    var countryId = itr.geoData.Country.IsoNumeric;

    host.City.once('change:items', function () {
      host.City.set('selectedValue', geoId);
    });

    host.TrafficChannelComboBox.set('selectedValue', itr.channelId);
    host.CampaignComboBox.set('selectedValue', itr.campaignId || this.emptyCampaign);
    host.Country.unset('selectedValue', { silent: true });
    host.Country.set('selectedValue', countryId);
    host.SearchEngine.set('selectedValue', itr.searchEngine);
    host.SearchKeyword.set('text', itr.searchKeyword);
    host.RecencyValue.set('text', itr.recency);
  };
  self.getInteraction = function () {
    currItr.channelName = host.TrafficChannelComboBox.get("selectedItem").$displayName;
    currItr.channelId = host.TrafficChannelComboBox.get("selectedItem").itemId;

    currItr.campaignName = host.CampaignComboBox.get("selectedItem").$displayName;

    currItr.campaignId = host.CampaignComboBox.get("selectedItem").itemId != emptyCampaign
      ? host.CampaignComboBox.get("selectedItem").itemId
      : null;

    currItr.recency = parseInt(host.RecencyValue.get("text")) || 0;
    currItr.geoData = host.City.get("selectedItem");
    currItr.location = host.City.get("selectedItem").Name + host.City.get("selectedItem").CountryCode;
    currItr.outcomes = _.map(host.OutcomeList.get("items"), itemToListElement);

    if (currItr.channelName.toLowerCase().indexOf('search') > -1) {
      currItr.searchEngine = host.SearchEngine.get("selectedValue");
      currItr.searchKeyword = host.SearchKeyword.get("text");
    }
    currItr.pages = host.PagesInVisitList.get("items");

    return currItr;
  };
  self.setTrafficChannel = function (model, selected) {
    if (!selected) return;

    this.SearchParams.viewModel.hide();
    if (selected.$displayName.toLowerCase().indexOf('search') > -1) {
      this.SearchParams.viewModel.show();
    }
  };
  self.pageSelected = function (model, selectedItem) {
    if (!selectedItem) return;
    this.GoalList.unset('items', { silent: true });
    this.GoalList.set('items', selectedItem.get('goals') || []);
  };
  self.setGoals = function (model, changed) {
    var selectedPage = host.PagesInVisitList.get("selectedItem");
    if (!selectedPage) return;

    var page = _.find(host.PagesInVisitList.get("items"), function (p) {
      return p.itemId == selectedPage.get("itemId");
    });

    if (!page || !changed) return;
    var goals = _.map(changed, itemToListElement);
    page["goals"] = goals;
    selectedPage.set("goals", goals);
  };

  self.loadCities = function (control, selectedCountry) {
    if (!selectedCountry) return;
    var url = "/api/xgen/cities/" + selectedCountry.IsoNumeric;
    $.ajax({
      url: url,
      type: "GET",
      contentType: "application/json; charset=utf-8",
      dataType: "json",
    }).done(function (data) {
      host.City.unset("items", { silent: true });
      host.City.set("items", _.sortBy(data, 'Name'));
    });
  };
  self.loadCountries = function () {
    var url = "/api/xgen/countries";
    $.ajax({
      url: url,
      type: "GET",
      contentType: "application/json; charset=utf-8",
      dataType: "json",
    }).done(function (data) {
      host.Country.set("items", _.sortBy(data, 'Name'));
    });
  };
  self.guid = function () {
    return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
      var r = Math.random() * 16 | 0, v = c == "x" ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  };

  var interactioneditor = {
    editInteraction: function (itr) {
      currItr = itr;
      updateControls(itr);
      host.InteractionDetailsDialogWindow.show();
    },

    retrieveInteraction: function () {
      host.InteractionDetailsDialogWindow.hide();
      return self.getInteraction();
    },
    initialize: function (appHost) {
      host = appHost;

      self.loadCountries();
      host.CampaignComboBox.once("change:items",
      function (m, sel) {
        self.emptyCampaign = self.guid();
        sel.push({
          $displayName: "None",
          itemId: self.emptyCampaign
        });
        m.setItems(sel);
      }, host);

      host.TrafficChannelComboBox.on("change:selectedItem", self.setTrafficChannel, host);
      host.Country.on("change:selectedItem", self.loadCities, host);
      host.PagesInVisitList.on("change:selectedItem", self.pageSelected, host);
      host.GoalList.on("change:items", self.setGoals, host);
      host.RecencyValue.viewModel.$el.attr("max", "0");
    },
    addPagesOKButton: function () {
      var pages = host.PagesInVisitList.get('items') || [];
      host.AddPageTreeView.viewModel.getSelectedNodes().forEach(function (x) {
        //deselect all
        x.select(false);
        var page = {
          itemId: x.data.key,
          path: x.data.path
        }

        pages.push(page);
      });

      host.PagesInVisitList.unset("items", { silent: true });
      host.PagesInVisitList.set('items', pages);
      host.AddInteractionPagesWindow.hide();
      host.InteractionDetailsDialogWindow.show();
    },
    addPagesCancelButton: function () {
      host.AddInteractionPagesWindow.hide();
      host.InteractionDetailsDialogWindow.show();
    },
    addPageToVisit: function () {
      host.AddInteractionPagesWindow.show();
    }
  };

  return interactioneditor;
});