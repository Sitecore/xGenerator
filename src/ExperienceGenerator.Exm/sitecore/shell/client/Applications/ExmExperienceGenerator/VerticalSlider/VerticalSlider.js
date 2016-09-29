require.config({
  paths: {
    bootstrapSlider: "/sitecore/shell/client/Applications/ExmExperienceGenerator/VerticalSlider/bootstrap-slider",
    sliderCss: "/sitecore/shell/client/Applications/ExmExperienceGenerator/VerticalSlider/VerticalSlider"
  }
});

define(["sitecore", "bootstrapSlider", "css!sliderCss"], function (_sc) {
  _sc.Factories.createBaseComponent({
    name: "VerticalSlider",
    base: "ComponentBase",
    selector: ".sc-VerticalSlider",
    sliderComponent: null,
    sliderControl: null,
    sliderValueChanged: false,
    attributes: [
      { name: "minimum", defaultValue: null, value: "$el.data:sc-minimum" },
      { name: "maximum", defaultValue: null, value: "$el.data:sc-maximum" },
      { name: "step", defaultValue: null, value: "$el.data:sc-step" },
      { name: "titleValuesStep", defaultValue: null, value: "$el.data:sc-titlevaluesstep" },
      { name: "value", defaultValue: null, value: "$el.data:sc-value" },
      { name: "showTooltip", defaultValue: null },
      { name: "selectedValue", defaultValue: null, value: "$el.data:sc-selectedvalue" },
      { name: "selectedValueStart", defaultValue: null, value: "$el.data:sc-selectedvaluestart" },
      { name: "selectedValueEnd", defaultValue: null, value: "$el.data:sc-selectedvalueend" },
      { name: "selectedItem", defaultValue: null },
      { name: "selectedItems", defaultValue: null },
      { name: "items", defaultValue: null, value: "$el.data:sc-items" },
      { name: "displayFieldName", defaultValue: null, value: "$el.data:sc-displayfieldname" },
      { name: "valueFieldName", defaultValue: null, value: "$el.data:sc-valuefieldname" },
      { name: "type", defaultValue: null, value: "$el.data:sc-type" },
      { name: "isEnabled", defaultValue: null, value: "$el.data:sc-isenabled" },
      { name: "isVisible", defaultValue: null, value: "$el.data:sc-isvisible" },
      { name: "isTitleBar", defaultValue: null, value: "$el.data:sc-istitlebar" },
      { name: "hideTooltip", defaultValue: null, value: "$el.data:sc-hidetooltip" },
      { name: "titleValueSuffix", defaultValue: null, value: "$el.data:sc-titlevaluesuffix" },
      { name: "titleValueAffix", defaultValue: null, value: "$el.data:sc-titlevalueaffix" }

    ],
    initialize: function () {
      var items = this.model.get("items");
      this.model.on("change:isEnabled", this.toggleEnable, this);
      this.model.on("change:items", this.initializeSlider, this);
      this.model.on("change:selectedValue", this.selectedValueChanged, this);


      this.initializeSlider(this, items);
    },

    /// <summary>
    /// Initialize the slider control.
    /// <param name="items">The items array.</param>
    /// </summary>
    initializeSlider: function (sender, items) {
      var inputControl,
        sliderValue,
        that = this,
        hasItems = (items && items.length > 0),
        sliderParent;

      var options = {
        formater: function (value) {
          var displayValue = value;
          if (hasItems && value < items.length) {
            displayValue = items[value].itemName;
          }

          if (!displayValue) {
            displayValue = Math.round(value * 10) / 10;
          }

          return displayValue;
        },

        getImageUrl: function (value) {
          var imageUrl;
          if (hasItems) {
            imageUrl = items[value].imageUrl;
          }

          return imageUrl;
        },

        isTitleBar: this.model.get("isTitleBar"),
        hideTooltip: this.model.get("hideTooltip"),
        titleValueSuffix: this.model.get("titleValueSuffix"),
        titleValueAffix: this.model.get("titleValueAffix")
      };

      this.validateMinMax(items, hasItems);

      inputControl = this.$el.find(".sliderControl");
      inputControl.attr("data-slider-min", this.model.get("minimum"));
      inputControl.attr("data-slider-max", this.model.get("maximum"));
      inputControl.attr("data-slider-step", this.model.get("step"));
      inputControl.attr("data-slider-titlevaluesstep", this.model.get("titleValuesStep"));

      sliderValue = this.calculateSliderValue(
        items,
        this.model.get("minimum"),
        this.model.get("maximum"),
        this.model.get("selectedValue"),
        this.model.get("selectedValueStart"),
        this.model.get("selectedValueEnd"),
        this.model.get("type"));

      inputControl.attr("data-slider-value", sliderValue);

      // If it's already a slider control, then this will "reset"
      sliderParent = inputControl.parents(".slider");
      if (sliderParent.length > 0) {
        sliderParent.before(inputControl);
        sliderParent.remove();
      }

      var slider = inputControl.bootstrapSlider(options);
      this.sliderComponent = slider.data("VerticalSlider");
      this.sliderControl = slider[0];
      this.toggleEnable();

      slider.on('slide', function (ev) {
        that.sliderValueChanged = true;
        if (Array.isArray(ev.value)) {
          if (hasItems) {
            that.model.set("selectedValueStart", items[ev.value[0]].value);
            that.model.set("selectedValueEnd", items[ev.value[1]].value);
            that.model.set("selectedItem", items[ev.value[0]]);
            that.model.set("selectedItems", "{" + items[ev.value[0]] + "," + items[ev.value[1]] + "}");
          } else {
            that.model.set("selectedValueStart", ev.value[0]);
            that.model.set("selectedValueEnd", ev.value[1]);
            that.model.set("selectedItem", null);
            that.model.set("selectedItems", null);
          }
        } else {
          if (hasItems) {
            that.model.set("selectedValue", items[ev.value].value);
            that.model.set("selectedItem", items[ev.value]);
            that.model.set("selectedItems", items[ev.value]);
          } else {
            that.model.set("selectedValue", ev.value);
            that.model.set("selectedItem", null);
            that.model.set("selectedItems", null);
          }
        }
      });
    },

    selectedValueChanged: function (sender, value) {
      if (!this.sliderValueChanged) {
        
        //this.slider.setValue(value);
        this.model.set("value", value);
      }
      this.sliderValueChanged = false;
      var items = this.model.get("items");
      if (items && value < items.length)
        this.model.set("selectedItem", items[value]);
    },

    /// <summary>
    /// Validate Mimimum and Maximum.
    /// </summary>
    validateMinMax: function (items, hasItems) {
      if (hasItems) {
        this.model.set("items", items);
        this.model.set("minimum", 0);
        this.model.set("maximum", items.length - 1);
        this.model.set("step", 1);
      }
      else {
        if (
          (isNaN(this.model.get("minimum")) || isNaN(this.model.get("maximum")))
          ||
          (this.model.get("minimum") === 0 && this.model.get("maximum") === 0)
          ) {
          this.model.set("minimum", 0);
          this.model.set("maximum", 10);
        }
      }
    },

    /// <summary>
    /// Toggle SLider control Enabled property.
    /// </summary>
    toggleSliderControlEnabled: function () {
      this.sliderControl.disabled = !this.model.get("isEnabled");
    },

    /// <summary>
    /// Toggle component Enabled property.
    /// </summary>
    toggleEnable: function () {
      if (!this.model.get("isEnabled")) {
        this.$el.addClass("disabled");
      } else {
        this.$el.removeClass("disabled");
      }

      this.sliderControl.disabled = !this.model.get("isEnabled");
    },

    /// <summary>
    /// CalculateSliderValue.
    /// </summary>
    /// <param name="items">The items array.</param>
    /// <param name="minimum">The minimum index.</param>
    /// <param name="maximum">The maximum index.</param>
    /// <param name="selectedValue">The selected value (single).</param>
    /// <param name="selectedValueStart">The selected value start (range).</param>
    /// <param name="selectedValueEnd">The selected value end (range).</param>
    /// <param name="type">The slider type (Single|Range)</param>
    /// <returns>The slider value.</returns>
    calculateSliderValue: function (items, minimum, maximum, selectedValue, selectedValueStart, selectedValueEnd, type) {

      selectedValue = this.getValueIndex(items, selectedValue, maximum);
      selectedValueStart = this.getValueIndex(items, selectedValueStart, minimum);
      selectedValueEnd = this.getValueIndex(items, selectedValueEnd, maximum);

      if (type === "Range") {
        if (selectedValueStart < minimum || selectedValueStart > maximum) {
          selectedValueStart = minimum;
        }

        if (selectedValueEnd < minimum || selectedValueEnd > maximum) {
          selectedValueEnd = maximum;
        }

        return selectedValueStart + ";" + selectedValueEnd;
      }

      if (selectedValue < minimum || selectedValue > maximum) {
        selectedValue = maximum;
      }

      return selectedValue;
    },
    /// <summary>
    /// Get slider index from item's value.
    /// </summary>
    /// <param name="items">The items array.</param>
    /// <param name="value">The item's value.</param>
    /// <returns>The value index.</returns>
    getValueIndex: function (items, value, defaultValue) {

      if (items && items.length > 0) {
        for (var i = 0; i < items.length; i++) {
          if (items[i].value === value.toString()) {
            return i;
          }
        }

        return defaultValue;
      }

      if (isNaN(value) || value === "") {
        return defaultValue;
      }

      return value;
    }
  });
});