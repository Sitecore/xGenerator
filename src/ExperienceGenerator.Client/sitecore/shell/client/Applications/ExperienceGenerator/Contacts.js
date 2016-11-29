/*/ THIS CODE IS FOR PROTOTYPE ONLY!!!! /*/
define(["sitecore", "knockout", "underscore", "/-/speak/v1/experienceGenerator/InteractionEditor.js"],
    function(_sc, ko, _, interactionEditor) {
        var DataSheet = _sc.Definitions.App.extend({
            interactionEditor: interactionEditor,
            addContact: function() {
                this.ContactList.unset("selectedItemId");
                var contacts = this.ContactList.get("items");
                var newContact = {
                    "itemId": this.guid(),
                    "image": "",
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

            addItemToList: function(sourceControl, targetControl) {
                var selected = this[sourceControl].get("selectedItem");
                if (!selected) return;

                var existingOutcomes = this[targetControl].get("items") || [];
                existingOutcomes.push(_.clone(selected));
                this[targetControl].unset("items", { silent: true });
                this[targetControl].set("items", existingOutcomes);
            },

            addInteraction: function() {
                var contact = this.ContactList.get("selectedItem");
                if (!contact) return;

                var newInteraction = {
                    pages: [],
                    recency: 0,
                    geoData: { Country: {} }
                };

                interactionEditor.editInteraction(newInteraction);
            },
            deleteSelected: function(controlName) {
                var control = this[controlName];
                var filteredItem = control.get("items");

                var checkedItems = control.get("checkedItems");

                filteredItem = _.difference(filteredItem, checkedItems);

                control.unset("items", { silent: true });
                control.set("items", filteredItem);
                if (controlName === "InteractionList") {
                    this.ContactList.get("selectedItem").set("interactions", filteredItem);

                }

            },
            duplicateSelected: function(controlName) {
                var control = this[controlName];
                var filteredItem = control.get("items");

                var checkedItems = control.get("checkedItems");

                _.each(checkedItems,
                    function(item) {
                        var newItem = _.clone(item);
                        newItem["itemId"] = this.guid();
                        filteredItem.push(newItem);
                    });

                control.unset("items", { silent: true });
                control.set("items", filteredItem);
                if (controlName === "InteractionList") {
                    var interactions = this.ContactList.get("selectedItem").get("interactions");

                    //hack to clear array
                    while (interactions.length > 0) {
                        interactions.pop();
                    }
                    _.each(filteredItem, function(item) { interactions.push(item); });

                    this.setInteractions(interactions);
                }
                control.viewModel.uncheckItems(control.get("checkedItems"));
            },
            selectLastElement: function(control) {
                control.viewModel.$el.find("tr").eq(-2).find("td:last").click();
            },

            initialized: function() {
                interactionEditor.initialize(this);

                this.landingPages = "";
                this.campaigns = "";
                this.data = {};
                this.jobId = undefined;
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


                var $file = $("<input id='contactImageFile' type='file'/>");
                var that = this;
                $("[data-sc-id=ImageContactPanel]").prepend($file);
                $file.on("change",
                    function() {
                        var fileReader = new FileReader();
                        fileReader.onload = function(e) {

                            var canvas = $("<canvas style='display:none'/>")[0];
                            var ctx = canvas.getContext("2d");

                            // limit the image to 300x300 maximum size
                            var maxW = 300;
                            var maxH = 300;


                            var img = new Image;
                            img.onload = function() {
                                var iw = img.width;
                                var ih = img.height;
                                var scale = Math.min((maxW / iw), (maxH / ih));
                                var iwScaled = iw * scale;
                                var ihScaled = ih * scale;
                                canvas.width = iwScaled;
                                canvas.height = ihScaled;
                                ctx.drawImage(img, 0, 0, iwScaled, ihScaled);
                                that.ContactImage.set("imageUrl", canvas.toDataURL());
                            };
                            img.src = e.target.result;


                        };
                        if (this.files.length > 0)
                            fileReader.readAsDataURL(this.files[0]);
                    });

                //this.initPresetDataSource();

                this.ContactList.on("change:selectedItem", this.loadSelectedContact, this);
                this.InteractionList.on("change:selectedItem", this.openEditInteractionModal, this);
                this.PrimeEmailValue.on("change:text",
                    function(model, text) {
                        var $parent = model.viewModel.$el.parent();

                        if (!text && !$parent.hasClass("has-error") || (text && $parent.hasClass("has-error"))) {
                            $parent.toggleClass("has-error");
                        }

                    },
                    this);

                this.ContactImage.on("change:src",
                    function(model, value) {
                        this.ContactList.get("selectedItem").set("image", value);
                        for (var idx in this.ContactList.attributes.items) {
                            var contact = this.ContactList.attributes.items[idx];
                            if (contact.itemId == this.ContactList.attributes.selectedItemId) {
                                contact["image"] = value;
                            }
                        }

                    },
                    this);
                this.BirthdayValue.on("change:date",
                    function(model, value) {
                        model.unset("text");
                        model.set("text", value);
                    },
                    this);

                this.applyTwoWayBindings();
            },
            applyTwoWayBindings: function() {
                for (var key in this.bindingMap) {
                    if (this.bindingMap.hasOwnProperty(key)) {
                        this[key].on("change:text", this.updateSelectedContact, this);
                    }
                }
            },
            interactionsOKButton: function() {
                var itr = interactionEditor.retrieveInteraction();
                var interactions = this.ContactList.get("selectedItem").get("interactions");
                if (!itr.itemId) {
                    itr.itemId = this.guid();
                    interactions.push(itr);
                } else {
                    for (var idx in interactions) {
                        if (interactions.hasOwnProperty(idx)) {
                            if (interactions[idx].itemId == itr.itemId) {
                                interactions[idx] = itr;
                            }
                        }
                    }
                }

                this.setInteractions(interactions);
            },
            openEditInteractionModal: function(control, selectedItem) {
                if (!selectedItem) return;
                interactionEditor.editInteraction(selectedItem.attributes);
            },
            updateSelectedContact: function(model) {
                var key = model.get("name");
                for (var idx in this.ContactList.attributes.items) {
                    var contact = this.ContactList.attributes.items[idx];
                    if (contact.itemId == this.ContactList.attributes.selectedItemId) {
                        contact[this.bindingMap[key]] = this[key].get("text");
                    }
                }
                this.ContactList.get("selectedItem").set(this.bindingMap[key], this[key].get("text"));
            },

            loadSelectedContact: function(control, selectedItem) {
                if (!selectedItem) {
                    return;
                }

                for (var key in this.bindingMap) {
                    if (this.bindingMap.hasOwnProperty(key)) {
                        this[key].set("text", selectedItem.get(this.bindingMap[key]));
                    }
                }
                this["BirthdayValue"].set("date", selectedItem.get(this.bindingMap["BirthdayValue"]));
                this.setInteractions(selectedItem.get("interactions"));
                this.ContactImage.set("imageUrl", selectedItem.get("image"));
            },
            setInteractions: function(interactions) {
                interactions = interactions || this.InteractionList.get("items");

                var sorted = _.sortBy(interactions, function(x) { return +x["recency"]; });
                sorted.reverse();

                this.InteractionList.unset("items", { silent: true });
                this.InteractionList.set("items", sorted);
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
            },

            guid: function() {
                return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g,
                    function(c) {
                        var r = Math.random() * 16 | 0, v = c == "x" ? r : (r & 0x3 | 0x8);
                        return v.toString(16);
                    });
            },


            dialogCancelButton: function(DialogWindow) {
                this[DialogWindow].hide();
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
                _sc.off("intervalCompleted:ProgressBar");
                $.ajax({
                        url: "/api/xgen/jobs/" + this.jobId,
                        type: "DELETE"
                    })
                    .done(function(data) {
                        that.stopped(data);
                    });
            },

            start: function() {
                if (this.jobId && !confirm("A job is already running. Start anyway?")) {
                    return;
                }

                this.data = this.adapt(ko.toJS(this.ContactList.get("items")));
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

            running: function(data) {
                this.jobId = data.Id;
                _sc.on("intervalCompleted:ProgressBar", this.updateProgress, this);
            },

            stopped: function(data) {
                this.jobId = undefined;
                _sc.off("intervalCompleted:ProgressBar");
            },

            updateProgress: function() {
                var jobId = this.jobId;
                var that = this;
                $.ajax({
                        url: "/api/xgen/jobs/" + that.jobId,
                        type: "GET",
                    })
                    .done(function(data) {
                        var total = 0;
                        var contacts = data.Specification.Specification.Contacts;
                        for (var i = 0; i < contacts.length; i++) {
                            total += contacts[i].interactions.length;
                        }

                        that.ProgressBar.set("value", data.CompletedVisitors / total * 100);
                        that.NumberVisitsValue.set("text", data.CompletedVisits);
                        if (data
                            .JobStatus !==
                            "Running" &&
                            data.JobStatus !== "Pending" &&
                            data.JobStatus !== "Paused") {
                            that.stopped(data);
                        }
                    });
            },
            loadPreset: function() {
                var self = this;
                var selectedItem = this.PresetList.attributes.selectedItemId;
                var url = "/api/xgen/contactsettingspreset?id=" + selectedItem;
                $.ajax({
                        url: url,
                        type: "GET",
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        success: function() {}
                    })
                    .done(function(data) {
                        _.each(data,
                            function(item) {
                                item["itemId"] = self.guid();
                                item["image"] = item["image"] || "";
                            });
                        self.ContactList.set("selectedItem", null);
                        self.ContactList.set("items", data);
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

                    this.data = { spec: ko.toJS(this.ContactList.get("items")), name: name, force: true };
                    console.log(this.data);

                    this.data = JSON.stringify(this.data);
                    $.ajax({
                            url: "/api/xgen/SaveContactsSettings",
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
            adapt: function(doc) {

                return {
                    Type: 1,
                    VisitorCount: doc.length,
                    Specification: {
                        Contacts: doc,
                        Segments: {
                        
                        }
                    }
                };
            }
        });
        return DataSheet;
    });
