/* Copyright (C) 2022-present Jube Holdings Limited.

 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License 
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty  
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not, 
 * see <https://www.gnu.org/licenses/>.
 */

var processingFailed = "Processing failed.  Please contact Support to check logs for the source of the error.";
var id;
var guid;
var hasResponsePayload = false;
var hasReportTable = false;
var parentKey;
var active;
var locked;
var responsePayload;
var deleteButton;
var addButton;
var updateButton;
var validator;
var reportTable;

const components = {
    get active() {
        if (!active || !active.length) active = $("#Active");
        return active.data("kendoSwitch") || active.kendoSwitch().data("kendoSwitch");
    },
    get locked() {
        if (!locked || !locked.length) locked = $("#Locked");
        return locked.data("kendoSwitch") || locked.kendoSwitch().data("kendoSwitch");
    },
    get responsePayload() {
        if (!responsePayload || !responsePayload.length) responsePayload = $("#ResponsePayload");
        return responsePayload;
    },
    get deleteButton() {
        if (!deleteButton || !deleteButton.length) deleteButton = $("#Delete");
        if (!deleteButton.data("kendoButton")) deleteButton.kendoButton();
        return deleteButton;
    },
    get addButton() {
        if (!addButton || !addButton.length) addButton = $("#Add");
        if (!addButton.data("kendoButton")) addButton.kendoButton();
        return addButton;
    },
    get updateButton() {
        if (!updateButton || !updateButton.length) updateButton = $("#Update");
        if (!updateButton.data("kendoButton")) updateButton.kendoButton();
        return updateButton;
    },
    get validator() {
        if (!validator) {
            const form = $("#Form");
            if (form.length) {
                validator = form.data("kendoValidator") || form.kendoValidator({validateOnBlur: false}).data("kendoValidator");
            }
        }
        return validator;
    },
    get reportTable() {
        if (!reportTable || !reportTable.length) reportTable = $("#ReportTable");
        return reportTable;
    },
    get errorMessage() {
        return $("#ErrorMessage");
    }
};

$(() => {
    active = $("#Active").kendoSwitch();
    locked = $("#Locked").kendoSwitch();
    responsePayload = $("#ResponsePayload");
    deleteButton = $("#Delete").kendoButton();
    addButton = $("#Add").kendoButton();
    updateButton = $("#Update").kendoButton();
    reportTable = $("#ReportTable");
    const form = $("#Form");
    if (form.length) {
        validator = form.kendoValidator({validateOnBlur: false}).data("kendoValidator");
    }

    if (typeof GetSelectedChildID === "function") {
        id = GetSelectedChildID();
    }

    if (typeof GetSelectedParentID === "function") {
        parentKey = GetSelectedParentID();
    }

    if (responsePayload.length > 0) {
        responsePayload.kendoSwitch();
        hasResponsePayload = true;
    }

    if (reportTable.length > 0) {
        reportTable.kendoSwitch();
        hasReportTable = true;
    }
});

function AddTemplateElements(data, keyName, parentKeyName) {
    const activeWidget = components.active;
    const lockedWidget = components.locked;

    data.active = activeWidget ? activeWidget.check() : false;
    data.locked = lockedWidget ? lockedWidget.check() : false;

    if (hasResponsePayload) {
        data.responsePayload = components.responsePayload.prop("checked");
    }

    if (hasReportTable) {
        data.reportTable = components.reportTable.prop("checked");
    }

    const $nameField = $("#Name");
    if ($nameField.length > 0) {
        data.name = $nameField.val();
    }

    data.id = id;

    if (parentKeyName !== undefined) {
        if (data[parentKeyName] === undefined) {
            data[parentKeyName] = parentKey;
        }
    }

    return data;
}

function DisplayServerValidationErrors(responseObject) {
    const $errorContainer = components.errorMessage;
    $errorContainer.empty();

    const $container = $(`
        <div class="server-error-box">
            <div class="server-error-title">Validation Errors:</div>
        </div>
    `);

    if (responseObject.errors) {
        Object.values(responseObject.errors).forEach(e => {
            $container.append(`<div class="server-error-line">${e.errorMessage}</div>`);
            highlightFieldError(e.propertyName, e.errorMessage);
        });
    }

    $errorContainer.append($container);
}

