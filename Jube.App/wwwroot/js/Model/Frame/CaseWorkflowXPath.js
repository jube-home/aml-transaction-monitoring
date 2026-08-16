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

var endpoint = "/api/CaseWorkflowXPath";
var parentKeyName = "caseWorkflowId";
var childKeyName = "caseWorkflowXPathGuid";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var xPath = $("#XPath").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var drill = $("#Drill").kendoSwitch({
    change: function () {
        SetEnableHttpEndpoint();
    }
});

var boldLineMatched = $("#BoldLineMatched").kendoSwitch({
    change: function () {
        SetBoldLineMatched();
    }
});

var conditionalRegularExpressionFormatting = $("#ConditionalRegularExpressionFormatting").kendoSwitch({
    change: function () {
        SetConditionalRegularExpressionFormatting();
    }
});

var foreRowColorScope = $("#ForeRowColorScope").kendoSwitch({
    width: 62,
    messages: {
        checked: "Row",
        unchecked: "Cell"
    }
});

var backRowColorScope = $("#BackRowColorScope").kendoSwitch({
    width: 62,
    messages: {
        checked: "Row",
        unchecked: "Cell"
    }
});

var forePicker = $("#ForePicker").kendoColorPicker({
    buttons: false,
    value: "#000000"
});

var backPicker = $("#BackPicker").kendoColorPicker({
    buttons: false,
    value: "#ffffff"
});

var boldLineForePicker = $("#BoldLineForePicker").kendoColorPicker({
    buttons: false,
    value: "#000000"
});

var boldLineBackPicker = $("#BoldLineBackPicker").kendoColorPicker({
    buttons: false,
    value: "#ffffff"
});

var fields = xPath.getKendoDropDownList();
fields.dataSource.data([]);
fields.text("");
fields.value("");


function SetEnableHttpEndpoint() {
    const $httpEndpointTable = $("#HttpEndpointTable");
    if ($('#EnableHttpEndpoint').prop('checked')) {
        $httpEndpointTable.show();
    } else {
        $httpEndpointTable.hide();
    }
}

function SetEnableNotification() {
    const $notificationTable = $("#NotificationTable");
    if ($('#EnableNotification').prop('checked')) {
        $notificationTable.show();
    } else {
        $notificationTable.hide();
    }
}

function SetConditionalRegularExpressionFormatting() {
    const $conditionalRegularExpressionFormattingTable = $("#ConditionalRegularExpressionFormattingTable");
    if ($('#ConditionalRegularExpressionFormatting').prop('checked')) {
        $conditionalRegularExpressionFormattingTable.show();
    } else {
        $conditionalRegularExpressionFormattingTable.hide();
    }
}

function SetBoldLineMatched() {
    const $boldLineMatchedTable = $("#BoldLineMatchedTable");
    if ($('#BoldLineMatched').prop('checked')) {
        $boldLineMatchedTable.show();
    } else {
        $boldLineMatchedTable.hide();
    }
}

$.get("/api/Completions/ByCaseWorkflowId",
    {
        caseWorkflowId: parentKey
    },
    function (data) {
        if (typeof data !== 'undefined') {
            for (const value of data) {
                fields.dataSource.add({
                    "value": value.xPath,
                    "text": value.name
                });
            }
        }

        if (typeof id === "undefined") {
            if (fields.dataSource.data().length > 0) {
                fields.select(0);
            }

            SetEnableHttpEndpoint();
            SetEnableNotification();
            SetConditionalRegularExpressionFormatting();
            SetBoldLineMatched();
            ReadyNew();
        } else {
            $.get(endpoint + "/" + id,
                function (data) {
                    $("#RegularExpression").val(data.regularExpression);

                    if (data.foreRowColorScope) {
                        foreRowColorScope.data("kendoSwitch").check(true);
                    } else {
                        foreRowColorScope.data("kendoSwitch").check(false);
                    }

                    if (data.backRowColorScope) {
                        backRowColorScope.data("kendoSwitch").check(true);
                    } else {
                        backRowColorScope.data("kendoSwitch").check(false);
                    }

                    if (data.conditionalRegularExpressionFormatting) {
                        conditionalRegularExpressionFormatting.data("kendoSwitch").check(true);
                    } else {
                        conditionalRegularExpressionFormatting.data("kendoSwitch").check(false);
                    }

                    if (data.boldLineMatched) {
                        boldLineMatched.data("kendoSwitch").check(true);
                    } else {
                        boldLineMatched.data("kendoSwitch").check(false);
                    }

                    let ForeColorPicker = forePicker.data("kendoColorPicker");
                    forePicker.val(data.conditionalFormatForeColor);
                    let foreColor = forePicker.val();
                    foreColor = kendo.parseColor(foreColor);
                    ForeColorPicker.value(foreColor);

                    let BackColorPicker = backPicker.data("kendoColorPicker");
                    backPicker.val(data.conditionalFormatBackColor);
                    let backColor = backPicker.val();
                    backColor = kendo.parseColor(backColor);
                    BackColorPicker.value(backColor);

                    boldLineForePicker.val(data.boldLineFormatForeColor);

                    let BoldLineForeColorPicker = boldLineForePicker.data("kendoColorPicker");
                    let boldLineForeColor = boldLineForePicker.val();
                    boldLineForeColor = kendo.parseColor(boldLineForeColor);
                    BoldLineForeColorPicker.value(boldLineForeColor);

                    let BoldLineBackColorPicker = boldLineBackPicker.data("kendoColorPicker");
                    boldLineBackPicker.val(data.boldLineFormatBackColor);
                    let boldLineBackColor = boldLineBackPicker.val();
                    boldLineBackColor = kendo.parseColor(boldLineBackColor);
                    BoldLineBackColorPicker.value(boldLineBackColor);

                    if (data.drill) {
                        drill.data("kendoSwitch").check(true);
                    } else {
                        drill.data("kendoSwitch").check(false);
                    }

                    xPath.data("kendoDropDownList").value(data.xPath);

                    SetEnableHttpEndpoint();
                    SetEnableNotification();
                    SetConditionalRegularExpressionFormatting();
                    SetBoldLineMatched();

                    ReadyExisting(data);
                });
        }

    }
);

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
        xPath: xPath.val(),
        regularExpression: $("#RegularExpression").val(),
        drill: drill.prop("checked"),
        displayName: $("#DisplayName").val(),
        conditionalFormatForeColor: forePicker.val(),
        conditionalFormatBackColor: backPicker.val(),
        boldLineFormatForeColor: boldLineForePicker.val(),
        boldLineFormatBackColor: boldLineBackPicker.val(),
        conditionalRegularExpressionFormatting: conditionalRegularExpressionFormatting
            .prop("checked"),
        boldLineMatched: boldLineMatched.prop("checked"),
        foreRowColorScope: foreRowColorScope.prop("checked"),
        backRowColorScope: backRowColorScope.prop("checked")
    };
}

$(function () {
    addButton
        .click(function () {
            if (validator.validate()) {
                Create(endpoint, GetData(), 'id', parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

$(function () {
    updateButton
        .click(function () {
            if (validator.validate()) {
                Update(endpoint, GetData(), 'id', parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

//# sourceURL=CaseWorkflowXPath.js