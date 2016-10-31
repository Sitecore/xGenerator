define(["sitecore", "knockout", "underscore"], function (sc, ko, _)
{
  // Clears checkedItems array when control gets new data.
  function addRefreshEventHandler(control)
  {
    var handlers = control.model.on("change:items", function()
    {
      this.model.set("checkedItems", []);
    }, control)._events['change:items'];

    handlers.unshift(handlers.pop());
  }

  function restoreRowCheck(control, rowElement)
  {
    var rowItem = getRowData(rowElement);
    if(rowItem.itemId && _.contains(control.model.get("checkedItemIds"), rowItem.itemId()))
    {
      updateCheckBox(rowElement, true);
    }
  }
  
  function convertToIdArray(items)
  {
    if(!items[0].itemId)
    {
      return items;
    }

    return _.map(items, function(item) { return item.itemId; });
  }

  function setChecked(control, itemIds, checked)
  {
    _.each(control.allCheck, function(rowElement)
    {
      var rowItem = getRowData(rowElement);
      
      if(_.contains(itemIds, rowItem.itemId()))
      {
        updateCheckBox(rowElement, checked);
      }
    });
  }
  
  function updateCheckBox(el, checked)
  {
    var checkBox = el.find(".sc-cb");
    checkBox.prop("checked", checked);
    checkBox.trigger("change");
  }
  
  function getRowData(rowElement)
  {
    return ko.contextFor(rowElement[0]).$data;
  }

  //function clearChecksOnSelect() {
  //  if (!this.model.get("selectedItem")) {
  //    return;
  //  }

  //  this.globalCheck.prop("checked", false);
  //  this.checkAll();
  //}

  function checkAllItems(model) {
    var items = model.get("items");
    var checkedItemIds = model.get("checkedItemIds");
    var checkedItems = model.get("checkedItems");

    var addedItems = [];
    var addedIds = [];

    _.each(items, function (item) {
      if (!_.contains(checkedItemIds, item.itemId) && !_.contains(addedIds, item.itemId)) {
        addedIds.push(item.itemId);
      }

      if (!_.contains(checkedItems, item)) {
        addedItems.push(item);
      }
    });

    var newCheckedItems = [].concat(items);
    model.set("checkedItems", newCheckedItems, { silent: true });
    model.trigger("change:checkedItems", createChangeArgument(addedItems, []));

    // remove this trigger when breaking changes are allowed
    model.trigger("change", model, newCheckedItems);

    model.set("checkedItemIds", checkedItemIds.concat(addedIds), { silent: true });
    model.trigger("change:checkedItemIds", createChangeArgument(addedIds, []));
  }

  function uncheckAllItems(model) {
    var removedItems = model.get("checkedItems");
    var removedIds = model.get("checkedItemIds");

    var newCheckedItems = [];
    model.set("checkedItems", newCheckedItems, { silent: true });
    model.trigger("change:checkedItems", createChangeArgument([], removedItems));

    // remove this trigger when breaking changes are allowed
    model.trigger("change", model, newCheckedItems);

    model.set("checkedItemIds", [], { silent: true });
    model.trigger("change:checkedItemIds", createChangeArgument([], removedIds));
  }

  function createChangeArgument(added, removed) {
    return { added: added, removed: removed };
  }

  sc.Factories.createBehavior("MultiSelectList", {
    events: {
      "change .sc-cb": "check",
      "click .cb": "checkCell",
      "change .sc-cball": "checkAll"
    },
    initialize: function() {
    },
    beforeRender: function () {
      this.model.set("checkedItems", []);
      this.model.set("checkedItemIds", []);

      addRefreshEventHandler(this);

      //this.model.on("change:selectedItem", clearChecksOnSelect, this);
    },
    afterRender: function () {
      this.allCheck = [];
      var items = this.model.get("items");
      if (items && items.length) {
        _.each(this.$el.find("thead tr"), this.insertGlobalcheck, this);
        _.each(this.$el.find(".sc-table tbody tr"), this.insertcheck, this);
        this.globalCheck = this.$el.find(".sc-cball");
        this.on("addrow", this.addrow);
      }
    },
    addrow: function () {
      this.insertcheck(this.$el.find(".sc-table tbody tr").last());
    },
    insertGlobalcheck: function (row) {
      var checkbox = "<th class='cb'><input class='sc-cball' type='checkbox' onclick='event.stopPropagation();' /></th>";
      $(row).prepend(checkbox);
      this.model.trigger("globalcheck:inserted");
    },
    insertcheck: function (el) {
      var $el = $(el);
      if (!$el.hasClass("empty")) {
        var checkbox = "<td class='cb'><input class='sc-cb' type='checkbox' onclick='event.stopPropagation();' /></td>";
        $el.prepend(checkbox);
        this.allCheck.push($el);
      } else {
        $el.prepend("<td></td>");
      }

      restoreRowCheck(this, $el);
    },
    
    checkItem: function (item)
    {
      this.checkItems([item]);
    },

    checkItems: function (items)
    {
      if(items.length < 1)
      {
        return;
      }

      items = convertToIdArray(items);

      var checkedItemIds = this.model.get("checkedItemIds");
      var newCheckedItemIds = _.union(checkedItemIds, items);
      var addedItemIds = _.chain(items).difference(checkedItemIds).uniq().value();

      if (checkedItemIds.length !== newCheckedItemIds.length) {
        this.model.set("checkedItemIds", newCheckedItemIds, { silent: true });
        this.model.trigger("change:checkedItemIds", createChangeArgument(addedItemIds, []));
        setChecked(this, items, true);
      }
    },
    
    uncheckItem: function (item)
    {
      this.uncheckItems([item]);
    },
    
    uncheckItems: function(items)
    {
      if(items.length < 1)
      {
        return;
      }

      items = convertToIdArray(items);

      var checkedItemIds = this.model.get("checkedItemIds");
      var newCheckedItemIds = _.difference(checkedItemIds, items);
      var removedItemIds = _.chain(items).intersection(checkedItemIds).uniq().value();

      if (checkedItemIds.length !== newCheckedItemIds.length) {
        this.model.set("checkedItemIds", newCheckedItemIds, { silent: true });
        this.model.trigger("change:checkedItemIds", createChangeArgument([], removedItemIds));
        setChecked(this, items, false);
      }
    },

    checkAll: function () {
      var isChecked = this.globalCheck.is(":checked");
      var $checkBoxes = this.$el.find(".sc-cb");

      if (isChecked) {
        this.model.set("selectedItemId", null);
        this.currentSelection = [];

        checkAllItems(this.model);
        $checkBoxes.closest("tr").addClass("checked");
      } else {
        uncheckAllItems(this.model);
        $checkBoxes.closest("tr").removeClass("checked");
      }

      $checkBoxes.prop("checked", isChecked);
      this.$el.find(".sc-cball").prop("checked", isChecked);
    },
        
    checkCell: function (evt) {
      var $current = $(evt.currentTarget);
      if ($current.hasClass("sc-cb"))
        return;
      if ($current.hasClass("sc-cball"))
        return;
      var checkBox = $current.find(".sc-cb");
      if (checkBox.length == 0)
        checkBox = $current.find(".sc-cball");
      if (checkBox.length == 0)
        return;
      checkBox.prop("checked", !checkBox.is(":checked"));
      checkBox.trigger("change");
    },

    check: function (evt) {
      var $current = $(evt.currentTarget),
          $row = $current.closest("tr"),
          rowItem,
          checkedItemsResult,
          checkedItemIdsResult;
      
      rowItem = this.model.get("items")[$row.index()];

      var colItems = this.model.get("checkedItems"),
          colItemIds = this.model.get("checkedItemIds");

      if ($current.is(":checked")) {
        var containsItemId = _.contains(colItemIds, rowItem.itemId),
            containsItem = _.contains(colItems, rowItem);

        if (!containsItem) {
          colItems.push(rowItem);
          this.model.set("checkedItems", colItems, { silent: true });
          this.model.trigger("change", this.model, colItems);
          this.model.trigger("change:checkedItems", createChangeArgument([rowItem], []));
        }

        if (!containsItemId) {
          colItemIds.push(rowItem.itemId);
          this.model.set("checkedItemIds", colItemIds, { silent: true });
          this.model.trigger("change:checkedItemIds", createChangeArgument([rowItem.itemId], []));
        }

        this.model.set("selectedItemId", null);
        $current.closest("tr").addClass("checked");
      } else {
        checkedItemsResult = _.filter(colItems, function (item) {
          return item !== rowItem;
        });

        if (checkedItemsResult.length !== colItems.length) {
          this.model.set("checkedItems", checkedItemsResult, { silent: true });
          this.model.trigger("change", this.model, checkedItemsResult);
          this.model.trigger("change:checkedItems", createChangeArgument([], [rowItem]));
        }

        checkedItemIdsResult = _.filter(colItemIds, function (itemId) {
          return itemId !== rowItem.itemId;
        });

        if (checkedItemIdsResult.length !== colItemIds.length) {
          this.model.set("checkedItemIds", checkedItemIdsResult, { silent: true });
          this.model.trigger("change:checkedItemIds", createChangeArgument([], [rowItem.itemId]));
        }

        $current.closest("tr").removeClass("checked");
      }

      var rowsNumber = this.$el.find(".sc-table tbody tr").length;
      var checkedRows = this.$el.find(".sc-cb:checked").length;

      this.globalCheck.prop("checked", (checkedRows > 0 && checkedRows === rowsNumber));
    }
  });
});