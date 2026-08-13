// noinspection ES6ConvertVarToLetConst,JSUnresolvedVariable,HtmlUnknownAttribute

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

let key;
let keyValue;

const processingFailed = "Processing failed.  Please contact Support to check logs for the source of the error.";

function onChange() {
    const grid = $("#grid").getKendoGrid();
    grid.dataSource.sync();
}

function ExistsSelect(test) {
    let exists = false;
    $($("#SuppressionKey").data("kendoDropDownList").dataItems()).each(function () {
        if (this.value === test) {
            exists = true;
        }
    });
    return exists;
}

function detailInit(e) {
    // noinspection JSObsoletePrivateAccessSyntax
    $("<div/>").appendTo(e.detailCell).kendoGrid({
        dataSource: {
            transport: {
                read: {
                    url: "../api/GetEntityAnalysisModelActivationsRuleSuppressionQuery",
                    data: {
                        suppressionKey: key,
                        suppressionKeyValue: keyValue,
                        entityAnalysisModelGuid: e.data.entityAnalysisModelGuid
                    },
                    dataType: "json"
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        suppression: {type: "boolean"},
                        name: {type: "string", editable: false},
                        deleteExpiryDate: {type: "date", defaultValue: null}
                    }
                }
            }
        },
        dataBound: function () {
            const grid = this;

            grid.tbody.find("tr").each(function () {
                const row = $(this);
                const toggleSwitchElement = row.find(".toggleSuppressionActivationRule");
                const dateInputElement = row.find(".expiryDateSuppressionActivationRule");

                if (!toggleSwitchElement.length) {
                    return;
                }

                const dataItem = grid.dataItem(row);

                const datePicker = dateInputElement.kendoDateTimePicker({
                    value: dataItem.deleteExpiryDate,
                    change: function (e) {
                        const newValue = e.sender.value();
                        const $errorMessage = $("#ErrorMessage");
                        if (!IsFutureDate(newValue)) {
                            $errorMessage.html(
                                '<div class="server-error-box"><div class="server-error-title">Validation Errors:</div>' +
                                '<div class="server-error-line">Delete Expiry Date must be greater than the current date and time.</div></div>');
                            e.sender.value(dataItem.deleteExpiryDate);
                            return;
                        }
                        $errorMessage.empty();
                        UpdateSuppressionActivationRuleDeleteExpiryDate(dataItem.entityAnalysisModelGuid,
                            dataItem.name, newValue, e.sender, dataItem.deleteExpiryDate);
                    }
                }).data("kendoDateTimePicker");

                datePicker.enable(dataItem.suppression === true);

                toggleSwitchElement.kendoSwitch({
                    change: function (e) {
                        SyncExpiryDatePicker(datePicker, e.checked);
                        UpdateSuppressionActivationRule(toggleSwitchElement.attr("EntityAnalysisModelGuid"),
                            toggleSwitchElement.attr("Name"), e.checked);
                    }
                });
            });
        },
        columns: [
            {
                field: "suppression",
                template:
                    '<input Name="#=name#" EntityAnalysisModelGuid="#=entityAnalysisModelGuid#" type="checkbox" class="toggleSuppressionActivationRule" #= (suppression==true) ? checked="checked" : "" # />',
                width: 110,
                title: "Suppression"
            },
            {
                field: "deleteExpiryDate",
                title: "Delete Expiry Date",
                width: 230,
                template: '<input type="text" class="expiryDateSuppressionActivationRule expiryDateCell" />'
            },
            {
                field: "name",
                title: "Activation Rule"
            }
        ]
    });
}

function IsFutureDate(value) {
    return !value || value > new Date();
}

function SyncExpiryDatePicker(picker, enabled) {
    if (!picker) {
        return;
    }

    picker.value(null);
    picker.enable(enabled);
}

function DisplayValidationErrors(jqXHR) {
    const $errorContainer = $("#ErrorMessage");
    $errorContainer.empty();

    if (jqXHR.status !== 400) {
        $errorContainer.html(processingFailed);
        return;
    }

    try {
        const response = JSON.parse(jqXHR.responseText);
        const $container = $(
            '<div class="server-error-box"><div class="server-error-title">Validation Errors:</div></div>');

        if (response.errors) {
            Object.values(response.errors).forEach(function (e) {
                $container.append('<div class="server-error-line">' + e.errorMessage + '</div>');
            });
        }

        $errorContainer.append($container);
    } catch (e) {
        $errorContainer.html(processingFailed);
    }
}

function UpdateSuppressionModel(EntityAnalysisModelGuid, checked) {
    $('#Updating').show();
    $.ajax({
        url: "/api/EntityAnalysisModelSuppression",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            entityAnalysisModelGuid: EntityAnalysisModelGuid,
            suppressionKeyValue: keyValue,
            suppressionKey: key,
            active: checked
        }),
        error: function () {
            $('#Updating').fadeOut();
        },
        success: function () {
            $('#Updating').fadeOut();
        }
    });
}

function UpdateSuppressionActivationRule(EntityAnalysisModelGuid, name, checked) {
    $('#Updating').show();
    $.ajax({
        url: "/api/EntityAnalysisModelActivationRuleSuppression",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            entityAnalysisModelGuid: EntityAnalysisModelGuid,
            suppressionKeyValue: keyValue,
            suppressionKey: key,
            active: checked,
            entityAnalysisModelActivationRuleName: name
        }),
        error: function () {
            $('#Updating').fadeOut();
        },
        success: function () {
            $('#Updating').fadeOut();
        }
    });
}

