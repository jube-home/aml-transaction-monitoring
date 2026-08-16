/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License 
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty  
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not, 
 * see <https://www.gnu.org/licenses/>.
 */

(function ($) {
    var kendo = window.kendo,
        ui = kendo.ui,
        Widget = ui.Widget;

    var RoleManager = Widget.extend({
        init: function (element, options) {
            var that = this;
            Widget.fn.init.call(that, element, options);

            that._pendingChanges = {
                toAssign: [],
                toUnassign: []
            };

            that._create();
            that._fetchRoles();
        },

        options: {
            name: "RoleManager",
            baseEndpoint: "/api/CaseWorkflowStatusRole",
            parentKeyField: "caseWorkflowStatusGuid",
            guid: null,
            width: 700,
            height: 350,
            availableTitle: "Available Roles",
            assignedTitle: "Assigned Roles",
            showSaveButton: true,
            showCancelButton: true
        },

        _create: function () {
            var that = this,
                options = that.options;

            that.element.addClass("k-role-manager");

            var html = `
                <div class="k-role-manager-wrapper">
                    <div class="k-role-manager-container" style="width: ${options.width}px;">
                        <div class="k-role-manager-panel">
                            <div class="k-role-manager-header">${options.availableTitle}</div>
                            <select id="availableRoles" multiple="multiple" style="height: ${options.height}px;"></select>
                        </div>
                        <div class="k-role-manager-buttons">
                            <button type="button" class="k-button k-button-md k-rounded-md k-button-solid k-button-solid-primary assign-btn" title="Assign selected roles">
                                <span class="k-icon k-i-arrow-chevron-right"></span>
                            </button>
                            <button type="button" class="k-button k-button-md k-rounded-md k-button-solid k-button-solid-primary unassign-btn" title="Unassign selected roles">
                                <span class="k-icon k-i-arrow-chevron-left"></span>
                            </button>
                        </div>
                        <div class="k-role-manager-panel">
                            <div class="k-role-manager-header">${options.assignedTitle}</div>
                            <select id="assignedRoles" multiple="multiple" style="height: ${options.height}px;"></select>
                        </div>
                    </div>
            `;

            if (options.showSaveButton || options.showCancelButton) {
                html += '<div class="k-role-manager-actions">';
                if (options.showSaveButton) {
                    html += '<button type="button" class="k-button save-btn">Update</button>';
                }
                if (options.showCancelButton) {
                    html += '<button type="button" class="k-button cancel-btn">Cancel</button>';
                }
                html += '</div>';
            }

            html += '</div>';

            that.element.html(html);

            that.availableListBox = that.element.find("#availableRoles").kendoListBox({
                dataTextField: "name",
                dataValueField: "guid",
                toolbar: {
                    tools: []
                }
            }).data("kendoListBox");

            that.assignedListBox = that.element.find("#assignedRoles").kendoListBox({
                dataTextField: "name",
                dataValueField: "guid",
                toolbar: {
                    tools: []
                }
            }).data("kendoListBox");

            that.element.find(".assign-btn").on("click", function (e) {
                e.preventDefault();
                that._moveToAssigned();
            });

            that.element.find(".unassign-btn").on("click", function (e) {
                e.preventDefault();
                that._moveToAvailable();
            });

            that.element.find(".save-btn").on("click", function (e) {
                e.preventDefault();
                that._saveChanges();
            });

            that.element.find(".cancel-btn").on("click", function (e) {
                e.preventDefault();
                that._cancelChanges();
            });

            that._addStyles();

            that._updateButtonStates();
        },

        _fetchRoles: function () {
            var that = this,
                options = that.options;

            $.ajax({
                url: "/api/RoleRegistry",
                method: "GET",
                success: function (allRoles) {
                    $.ajax({
                        url: options.baseEndpoint + "/By" + options.parentKeyField + "/" + options.guid,
                        method: "GET",
                        success: function (assignedRoles) {
                            that._populateLists(allRoles, assignedRoles);
                        },
                        error: function (xhr, status, error) {
                            console.error("Error fetching assigned roles:", error);
                            that._populateLists(allRoles, []);
                        }
                    });
                },
                error: function (xhr, status, error) {
                    console.error("Error fetching roles:", error);
                }
            });
        },

        _populateLists: function (allRoles, assignedRoles) {
            var that = this
            var assignedGuidSet = new Set();
            var guidToAssignmentId = new Map();

            assignedRoles.forEach(function (ar) {
                var guid = ar.RoleRegistryGuid || ar.roleRegistryGuid || ar["guid"];
                var assignmentId = ar.Id || ar.id || ar["id"];
                if (guid) {
                    assignedGuidSet.add(guid);
                    if (assignmentId != null) guidToAssignmentId.set(guid, assignmentId);
                }
            });

            that._originalAssignedIds = Array.from(assignedGuidSet);

            var available = [];
            var assigned = [];

            allRoles.forEach(function (role) {
                var guid = role.RoleRegistryGuid || role.roleRegistryGuid || role["guid"];
                var name = role.Name || role.name || role["name"];

                if (!guid) return;

                var item = {};
                item["guid"] = guid;
                item["name"] = name;

                if (assignedGuidSet.has(guid)) {
                    item["id"] = guidToAssignmentId.get(guid);
                    assigned.push(item);
                } else {
                    available.push(item);
                }
            });

            that.availableListBox.setDataSource(new kendo.data.DataSource({data: available}));
            that.assignedListBox.setDataSource(new kendo.data.DataSource({data: assigned}));

            that._pendingChanges = {toAssign: [], toUnassign: []};
            that._updateButtonStates();
        },

        _moveToAssigned: function () {
            var that = this;
            var selected = that.availableListBox.select();
            if (!selected || selected.length === 0) return;

            selected.each(function () {
                var dataItem = that.availableListBox.dataItem($(this));
                var availableDS = that.availableListBox.dataSource;
                var assignedDS = that.assignedListBox.dataSource;

                var itemCopy = {};
                itemCopy["guid"] = dataItem["guid"];
                itemCopy["name"] = dataItem["name"];

                availableDS.remove(dataItem);
                assignedDS.add(itemCopy);

                that._trackChange(itemCopy, 'assign');
            });

            that._updateButtonStates();
        },

        _moveToAvailable: function () {
            var that = this;
            var selected = that.assignedListBox.select();
            if (!selected || selected.length === 0) return;

            selected.each(function () {
                var dataItem = that.assignedListBox.dataItem($(this));
                var availableDS = that.availableListBox.dataSource;
                var assignedDS = that.assignedListBox.dataSource;

                var itemCopy = {};
                itemCopy["guid"] = dataItem["guid"];
                itemCopy["name"] = dataItem["name"];

                if (dataItem["id"] != null) {
                    itemCopy["id"] = dataItem["id"];
                }

                assignedDS.remove(dataItem);
                availableDS.add(itemCopy);

                that._trackChange(dataItem, 'unassign');
            });

            that._updateButtonStates();
        },

        _trackChange: function (dataItem, action) {
            var that = this;
            var guid = dataItem["guid"];
            var assignmentId = dataItem["id"];
            var wasOriginallyAssigned = that._originalAssignedIds.indexOf(guid) >= 0;

            if (action === 'assign') {
                if (assignmentId != null) {
                    var idxUn = that._pendingChanges.toUnassign.indexOf(assignmentId);
                    if (idxUn >= 0) that._pendingChanges.toUnassign.splice(idxUn, 1);
                }

                if (!wasOriginallyAssigned && that._pendingChanges.toAssign.indexOf(guid) < 0) {
                    that._pendingChanges.toAssign.push(guid);
                }
            } else {
                var idxAsn = that._pendingChanges.toAssign.indexOf(guid);
                if (idxAsn >= 0) {
                    that._pendingChanges.toAssign.splice(idxAsn, 1);
                    return;
                }

                if (wasOriginallyAssigned && assignmentId != null && that._pendingChanges.toUnassign.indexOf(assignmentId) < 0) {
                    that._pendingChanges.toUnassign.push(assignmentId);
                }
            }
        },

        _saveChanges: function () {
            var that = this, options = that.options;
            if (!that._hasPendingChanges()) return;

            var promises = [];

            that._pendingChanges.toAssign.forEach(function (roleGuid) {
                var data = {
                    [options.parentKeyField]: options.guid,
                    RoleRegistryGuid: roleGuid
                };

                promises.push($.ajax({
                    url: options.baseEndpoint,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(data)
                }));
            });

            that._pendingChanges.toUnassign.forEach(function (assignmentId) {
                promises.push($.ajax({
                    url: options.baseEndpoint + "/" + encodeURIComponent(assignmentId),
                    method: "DELETE",
                    contentType: "application/json"
                }));
            });

            $.when.apply($, promises).done(function () {
                that._originalAssignedIds = [];

                that.assignedListBox.dataSource.data().forEach(function (item) {
                    that._originalAssignedIds.push(item["guid"]);
                });

                that._pendingChanges = {toAssign: [], toUnassign: []};
                that._updateButtonStates();
                that.trigger("save", {success: true});
            }).fail(function (xhr, status, error) {
                console.error("Error saving changes:", error);
                alert("Error saving changes. Please try again.");
                that.trigger("save", {success: false, error: error});
            });
        },

        _cancelChanges: function () {
            var that = this;

            if (!that._hasPendingChanges()) {
                return;
            }

            that._fetchRoles();
            that.trigger("cancel");
        },

        _hasPendingChanges: function () {
            var that = this;
            return that._pendingChanges.toAssign.length > 0 ||
                that._pendingChanges.toUnassign.length > 0;
        },

        _updateButtonStates: function () {
            var that = this,
                hasPending = that._hasPendingChanges();

            var saveBtn = that.element.find(".save-btn");
            var cancelBtn = that.element.find(".cancel-btn");

            if (hasPending) {
                saveBtn.removeClass("k-disabled").prop("disabled", false);
                cancelBtn.removeClass("k-disabled").prop("disabled", false);
            } else {
                saveBtn.addClass("k-disabled").prop("disabled", true);
                cancelBtn.addClass("k-disabled").prop("disabled", true);
            }
        },

        _addStyles: function () {
            if ($("#role-manager-styles").length === 0) {
                var styles = `
                    <style id="role-manager-styles">
                        .k-role-manager-wrapper {
                            display: inline-block;
                        }
                        .k-role-manager-container {
                            display: flex;
                            gap: 10px;
                            align-items: flex-start;
                        }
                        .k-role-manager-panel {
                            flex: 1;
                        }
                        .k-role-manager-header {
                            font-weight: bold;
                            padding: 8px;
                            background-color: #f5f5f5;
                            border: 1px solid #ddd;
                            border-bottom: none;
                        }
                        .k-role-manager-buttons {
                            display: flex;
                            flex-direction: column;
                            gap: 10px;
                            padding-top: 40px;
                        }
                        .k-role-manager .k-listbox {
                            width: 100%;
                        }
                        .k-role-manager-actions {
                            margin-top: 15px;
                            text-align: right;
                            padding-right: 5px;
                        }
                        .k-role-manager-actions .k-button {
                            margin-left: 10px;
                        }
                    </style>
                `;
                $("head").append(styles);
            }
        },

        refresh: function () {
            this._fetchRoles();
        },

        destroy: function () {
            var that = this;

            that.availableListBox.destroy();
            that.assignedListBox.destroy();
            that.element.find(".assign-btn").off("click");
            that.element.find(".unassign-btn").off("click");
            that.element.find(".save-btn").off("click");
            that.element.find(".cancel-btn").off("click");

            Widget.fn.destroy.call(that);
        }
    });

    ui.plugin(RoleManager);

})(jQuery);

//# sourceURL=RoleAllocation.js