function HandleDataSourceError(e) {
    const jqXHR = e && e.xhr;

    if (jqXHR && jqXHR.status === 400) {
        try {
            DisplayServerValidationErrors(JSON.parse(jqXHR.responseText));
            return;
        } catch (ex) {
            // fall through to the generic failure message below
        }
    }

    components.errorMessage.html(processingFailed);
}

function WireListViewValidation(listView, dataSource) {
    let pendingUid = null;

    listView.bind("save", function (e) {
        pendingUid = e.model.uid;
    });

    dataSource.bind("sync", function () {
        pendingUid = null;
        clearFieldErrorStyles();
    });

    dataSource.bind("error", function (e) {
        HandleDataSourceError(e);

        if (!pendingUid) {
            return;
        }

        const uid = pendingUid;
        pendingUid = null;

        const row = listView.items().filter("[data-uid='" + uid + "']");
        if (!row.length) {
            return;
        }

        listView.edit(row);
        listView.items().filter("[data-uid='" + uid + "']").addClass("field-error-highlight");
    });
}

function hideRoles() {
    const roleManager = $("#RoleManager").data("kendoRoleManager");
    if (roleManager) {
        roleManager.destroy();
    }
}

function clearFieldErrorStyles() {
    components.errorMessage.empty();

    $(".field-error-highlight").each(function () {
        const $el = $(this);
        $el.removeClass("field-error-highlight").removeAttr("title");

        // Destroy bootstrap tooltip if it exists
        if ($el.data('bs.tooltip')) {
            $el.tooltip('dispose');
        }
    });
}

function highlightFieldError(fieldId, errorMessage) {
    let $field = $(`#${fieldId}`);

    // Handle radio buttons
    const $radios = $(`input[name='${fieldId}'][type='radio']`);
    if ($radios.length > 1) {
        $radios.each(function () {
            const $radio = $(this);
            $radio.addClass("field-error-highlight").attr("title", errorMessage);
            if ($radio.tooltip) $radio.tooltip();
        });
        return;
    }

    if (!$field.length) return;

    const kendoWidgets = [
        "kendoNumericTextBox", "kendoSwitch", "kendoTextBox", "kendoMaskedTextBox",
        "kendoDropDownList", "kendoComboBox", "kendoMultiSelect", "kendoDatePicker", "kendoDateTimePicker"
    ];

    let widget = null;
    for (const type of kendoWidgets) {
        widget = $field.data(type);
        if (widget) break;
    }

    if (widget) {
        const $wrapper = widget.wrapper || widget.element.closest(".k-widget");
        if ($wrapper && $wrapper.length) {
            $wrapper.addClass("field-error-highlight").attr("title", errorMessage);
            if ($wrapper.tooltip) $wrapper.tooltip();
            return;
        }
    }

    $field.addClass("field-error-highlight").attr("title", errorMessage);
    if ($field.tooltip) $field.tooltip();
}

function Create(endpoint, data, keyName, parentKeyName, callback) {
    components.errorMessage.html('');
    clearFieldErrorStyles();

    return $.ajax({
        url: endpoint,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify(AddTemplateElements(data, keyName, parentKeyName)),
        error: (jqXHR) => {
            if (jqXHR.status === 400) {
                try {
                    const response = JSON.parse(jqXHR.responseText);
                    DisplayServerValidationErrors(response);
                } catch (e) {
                    components.errorMessage.html(processingFailed);
                }
            } else {
                components.errorMessage.html(processingFailed);
            }
        },
        success: (responseData) => {
            if (parentKeyName !== undefined) {
                parentKey = responseData[parentKeyName];
            }

            id = responseData.id;
            guid = responseData.guid;

            if (responseData.version === 1) {
                if (typeof AddNode === "function") {
                    const nodeName = $("#Name").length > 0 ? responseData.name : window.Name;
                    AddNode(responseData[parentKeyName], id, nodeName);
                }
            }

            components.addButton.hide();
            components.updateButton.show();
            components.deleteButton.show();

            if (typeof showRoleAllocation === "function") {
                showRoleAllocation();
            }

            const $guidLabel = $("#Guid");
            if ($guidLabel.length > 0) {
                $guidLabel.html(responseData.guid);
            }

            $("#Version").html(responseData.version);
            $("#CreatedUser").html(responseData.createdUser);
            $("#CreatedDate").html(new Date(responseData.createdDate).toLocaleString());

            SetTable();

            const lockedWidget = components.locked;
            Lock(lockedWidget ? lockedWidget.check() : false);

            if (typeof callback === "function") {
                callback(responseData);
            }
        }
    });
}

