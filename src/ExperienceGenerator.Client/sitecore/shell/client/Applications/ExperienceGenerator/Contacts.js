/*/ THIS CODE IS FOR PROTOTYPE ONLY!!!! /*/
define(["sitecore", "knockout", "underscore"], function (_sc, ko, _) {
  var DataSheet = _sc.Definitions.App.extend({
    addContact: function () {
      this.ContactList.unset("selectedItemId");
      var contacts = this.ContactList.get("items");
      var newContact = {
        "itemId": this.guid(),
        "interactions": []
      };

      for (var key in this.bindingMap) {
        if (this.bindingMap.hasOwnProperty(key)) {
          newContact[this.bindingMap[key]] = "";
        }
      }
      contacts.push(newContact);

      this.ContactList.unset("items", { silent: true });
      this.ContactList.set("items", contacts);
      this.selectLastElement(this.ContactList);

    },

    addItemToList: function (sourceControl, targetControl) {
      var selected = this[sourceControl].get('selectedItem');

      if (!selected) return;


      var existingOutcomes = this[targetControl].get('items') || [];
      existingOutcomes.push(selected);
      this[targetControl].unset("items");
      this[targetControl].set("items", existingOutcomes);


    },

    addInteraction: function () {
      var contact = this.ContactList.get("selectedItem");
      if (!contact) return;
      var interactions = contact.get("interactions");
      interactions.push({
        pages: [],
        recency: 0,
        geoData: { Country: {} },
        "itemId": this.guid()
      });

      this.InteractionList.unset("items", { silent: true });
      this.InteractionList.set('items', contact.get('interactions'));
      //how to avoid?

      this.selectLastElement(this.InteractionList);

    },
    deleteSelected: function (controlName) {
      var filteredItem = this[controlName].get("items");

      var checkedItems = this[controlName].get('checkedItems');
      _.each(checkedItems, function (checkedItem) {
        filteredItem = _.without(filteredItem, checkedItem);
      });
      this[controlName].unset("items", { silent: true });
      this[controlName].set('items', filteredItem);
      if (controlName === "InteractionList")
        this.ContactList.get("selectedItem").set("interactions", filteredItem);

    },

    selectLastElement: function (control) {
      control.viewModel.$el.find("tr").eq(-2).find("td:last").click();
    },

    initialized: function () {
      this.landingPages = "";
      this.campaigns = "";
      this.data = {};
      this.jobId = "";
      this.paused = false;
      this.bindingMap = {
        FirstNameValue: "firstName",
        MiddleNameValue: "middleName",
        LastNameValue: "lastName",
        TitleValue: "jobTitle",
        GenderValue: "gender",
        BirthdayValue: "birthday",
        PrimeEmailValue: "email",
        PrimePhoneValue: "phone",
        PrimeAddressValue: "address"
      };



      this.loadCountries();
      //this.initPresetDataSource();

      this.CampaignComboBox.once("change:items",
        function (m, sel) {
          this.emptyCampaign = this.guid();
          sel.push({
            $displayName: "None",
            itemId: this.emptyCampaign
          });
          m.setItems(sel);
        }, this);

      this.ContactList.on("change:selectedItem", this.setEditContactBindings, this);
      this.InteractionList.on("change:selectedItem", this.openEditInteractionModal, this);
      this.TrafficChannelComboBox.on("change:selectedItem", this.setTrafficChannel, this);
      this.CampaignComboBox.on("change:selectedItem", this.setCampaign, this);
      this.RecencyValue.on("change:text", this.setRecency, this);
      this.Country.on("change:selectedItem", this.loadCities, this);
      this.BirthdayValue.on("change:date", function (model, value) {
        model.unset("text");
        model.set("text", value);
      }, this)
      this.City.on("change:selectedItem", function (m, sel) {

        var itr = this.InteractionList.get('selectedItem');
        if (!itr || !sel) return;
        itr.set('location', sel.Name + ' ' + sel.CountryCode);
        itr.set('geoData', sel);
      }, this);
      this.SearchEngine.on("change:selectedValue", function (m, sel) {
        var itr = this.InteractionList.get('selectedItem');
        if (!itr || !sel) return;
        itr.set('searchEngine', sel);
      }, this);
      this.SearchKeyword.on("change:text", function (m, sel) {
        var itr = this.InteractionList.get('selectedItem');
        if (!itr || !sel) return;
        itr.set('searchKeyword', sel);
      }, this);


      this.PagesInVisitList.on("change:selectedItem", this.pageSelected, this);

      this.OutcomeList.on("change:items", this.setOutcomes, this);
      this.GoalList.on("change:items", this.setGoals, this);


      this.applyTwoWayBindings();




    },
    loadCities: function (control, selectedCountry) {
      if (!selectedCountry) return;
      var url = "/api/xgen/cities/" + selectedCountry.IsoNumeric;

      var self = this;
      $.ajax({
        url: url,
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
      }).done(function (data) {
        self.City.set("items", _.sortBy(data, 'Name'), true);
      });
    },
    loadCountries: function () {
      var url = "/api/xgen/countries";
      var self = this;
      $.ajax({
        url: url,
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
      }).done(function (data) {
        self.Country.set("items", data, true);
      });
    },
    applyTwoWayBindings: function () {
      for (var key in this.bindingMap) {
        if (this.bindingMap.hasOwnProperty(key)) {
          this[key].on("change:text", this.updateSelectedContact, this);
        }
      }
    },

    setTrafficChannel: function (model, selected) {
      var target = this.InteractionList.get('selectedItem');

      if (!target || !selected) return;
      this.SearchParams.viewModel.hide();
      if (selected.$displayName.toLowerCase().indexOf('search') > -1) {
        this.SearchParams.viewModel.show();
      }

      target.set("channelName", selected.$displayName);
      target.set("channelId", selected.itemId);


    },

    setCampaign: function (model, selected) {
      var target = this.InteractionList.get('selectedItem');

      if (!target || !selected) return;

      if (selected.itemId == this.emptyCampaign) {
        target.unset("campaignName");
        target.unset("campaignId");
      } else {
        target.set("campaignName", selected.$displayName);
        target.set("campaignId", selected.itemId);
      }



    },

    pageSelected: function (model, selectedItem) {

      if (!selectedItem) return;
      this.GoalList.unset('items');
      this.GoalList.set('items', selectedItem.get('goals'));

    },


    setGoals: function (model, changed) {
      var target = this.PagesInVisitList.get('selectedItem');

      var interaction = this.InteractionList.get('selectedItem');
      if (!target || !changed || !interaction) return;
      interaction.get('pages')[target.collection.indexOf(target)]['goals'] = _.map(changed, function (x) {
        return {
          itemId: x.itemId,
          $displayName: x.$displayName
        }
      });

      this.PagesInVisitList.unset('items');
      this.PagesInVisitList.set('items', interaction.get('pages'));

    },

    setOutcomes: function (model, changed) {
      var target = this.InteractionList.get('selectedItem');

      if (!target || !changed) return;

      target.unset("outcomes");
      target.set("outcomes", _.map(changed, function (x) {
        return {
          itemId: x.itemId, $displayName: x.$displayName
        }
      }));
    },

    setRecency: function (model, changed) {
      var target = this.InteractionList.get('selectedItem');
      if (!target || !changed) return;
      target.set("recency", changed);
    },
    addPagesOKButton: function () {
      var selectedItem = this.InteractionList.get('selectedItem');

      var items = this.AddPageTreeView.viewModel.getSelectedNodes().map(function (x) {
        //deselect all
        x.select(false);

        return {
          itemId: x.data.key,
          path: x.data.path
        }
      });

      for (var idx in items) {
        selectedItem.get('pages').push(items[idx]);
      }

      this.PagesInVisitList.unset('items');
      this.PagesInVisitList.set('items', selectedItem.get('pages'));

      this.AddInteractionPagesWindow.hide();
      this.InteractionDetailsDialogWindow.show();
    },


    interactionsOKButton: function () {
      this.InteractionDetailsDialogWindow.hide();
      for (var idx in this.InteractionList.attributes.items) {
        var obj = this.InteractionList.attributes.items[idx];
        if (this.InteractionList.attributes.selectedItemId == obj.itemId) {
          var viewModel = ko.toJS(this.InteractionList.attributes.selectedItem.viewModel);
          for (var property in viewModel) {
            if (viewModel.hasOwnProperty(property)) {
              obj[property] = viewModel[property];
            }
          }
        }
      }
      this.InteractionList.unset('selectedItemId');
      var contacts = this.ContactList.get('items');

      for (var idx in contacts) {
        var contact = contacts[idx];
        if (this.ContactList.attributes.selectedItemId == contact.itemId) {
          contact.interactions = ko.toJS(this.InteractionList.attributes.items);

        }
      }

      var items = this.InteractionList.get('items');
      this.InteractionList.unset('items');
      this.InteractionList.set('items', items);


    },


    addPageToVisit: function () {
      this.InteractionDetailsDialogWindow.hide();
      this.AddInteractionPagesWindow.show();
    },

    openEditInteractionModal: function (control, selectedItem) {
      if (!selectedItem) return;
      this.PagesInVisitList.set('items', selectedItem.get('pages'));
      this.OutcomeList.set('items', selectedItem.get('outcomes'));
      this.GoalList.unset('items');

      var that = this;
      var geoId = selectedItem.get('geoData').GeoNameId;
      this.City.once('change:items', function () {
        that.City.set('selectedValue', geoId);
      });
      this.TrafficChannelComboBox.set('selectedValue', selectedItem.get('channelId'));
      this.CampaignComboBox.set('selectedValue', selectedItem.get('campaignId') || this.emptyCampaign);
      this.Country.set('selectedValue', selectedItem.get('geoData').Country.IsoNumeric);
      this.SearchEngine.set('selectedValue', selectedItem.get('searchEngine'));
      this.SearchKeyword.set('text', selectedItem.get('searchKeyword'));
      this.RecencyValue.set('text', selectedItem.get('recency'));
      this.InteractionDetailsDialogWindow.show();
    },
    updateSelectedContact: function (model) {
      var key = model.get("name");
      for (var idx in this.ContactList.attributes.items) {
        var contact = this.ContactList.attributes.items[idx];
        if (contact.itemId == this.ContactList.attributes.selectedItemId) {
          contact[this.bindingMap[key]] = this[key].get("text");
        }
      }
      this.ContactList.get("selectedItem").set(this.bindingMap[key], this[key].get("text"));
    },

    setEditContactBindings: function (control, selectedItem) {
      if (!selectedItem) {
        return;
      }

      for (var key in this.bindingMap) {
        if (this.bindingMap.hasOwnProperty(key)) {
          this[key].set("text", selectedItem.get(this.bindingMap[key]));
        }
      }
      this["BirthdayValue"].set("date", selectedItem.get(this.bindingMap["BirthdayValue"]));
      this.InteractionList.set('items', selectedItem.get('interactions'));
    },

    initPresetDataSource: function () {
      var url = "/api/xgen/presetquery";
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

    loadOptions: function () {
      var that = this;
      $.ajax({
        url: "/api/xgen/options"
      }).done(function (data) {
        that.populate(data);
      });
    },

    guid: function () {
      return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == "x" ? r : (r & 0x3 | 0x8);
        return v.toString(16);
      });
    },


    dialogCancelButton: function (DialogWindow) {
      this[DialogWindow].hide();
    },


    deleteData: function () {
      this.DelWindow.hide();
      $.ajax({
        url: "/api/xgen/flush",
        type: "POST"
      });
    },

    pause: function () {
      this.paused = !this.paused;
      //ToDo: toggle event listener for intervalCompleted:ProgressBar
      if (this.paused) {
        _sc.off("intervalCompleted:ProgressBar");
        $.ajax({
          url: "/api/xgen/jobs/" + this.jobId + "?pause=true",
          type: "GET"
        });
      } else {
        _sc.on("intervalCompleted:ProgressBar", this.updateProgress, this);
        $.ajax({
          url: "/api/xgen/jobs/" + this.jobId + "?pause=false",
          type: "GET"
        });
      }
    },
    stop: function () {
      if (this.jobId == "") {
        //No job has ever been started
        return;
      }
      _sc.off("intervalCompleted:ProgressBar");
      $.ajax({
        url: "/api/xgen/jobs/" + this.jobId,
        type: "DELETE",
      }).done(function (data) {
        //Do something here when job is aborted
      });
    },

    start: function () {
      this.data = this.adapt(ko.toJS(this.ContactList.get('items')));
      console.log(this.data);
      //console.log(JSON.stringify(this.data, null, 2));
      this.data = JSON.stringify(this.data);
      var that = this;
      $.ajax({
        url: "/api/xgen/jobs",
        type: "POST",
        data: this.data,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function () { }
      }).done(function (data) {
        that.running(data);
      });
    },

    running: function (data) {
      this.jobId = data.Id;
      _sc.on("intervalCompleted:ProgressBar", this.updateProgress, this);
    },

    updateProgress: function () {
      var jobId = this.jobId;
      var that = this;
      $.ajax({
        url: "/api/xgen/jobs/" + that.jobId,
        type: "GET",
      }).done(function (data) {
        var total = 0;
        var contacts = data.Specification.Specification.Contacts;
        for (var i = 0; i < contacts.length; i++) {
          total += contacts[i].interactions.length;
        }

        that.ProgressBar.set("value", data.Progress / total * 100);
        that.NumberVisitsValue.set("text", data.CompletedVisits);
        if (data.JobStatus != "Running" && data.JobStatus != "Pending" && data.JobStatus != "Paused") {
          _sc.off("intervalCompleted:ProgressBar");
        }
      });
    },


    loadPreset: function () {
      var self = this;
      var selectedItem = this.PresetList.attributes.selectedItemId;
      var url = "/api/xgen/contactsettingspreset?id=" + selectedItem;
      $.ajax({
        url: url,
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function () { }
      }).done(function (data) {
        _.each(data, function (item) { item['itemId'] = self.guid(); });
        self.ContactList.set('selectedItem', null);
        self.ContactList.set('items', data);
      });
    },

    save: function (name) {
      var self = this;
      var name = this.PresetName.attributes.text;
      if (name == "") {
        alert("Please enter preset name.");
      } else {
        this.data = { spec: ko.toJS(this.ContactList.get('items')), name: name };
        console.log(this.data);

        this.data = JSON.stringify(this.data);
        $.ajax({
          url: "/api/xgen/SaveContactsSettings",
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


    adapt: function (doc) {

      return {
        Type: 1,
        VisitorCount: doc.length,
        Specification: {
          Contacts: doc,
          Segments: {

          }
        }
      }

    }



  });
  return DataSheet;
});