function UpdateSuppressionModelDeleteExpiryDate(EntityAnalysisModelGuid, deleteExpiryDate, picker, previousValue) {
    $('#Updating').show();
    $.ajax({
        url: "/api/EntityAnalysisModelSuppression/DeleteExpiryDate",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            entityAnalysisModelGuid: EntityAnalysisModelGuid,
            suppressionKeyValue: keyValue,
            suppressionKey: key,
            deleteExpiryDate: deleteExpiryDate
        }),
        error: function (jqXHR) {
            $('#Updating').fadeOut();
            DisplayValidationErrors(jqXHR);
            if (picker) {
                picker.value(previousValue || null);
            }
        },
        success: function () {
            $('#Updating').fadeOut();
            $("#ErrorMessage").empty();
        }
    });
}

function UpdateSuppressionActivationRuleDeleteExpiryDate(EntityAnalysisModelGuid, name, deleteExpiryDate, picker,
    previousValue) {
    $('#Updating').show();
    $.ajax({
        url: "/api/EntityAnalysisModelActivationRuleSuppression/DeleteExpiryDate",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            entityAnalysisModelGuid: EntityAnalysisModelGuid,
            suppressionKeyValue: keyValue,
            suppressionKey: key,
            deleteExpiryDate: deleteExpiryDate,
            entityAnalysisModelActivationRuleName: name
        }),
        error: function (jqXHR) {
            $('#Updating').fadeOut();
            DisplayValidationErrors(jqXHR);
            if (picker) {
                picker.value(previousValue || null);
            }
        },
        success: function () {
            $('#Updating').fadeOut();
            $("#ErrorMessage").empty();
        }
    });
}

$(document).ready(function () {
    $("#Fetch").kendoButton({
        click: function () {
            {
                keyValue = $("#SuppressionKeyValue").val();
                key = $("#SuppressionKey").data("kendoDropDownList").value();

                const grid = $('#grid').data('kendoGrid');
                grid.dataSource.options.transport.read.data.SuppressionKeyValue = keyValue;
                grid.dataSource.options.transport.read.data.SuppressionKey = key;
                grid.dataSource.read();
            }
        }
    });

    $("#SuppressionKey").kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value"
    });

    $.get("/api/EntityAnalysisModelRequestXPath/BySuppressionKey",
        function (data) {
            $.each(data,
                function (i, value) {
                    if (ExistsSelect(value.name) === false) {
                        $("#SuppressionKey").getKendoDropDownList().dataSource.add({
                            "value": value.name,
                            "text": value.name
                        });
                    }
                });
        }
    );

    // noinspection JSObsoletePrivateAccessSyntax
    $("#grid").kendoGrid({
        dataSource: {
            transport: {
                read: {
                    url: "../api/GetEntityAnalysisModelSuppressionQuery",
                    data: {suppressionKeyValue: keyValue, suppressionKey: key},
                    dataType: "json"
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        suppression: {type: "boolean"},
                        name: {type: "string", editable: false},
                        deleteExpiryDate: {type: "date", defaultValue: null}
                    }
                }
            }
        },
        height: 600,
        sortable: true,
        autoBind: false,
        detailInit: detailInit,
        dataBound: function () {
            const grid = this;

            grid.tbody.find("tr").each(function () {
                const row = $(this);
                const toggleSwitchElement = row.find(".toggleSuppression");
                const dateInputElement = row.find(".expiryDateSuppression");

                if (!toggleSwitchElement.length) {
                    return;
                }

                const dataItem = grid.dataItem(row);

                const datePicker = dateInputElement.kendoDateTimePicker({
                    value: dataItem.deleteExpiryDate,
                    change: function (e) {
                        const newValue = e.sender.value();
                        const $errorMessage = $("#ErrorMessage");
                        if (!IsFutureDate(newValue)) {
                            $errorMessage.html(
                                '<div class="server-error-box"><div class="server-error-title">Validation Errors:</div>' +
                                '<div class="server-error-line">Delete Expiry Date must be greater than the current date and time.</div></div>');
                            e.sender.value(dataItem.deleteExpiryDate);
                            return;
                        }
                        $errorMessage.empty();
                        UpdateSuppressionModelDeleteExpiryDate(dataItem.entityAnalysisModelGuid, newValue,
                            e.sender, dataItem.deleteExpiryDate);
                    }
                }).data("kendoDateTimePicker");

                datePicker.enable(dataItem.suppression === true);

                toggleSwitchElement.kendoSwitch({
                    change: function (e) {
                        SyncExpiryDatePicker(datePicker, e.checked);
                        UpdateSuppressionModel(toggleSwitchElement.attr("EntityAnalysisModelGuid"), e.checked);
                    }
                });
            });
        },
        columns: [
            {
                field: "suppression",
                template:
                    '<input EntityAnalysisModelGuid="#=entityAnalysisModelGuid#" type="checkbox" class="toggleSuppression" #= (suppression==true) ? checked="checked" : "" # />',
                width: 110,
                title: "Suppression"
            },
            {
                field: "deleteExpiryDate",
                title: "Delete Expiry Date",
                width: 230,
                template: '<input type="text" class="expiryDateSuppression expiryDateCell" />'
            },
            {
                field: "name",
                title: "Model"
            }
        ]
    });
});

//# sourceURL=Suppression.js