function Update(endpoint, data, keyName, parentKeyName, callback) {
    components.errorMessage.html('');
    clearFieldErrorStyles();

    return $.ajax({
        url: endpoint,
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify(AddTemplateElements(data, keyName, parentKeyName)),
        error: (jqXHR) => {
            if (jqXHR.status === 400) {
                try {
                    const response = JSON.parse(jqXHR.responseText);
                    DisplayServerValidationErrors(response);
                } catch (e) {
                    components.errorMessage.html(processingFailed);
                }
            } else {
                components.errorMessage.html(processingFailed);
            }
        },
        success: (responseData) => {
            id = responseData.id;

            $("#Version").html(responseData.version);
            $("#CreatedUser").html(responseData.createdUser);
            $("#CreatedDate").html(new Date(responseData.createdDate).toLocaleString());

            components.addButton.hide();
            components.updateButton.show();
            components.deleteButton.show();

            const lockedWidget = components.locked;
            Lock(lockedWidget ? lockedWidget.check() : false);

            if (typeof callback === "function") {
                callback(responseData);
            }
        }
    });
}

function Delete(endpoint, key) {
    components.errorMessage.html('');
    clearFieldErrorStyles();

    return $.ajax({
        url: `${endpoint}/${key}`,
        type: "DELETE",
        error: () => {
            components.errorMessage.html(processingFailed);
        },
        success: () => {
            if (typeof DeleteNode === "function") {
                DeleteNode(key, 1);
            } else if (typeof showHomePage === "function") {
                showHomePage();
            }
        }
    });
}

function SetTable() {
    if (window.OverrideSetTable) {
        window.OverrideSetTable();
        return;
    }

    const $table = $('#TemplateTable');
    if (!$table.length) return;

    let rowsCount = $table.find('tr').length;
    rowsCount -= ($("#Guid").length > 0) ? 5 : 4;

    const rowsToToggle = $table.find("tr:gt(" + rowsCount + ")");
    if (id !== undefined) {
        rowsToToggle.show();
    } else {
        rowsToToggle.hide();
    }
}

function Lock(isLocked) {
    if (isLocked) {
        components.deleteButton.hide();
        components.updateButton.hide();
    } else {
        components.deleteButton.show();
        components.updateButton.show();
    }
}

function ReadyNew() {
    id = undefined;
    $("#Name").val("");
    $("#Version, #CreatedDate, #CreatedUser").html("");
    $("#Counters").html("0 / 0 (0%)");
    $("#LastCounters").html(new Date().toLocaleString());

    components.updateButton.hide();
    components.deleteButton.hide();
    components.addButton.show();

    const activeWidget = components.active;
    if (activeWidget) activeWidget.check(false);

    const lockedWidget = components.locked;
    if (lockedWidget) {
        lockedWidget.check(false);
        lockedWidget.enable(false);
    }

    SetTable();
}

function ReadyExisting(data) {
    id = data.id;
    $("#Name").val(data.name);

    if (hasResponsePayload) {
        const sw = components.responsePayload.data("kendoSwitch");
        if (sw) sw.check(!!data.responsePayload);
    }

    if (hasReportTable) {
        const sw = components.reportTable.data("kendoSwitch");
        if (sw) sw.check(!!data.reportTable);
    }

    const activeWidget = components.active;
    if (activeWidget) activeWidget.check(!!data.active);

    guid = data.guid;

    const $guidTextbox = $("#Guid");
    if ($guidTextbox.length > 0) {
        $guidTextbox.html(data.guid);
    }

    $("#Version").html(data.version);
    $("#CreatedDate").html(new Date(data.createdDate).toLocaleString());
    $("#CreatedUser").html(data.createdUser);

    SetTable();

    components.addButton.hide();
    components.updateButton.show();
    components.deleteButton.show();

    if (typeof showRoleAllocation === "function") {
        showRoleAllocation();
    }

    const lockedWidget = components.locked;
    if (lockedWidget) {
        lockedWidget.check(!!data.locked);
        Lock(!!data.locked);
        lockedWidget.enable(false);
    }
}

function showRoleAllocation() {
    const $roleManager = $('#RoleManager');

    if ($roleManager.length) {
        hideRoles();

        $roleManager.kendoRoleManager({
            baseEndpoint: (window.endpoint || "") + "Role",
            parentKeyField: window.childKeyName,
            guid: guid
        });
    }
}

//# sourceURL=CRUD.js