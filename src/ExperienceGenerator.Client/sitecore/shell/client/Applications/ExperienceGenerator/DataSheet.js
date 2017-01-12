/*/ THIS CODE IS FOR PROTOTYPE ONLY!!!! /*/
define(["sitecore", "underscore"],
    function(_sc, _) {
        var DataSheet = _sc.Definitions.App.extend({
            initialized: function() {
                this.landingPages = "";
                this.campaigns = "";
                this.devices = "";
                this.data = {};
                this.jobId = undefined;
                this.paused = false;
                var now = new Date();
                this.StartDate.viewModel.setDate(new Date(now.getFullYear() - 1, now.getMonth(), now.getDate()));
                this.EndDate.viewModel.setDate(now);
                this.loadOptions();
                //this.initPresetDataSource();
            },

            initPresetDataSource: function() {
                var url = "/api/xgen/presetquery";
                var self = this;
                $.ajax({
                        url: url,
                        type: "GET",
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                    })
                    .done(function(data) {
                        self.DataSource.set("query", data.query);
                        self.DataSource.refresh();
                    });
            },

            loadOptions: function() {
                var that = this;
                $.ajax({
                        url: "/api/xgen/options"
                    })
                    .done(function(data) {
                        that.populate(data);
                    });

                $.ajax({
                        url: "/api/xgen/devices"
                    })
                    .done(function(data) {

                        var root = _.groupBy(data, "Type");
                        var arr = [];
                        var mapped = Object.keys(root)
                            .map(function(key) {
                                arr.push({ title: key, isFolder: true, children: root[key], itemUri: { itemId: "" } });
                            });
                        _.each(arr,
                            function(group) {
                                _.each(group.children,
                                    function(device) {
                                        device.title = device.Name;
                                        device.key = device.Id;
                                        device.itemUri = { itemId: device.Id };

                                    });
                            });
                        var r = that.DeviceTreeView.viewModel.getRoot();
                        r.removeChildren();
                        r.addChild(arr);
                    });
            },

            guid: function() {
                return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g,
                    function(c) {
                        var r = Math.random() * 16 | 0, v = c == "x" ? r : (r & 0x3 | 0x8);
                        return v.toString(16);
                    });
            },

            dialogOKButton: function(TreeView, DialogWindow, panel, pages) {
                this[pages] = this[TreeView].viewModel.checkedItemIds();
                var nodes = this[TreeView].viewModel.getRoot().tree;
                var that = this;
                var $target = $("div[data-sc-id='" + panel + "']");
                var weights = [];
                $target.find("input.sc-ds-slider")
                    .map(function(idx, el) { weights[el.getAttribute("data-sc-id")] = el.value });

                $target.empty();
                nodes.visit(function(childNode) {
                    var id = childNode.data.key;
                    if (that[pages].indexOf(id) > -1) {
                        var output = "";
                        output += "<div class='landingpage'>";
                        output += "<div class='channel-innerlabel truncate'>" + childNode.data.title + "</div>";
                        output +=
                            "<div class='landingpage-channel-slider'><input class='form-control sc-textbox sc-ds-slider' data-sc-id='" + id + "' value='" + weights[id] + "' type='range'/></div>";
                        output +=
                            "<div class='end-button-area'><button class='btn btn-default end-button'><span class='sc-button-text'>Delete</span></button></div>";
                        output += "</div>";
                        var $output = $(output);
                        $target.append($output);
                        $output.find("button")
                            .click(function(e) {
                                that.removeSelectedNode($output, pages);
                            });
                    }
                });
                this[DialogWindow].hide();
            },

            removeSelectedNode: function(node, pages) {
                node.remove();
                if (pages) { // only for landingPages and campaigns
                    this[pages] = this[pages].replace(node.find("[data-sc-id]").attr("data-sc-id"), "");
                    this[pages] = this[pages].replace("|{}|", "|");
                    this[pages] = this[pages].replace("||", "|");
                }
            },

            dialogCancelButton: function(DialogWindow) {
                this[DialogWindow].hide();
            },

            dialogShow: function(TreeView, DialogWindow, pages) {
                //Go through all the nodes in the TreeView, and make them correspond to what we've got in this[pages]
                var nodes = this[TreeView].viewModel.getRoot().tree;
                var that = this;
                nodes.visit(function(childNode) {
                    var id = childNode.data.key;
                    childNode.select(that[pages].indexOf(id) > -1);
                });
                this[DialogWindow].show();
            },

            addRow: function(textBoxId, targetBorderId) {
                var guid = this.guid();
                var text = this[textBoxId].get("text");
                this[textBoxId].set("text", "");
                this.appendRowToDOM(targetBorderId, text, guid, encodeURIComponent(text));
            },

            appendRowToDOM: function (targetBorderId, text, guid, datascid, action, weight) {
                if(action = null || action == "none" || action == "")
                action = "Delete";
                var output = "";
                var that = this;
                var $target = $("div[data-sc-id='" + targetBorderId + "']");
                output += "<div class='landing page'>";
                output += "<div class='channel-innerlabel truncate'>" +
                    text +
                    "</div><div class='landingpage-channel-slider'>";
                //if (action == "Delete") {
                output += "<input class='form-control sc-textbox sc-ds-slider' data-sc-id='" +
                    datascid +
                    "' type='range' value='" +
                    weight +
                    "'/>";
                //} 
                output += "</div>";
                output += "<div class='end-button-area'>";
                if (action != "none") {
                    output += "<button class='" +
                        guid +
                        " btn btn-default end-button " +
                        "" +
                        "'><span class='sc-button-text'>" +
                        action +
                        "</span></button>";
                }
                output += "</div>";
                output += "</div>";
                var $output = $(output);
                $target.append($output);
                $output.find("button")
                    .click(function(e) {
                        if (action == "Delete") {
                            that.removeSelectedNode($output);
                        }
                        if (action == "Action") {
                            that.action($output.attr("data-sc-id"), text);
                        }
                    });
            },

            action: function(guid) {
                console.log("Do action... on " + guid);
                this.SingleCampaignWindow.show();
                this.SingleCampaignWindow.set("", text);
            },

            createRows: function(targetDivId, p, action) {
                for (var key in p) {
                    if (p.hasOwnProperty(key)) {
                        this.appendRowToDOM(targetDivId,
                            p[key].Label,
                            p[key].Id.slice(1, -1),
                            p[key].Id,
                            action,
                            p[key].DefaultWeight);
                        this.campaigns += p[key].Id + "|";
                    }
                }
            },

            createSliders: function(targetDivId, p) {
                var target = $("div[data-sc-id='" + targetDivId + "']");
                var output = "";
                for (var key in p) {
                    if (p.hasOwnProperty(key)) {
                        output +=
                            "<div class='small-slider-unit'><div class='small-slider-unit-label'><span class='sc-text truncate' title='" +
                            p[key].Label +
                            "'>" +
                            p[key].Label +
                            "</span></div><div class='small-slider-unit-slider'><input class='form-control sc-textbox sc-ds-slider' data-sc-id='" +
                            p[key].Id +
                            "' type='range' value='" +
                            (p[key].DefaultWeight) +
                            "'/></div></div>";
                    }
                }
                target.append(output);
            },

            fillListTab: function(tab) {

                output = "";

                var types = this.uiSetup[tab + "Types"];
                if (types != null) {
                    for (var typeIdx in types) {
                        var type = types[typeIdx];
                        output = this.extractGroups(type["OptionGroups"]);
                        $("div[data-sc-id='" + type["Type"] + tab + "Panel']").append(output);
                    }
                    return;
                }

                var p = this.uiSetup[tab + "Groups"];
                output = this.extractGroups(p);
                $("div[data-sc-id='" + tab + "Panel']").append(output);
            },

            extractGroups: function(p) {
                var prevSectionLabel, sectionLabel;
                var output = "";
                for (var key in p) {
                    if (p.hasOwnProperty(key)) {
                        var c = p[key].Options;
                        for (var key2 in c) {
                            if (c.hasOwnProperty(key2)) {
                                var innerLabel = c[key2].Label;
                                if (prevSectionLabel == p[key].Label) {
                                    sectionLabel = "";
                                } else {
                                    sectionLabel = prevSectionLabel = p[key].Label;
                                }
                                output += "<div class='large-slider-unit'>";
                                output += "<div class='channel-sectionlabel truncate'>" + sectionLabel + "</div>";
                                output += "<div class='channel-innerlabel truncate'>" + innerLabel + "</div>";
                                output +=
                                    "<div class='channel-slider'><input class='form-control sc-textbox sc-ds-slider' data-sc-id='" + c[key2].Id + "' type='range' value='" + c[key2].DefaultWeight + "'/></div>";
                                output += "</div>";
                            }
                        }
                    }
                }
                return output;
            },

            populate: function(data) {
                this.uiSetup = data;


                var target,
                    output;
                var days = [
                    { Id: "Monday", Label: "Monday" }, { Id: "Tuesday", Label: "Tuesday" },
                    { Id: "Wednesday", Label: "Wednesday" }, { Id: "Thursday", Label: "Thursday" },
                    { Id: "Friday", Label: "Friday" }, { Id: "Saturday", Label: "Saturday" },
                    { Id: "Sunday", Label: "Sunday" }
                ];
                var months = [
                    { Id: "January", Label: "January" }, { Id: "February", Label: "February" },
                    { Id: "March", Label: "March" },
                    { Id: "April", Label: "April" }, { Id: "May", Label: "May" }, { Id: "June", Label: "June" },
                    { Id: "July", Label: "July" }, { Id: "August", Label: "August" },
                    { Id: "September", Label: "September" },
                    { Id: "October", Label: "October" }, { Id: "November", Label: "November" },
                    { Id: "December", Label: "December" }
                ];

                this.createSliders("TrafficDistributionBorder", this.uiSetup.Websites);
                this.createSliders("LanguageDistributionBorder", this.uiSetup.Languages);
                this.createSliders("DailyDistributionBorder", days);
                this.createSliders("MonthlyDistributionBorder", months);
                this.fillListTab("Location");
                this.fillListTab("Channel");
                this.fillListTab("Outcome");
                this.createSliders("OrganicBorder", this.uiSetup.OrganicSearch);
                this.createSliders("PPCBorder", this.uiSetup.PpcSearch);
                this.createRows("CampaignsListInnerPanelTopBorder", this.uiSetup.Campaigns, "none");

                this.CdBorder.set("isVisible", !data.TrackerIsEnabled);
                this.ContentDeliveryValue.set("text", window.location.origin);
            },

            deleteData: function() {
                this.DelWindow.hide();
                $.ajax({
                    url: "/api/xgen/flush",
                    type: "POST"
                });
            },

            pause: function() {
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
            stop: function() {
                if (!this.jobId) {
                    //No job has ever been started
                    return;
                }
                var that = this;
                $.ajax({
                        url: "/api/xgen/jobs/" + this.jobId,
                        type: "DELETE",
                    })
                    .done(function(data) {
                        that.stopped(data);
                    });
            },

            start: function () {
                if (this.jobId && !confirm("A job is already running. Start anyway?")) {
                    return;
                }

                this.collectSettings();
                console.log(this.data);
                this.data = this.adapt(this.data); //Adapter Hell alert
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
                        success: function() {}
                    })
                    .done(function(data) {
                        that.running(data);
                    });
            },

            running: function (data) {
                this.SummaryTitle.set("text", "Running...");
                this.StartButton.attributes.isEnabled = false;
                this.jobId = data.Id;
                _sc.on("intervalCompleted:ProgressBar", this.updateProgress, this);
            },

            stopped: function(data) {
                this.SummaryTitle.set("text", data.JobStatus);
                this.StartButton.attributes.isEnabled = true;
                this.jobId = undefined;
                _sc.off("intervalCompleted:ProgressBar");
            },

            updateProgress: function() {
                var jobId = this.jobId;
                var that = this;
                $.ajax({
                        url: "/api/xgen/jobs/" + that.jobId,
                        type: "GET"
                    })
                    .done(function(data) {
                        that.ProgressBar.set("value", data.Progress * 100);
                        that.NumberVisitsValue.set("text", data.CompletedVisits);
                        that.NumberContactsValue.set("text", data.CompletedVisitors);
                        that.SummaryTitle.set("text", data.JobStatus);
                        if (data.JobStatus !== "Running" && data.JobStatus !== "Pending" && data.JobStatus !== "Paused") {
                            that.stopped(data);
                        }
                    });
            },

            readDomValues: function(tab, section) {
                var data = this.data;
                if (!data[tab]) {
                    data[tab] = {};
                }
                var obj = data[tab];
                var targ = obj;
                var borderName = tab;
                if (section) {
                    borderName = section + "Border";
                    obj[section] = {};
                    targ = obj[section];
                }
                var $where = $("div[data-sc-id='" + borderName + "']");
                var $comp = $where.find(".sc-textbox");
                for (i = 0; i < $comp.length; i++) {
                    id = $comp.get(i).getAttribute("data-sc-id");
                    targ[id] = $comp.get(i).value;
                }
                if (section != "Dates") { //Cannot get dates in the same way as Sliders and TextBoxes
                    return;
                }
                $comp = $where.find(".sc-datepicker");
                for (i = 0; i < $comp.length; i++) {
                    id = $comp.get(i).getAttribute("data-sc-id");
                    obj[section][id] = this[id].get("formattedDate");
                }
            },

            emptyDomValues: function() {
                this.emptyDomValue("LandingPages");
                this.emptyDomValue("Search");
                this.emptyDomValue("Devices");
                this.emptyDomValue("RefURLsTab");
            },

            emptyDomValue: function(area) {
                var elements = $("div[data-sc-id='" + area + "'] .landingpage");
                elements.remove();
            },

            writeDomValues: function(data) {
                for (var i in data) {
                    var tab = data[i];
                    var borderName = i;
                    for (var j in tab) {
                        var section = tab[j];

                        if (typeof section === "object") {
                            borderName = j + "Border";
                            var $where = $("div[data-sc-id='" + borderName + "']");
                            var $comp = $where.find(".sc-textbox");
                            for (var ii in section) {
                                id = ii;
                                var row = $comp.filter("[data-sc-id='" + id + "']");
                                if (row == undefined || row.length == 0) {
                                    this.appendRow($where, id, section[id]);
                                } else {
                                    row[0].value = section[id];
                                }
                            }
                        } else {
                            var $where = $("div[data-sc-id='" + borderName + "']");
                            var $comp = $where.find("input[data-sc-id='" + j + "']");
                            if ($comp[0] == undefined) {
                                this.appendRow($where, j, section);
                            } else {
                                $comp[0].value = section;
                            }
                        }
                    }
                }
            },

            appendRow: function($tab, id, value) {
                var container = {};
                if ($tab.hasClass("row")) {
                    container = $tab.children().first()[0];
                } else {
                    container = $tab.find("div.row").first().children().first()[0];
                }


                var isId = _sc.Helpers.id.isId(id);
                if (isId) {
                    var db = new _sc.Definitions.Data.Database(new _sc.Definitions.Data
                        .DatabaseUri(this.LandingPageTreeView.get("database") || "master"));
                    db.getItem(id,
                        function(item) {
                            $("[data-sc-id='" + id + "']")
                                .parents(".landingpage")
                                .find(".channel-innerlabel")
                                .text(item.$displayName);
                        });
                }

                this
                    .appendRowToDOM($(container).attr("data-sc-id"),
                        decodeURIComponent(id),
                        this
                        .guid(),
                        id,
                        "none",
                        value);
            },

            loadPreset: function() {
                var self = this;
                var selectedItem = this.PresetList.attributes.selectedItemId;
                var url = "/api/xgen/settingspreset?id=" + selectedItem;
                $.ajax({
                        url: url,
                        type: "GET",
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        success: function() {}
                    })
                    .done(function(data) {
                        self.emptyDomValues();
                        self.writeDomValues(data);
                        alert("Presets loaded");
                    });
            },

            save: function(name) {
                var self = this;
                var name = this.PresetName.attributes.text;
                if (name == "") {
                    alert("Please enter preset name.");
                } else {
                    if (_.any(this.DataSource.get("items"), function(item) { return item.itemName === name })) {
                        var overwrite = confirm("Are you sure you want to overwrite settings?");
                        if (!overwrite) return;
                    }
                    this.collectSettings();
                    console.log(this.data);
                    //this.data = this.adapt(this.data); //Adapter Hell alert
                    this.data = { spec: this.data, name: name };
                    //console.log(JSON.stringify(this.data, null, 2));
                    this.data = JSON.stringify(this.data);
                    var that = this;
                    $.ajax({
                            url: "/api/xgen/savesettings",
                            type: "POST",
                            data: this.data,
                            dataType: "json",
                            contentType: "application/json; charset=utf-8",
                            success: function() {}
                        })
                        .done(function(data) {
                            self.PresetName.set("text", "");
                            self.DataSource.refresh();
                        })
                        .fail(function(data) {
                            alert(data.responseJSON.ExceptionMessage);
                        });
                }
            },

            collectSettings: function() {
                this.data = {};

                if (this.CdBorder.get("isVisible") && this.ContentDeliveryValue.get("text"))
                    this.data.RootUrl = this.ContentDeliveryValue.get("text");

                this.readDomValues("Overview", "Visits");
                this.readDomValues("Overview", "TrafficDistribution");
                this.readDomValues("Overview", "LanguageDistribution");
                this.readDomValues("Locations");
                this.readDomValues("Overview", "DailyDistribution");
                this.readDomValues("Overview", "MonthlyDistribution");
                this.readDomValues("Overview", "Dates");
                this.readDomValues("Channels");
                this.readDomValues("LandingPages");
                this.readDomValues("Devices");
                this.readDomValues("RefURLs");
                this.readDomValues("Search", "InternalSearchTerms");
                this.readDomValues("Search", "ExternalSearchTerms");
                this.readDomValues("Search", "PercentageTrafficFromSearch");
                this.readDomValues("Search", "Organic");
                this.readDomValues("Search", "PPC");
                this.readDomValues("Campaigns");
                this.readDomValues("Outcomes");
            },


            ///////////////////////////////////////////////////////////////Niels' adaptor start ///////////////////
            adapt: function(doc) {
                var request = {
                    VisitorCount: 1 * doc.Overview.Visits.NumberOfUniqueVisitors
                };

                var defaultSegment = {
                    Identified: 1 * doc.Overview.Visits.PercentageIdentifiedVisitors / 100,
                    BounceRate: 1 * doc.Overview.Visits.BounceRate / 100,
                    VisitCount: 1 *
                        doc.Overview.Visits.NumberOfVisitsGenerated /
                        doc.Overview.Visits
                        .NumberOfUniqueVisitors,
                    PageViews: 1 * doc.Overview.Visits.PageviewsPerVisitAvg,
                    Duration: this.durationToSeconds(doc.Overview.Visits.TimeSpentPerPageAvg),
                    StartDate: this.StartDate.viewModel.getDate().toISOString(),
                    EndDate: this.EndDate.viewModel.getDate().toISOString(),
                    YearlyTrend: 1 + (4 * (1 * doc.Overview.Dates.AnnualIncreaseInTraffic - 50) / 100),
                    DayOfWeek: this.toWeights(doc.Overview.DailyDistribution),
                    Month: this.toWeights(doc.Overview.MonthlyDistribution, function(value, i) { return i + 1; }),
                    LandingPage: {
                        Site: this.toWeights(doc.Overview.TrafficDistribution),
                        Item: this.toWeights(doc.LandingPages)
                    },

                    Referrer: this.toWeights(doc.RefURLs),
                    InternalSearch: {
                        Percentage: doc.Search.PercentageTrafficFromSearch.Percentage / 100,
                        Keywords: this.toWeights(doc.Search.InternalSearchTerms)
                    },
                    ExternalSearch: {
                        Percentage: doc.Search.PercentageTrafficFromSearch.Percentage / 100,
                        Keywords: this.toWeights(doc.Search.ExternalSearchTerms),
                        Engine: this.joinDicts(this.toWeights(doc.Search.Organic), this.toWeights(doc.Search.PPC))
                    },
                    Devices: this.toWeights(doc.Devices),
                    Geo: {
                        Region: this.toWeights(doc.Locations)
                    },
                    Outcomes: this.toWeights(doc.Outcomes),

                    Channel: {
                        Percentage: 1 * doc.Channels.Percentage / 100,
                        Weights: this.toWeights(doc.Channels)
                    },

                    Campaign: {
                        Percentage: 1 * doc.Campaigns.Percentage / 100,
                        Weights: this.toWeights(doc.Campaigns)
                    },

                    Language: this.toWeights(doc.Overview.LanguageDistribution)
                };
                delete (defaultSegment.Channel.Weights.Percentage);
                delete (defaultSegment.Campaign.Weights.Percentage);

                request.Specification = {
                    Segments: {
                        Default: defaultSegment
                    }
                };
                request.RootUrl = doc.RootUrl;
                return request;
            },

            joinDicts: function(d1, d2) {
                for (var key in d2) {
                    d1[key] = d2[key];
                }
                return d1;
            },

            toWeights: function(o, keyTranslator) {
                var weights = {};
                var i = 0;
                keyTranslator = keyTranslator || function(value, i) { return value; };
                for (var key in o) {
                    weights[keyTranslator(key, i++)] = (1 * o[key] || 0) / 100;
                }
                return weights;
            },

            durationToSeconds: function(s) {
                var parts = s.split(":");
                var i = 0;
                var duration = 0;
                if (parts.length > 2) duration += parts[i++] * 3600;
                if (parts.length > 1) duration += parts[i++] * 60;
                return duration + 1 * parts[i];
            }
            ///////////////////////////////////////////////////////Niel's adaptor end ///////////////////


        });
        return DataSheet;
    });
