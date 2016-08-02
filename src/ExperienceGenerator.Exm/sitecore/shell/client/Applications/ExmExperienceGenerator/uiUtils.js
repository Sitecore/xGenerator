define(["sitecore", "jquery", "underscore"], function (sc, $, _) {
  var templates = {
    sliderTemplate: "<div class='large-slider-unit'> \
  <div class='channel-sectionlabel truncate'><%= sectionLabel %></div>\
  <div class='channel-innerlabel truncate'><%= innerLabel %></div>\
  <div class='channel-slider'><input class='form-control sc-textbox sc-ds-slider' data-sc-id='<%= sliderId %>' type='range' value='<%= sliderWeight %>'/></div>\
  </div>",
    deletableSliderTemplate: "<div class='landingpage'>\
  <div class='channel-innerlabel truncate'><%= sectionLabel %></div>\
  <div class='landingpage-channel-slider'><input class='form-control sc-textbox sc-ds-slider' data-sc-id='<%= sliderId %>' value='<%= sliderWeight %>' type='range'/></div>\
  <div class='end-button-area'><button class='btn btn-default end-button' onclick='$(this).parents(&quot;.landingpage&quot;).remove()'><span class='sc-button-text'>Delete</span></button></div>\
  </div>"
  };

  var uiUtils = {
    renderSlider: function (label, innerLabel, sliderId, sliderWeight) {
      var options = { sectionLabel: label, innerLabel: innerLabel, sliderId: sliderId, sliderWeight: sliderWeight };
      return _.template(templates.sliderTemplate, options);
    },
    renderSliderWithDelete: function (label, sliderId, sliderWeight) {
      var options = { sectionLabel: label, sliderId: sliderId, sliderWeight: sliderWeight };
      return _.template(templates.deletableSliderTemplate, options);
    },

    renderSliders: function (group) {
      var prevSectionLabel, sectionLabel;
      var output = "";
      for (var groupKey in group) {
        if (group.hasOwnProperty(groupKey)) {
          var options = group[groupKey].Options;
          for (var optionIdx in options) {
            if (options.hasOwnProperty(optionIdx)) {
              var innerLabel = options[optionIdx].Label;
              if (prevSectionLabel == group[groupKey].Label) {
                sectionLabel = "";
              } else {
                sectionLabel = prevSectionLabel = group[groupKey].Label;
              }
              output += this.renderSlider(sectionLabel, innerLabel, options[optionIdx].Id, options[optionIdx].DefaultWeight);
            }
          }
        }
      }
      return output;
    },

    parseSlidersContainer: function (slidersContainer) {
      var slidersData = [];
      $(slidersContainer).find("input[type='range']").each(function(idx, slider) {
        slidersData[slider.getAttribute("data-sc-id")] = slider.value;
      });

      return slidersData;
    },

    resetSliders: function(slidersContainer, host) {
      $(slidersContainer).find("input[type='range']").each(function (idx, slider) {

        var objectId = slider.getAttribute("data-sc-id");
        if (host && host[objectId])
          host[objectId].set("text", undefined);
        else 
          slider.value = undefined;
      });
    },

    setSliderValues: function (sliderValues) {
      for (var id in sliderValues) {
        $("input[type='range'][data-sc-id='" + id + "']")[0].value = sliderValues[id];
      }
    }

  };

  return uiUtils;
});