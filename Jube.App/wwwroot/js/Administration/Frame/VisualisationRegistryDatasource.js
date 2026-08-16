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

var endpoint = "/api/VisualisationRegistryDatasource";
var parentKeyName = "visualisationRegistryId";
var childKeyName = "visualisationRegistryDatasourceGuid";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var coderBacking;
var coderDisplay;

var columnSpan = $("#ColumnSpan").kendoNumericTextBox({
    format: "#",
    decimals: 0,
    step: 1
});

var rowSpan = $("#RowSpan").kendoNumericTextBox({
    format: "#",
    decimals: 0,
    step: 1
});

var includeGrid = $("#IncludeGrid").kendoSwitch();

var includeDisplay = $("#IncludeDisplay").kendoSwitch({
    change: function () {
        SetGroupDisplay();
    }
});

var priority = $("#Priority").kendoNumericTextBox({
    format: "n3",
    decimals: 3,
    step: 0.001
});

function changeVisualisationType() {
    const formatValue = $("input[name=VisualisationType]:checked").val();
    if (formatValue === '1') {
        coderDisplay.session.setMode("ace/mode/javascript");
    } else if (formatValue === '2') {
        coderDisplay.session.setMode("ace/mode/javascript");
    } else {
        coderDisplay.session.setMode("ace/mode/html");
    }
}

$("input[name=VisualisationType]").on("change", changeVisualisationType);

function SetGroupDisplay() {
    const table = $('#DisplayTable');
    if (includeDisplay.prop("checked")) {
        table.show();

        coderDisplay.setTheme('ace/theme/sqlserver');
        coderDisplay.getSession().setMode('ace/mode/json');
        coderDisplay.setReadOnly(false);

    } else {
        table.hide();
    }
}

$.getScript('/js/ace/ace.js', function () {
    ace.config.set('basePath', '/js/ace/');

    coderBacking = ace.edit('coderBacking');
    coderBacking.setTheme('ace/theme/sqlserver');
    coderBacking.getSession().setMode('ace/mode/sqlserver');
    coderBacking.setReadOnly(false);

    coderDisplay = ace.edit('coderDisplay');

    if (typeof id === "undefined") {
        SetGroupDisplay();
        changeVisualisationType();
        ReadyNew();
    } else {
        $.get(endpoint + "/" + id,
            function (data) {
                columnSpan.data("kendoNumericTextBox").value(data.columnSpan);
                rowSpan.data("kendoNumericTextBox").value(data.rowSpan);
                priority.data("kendoNumericTextBox").value(data.priority);

                $("input[name=VisualisationType][value=" + data.visualisationTypeId + "]")
                    .prop('checked', true);

                coderBacking.setValue(data.command);
                coderDisplay.setValue(data.visualisationText);

                if (data.includeGrid) {
                    includeGrid.data("kendoSwitch").check(true);
                } else {
                    includeGrid.data("kendoSwitch").check(false);
                }

                if (data.includeDisplay) {
                    includeDisplay.data("kendoSwitch").check(true);
                } else {
                    includeDisplay.data("kendoSwitch").check(false);
                }

                SetGroupDisplay();
                changeVisualisationType();
                ReadyExisting(data);
            });
    }
});

$(function () {
    deleteButton
        .click(function () {
            if (confirm('Are you sure you want to delete?')) {
                Delete(endpoint, id);
            }
        });
});

function GetData() {
    return {
        includeGrid: includeGrid.prop("checked"),
        includeDisplay: includeDisplay.prop("checked"),
        visualisationText: coderDisplay.getValue(),
        visualisationTypeId: $('input[name=VisualisationType]:checked').val(),
        command: coderBacking.getValue(),
        priority: priority.val(),
        rowSpan: rowSpan.val(),
        columnSpan: columnSpan.val()
    };
}

$(function () {
    addButton
        .click(function () {
            if (validator.validate()) {
                Create(endpoint, GetData(), "id", parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

$(function () {
    updateButton
        .click(function () {
            if (validator.validate()) {
                Update(endpoint, GetData(), "id", parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

//# sourceURL=UserRegistry.js