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

var endpoint = "/api/CaseWorkflowStatus";
var parentKeyName = "caseWorkflowId";
var childKeyName = "caseWorkflowStatusGuid";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var priority = $("#Priority").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var enableHttpEndpoint = $("#EnableHttpEndpoint").kendoSwitch({
    change: function () {
        SetEnableHttpEndpoint();
    }
});

var enableNotification = $("#EnableNotification").kendoSwitch({
    change: function () {
        SetEnableNotification();
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

var notificationBody = $("#NotificationBody").kendoTextArea();

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

if (typeof id === "undefined") {
    SetEnableHttpEndpoint();
    SetEnableNotification();
    ReadyNew();
} else {
    $.get(endpoint + "/" + id,
        function (data) {
            const ForeColorPicker = forePicker.data("kendoColorPicker");
            const foreColor = kendo.parseColor(data.foreColor);
            ForeColorPicker.value(foreColor);

            const BackColorPicker = backPicker.data("kendoColorPicker");
            const backColor = kendo.parseColor(data.backColor);
            BackColorPicker.value(backColor);

            priority.data("kendoDropDownList").value(data.priority);

            if (data.enableHttpEndpoint) {
                enableHttpEndpoint.data("kendoSwitch").check(true);
            } else {
                enableHttpEndpoint.data("kendoSwitch").check(false);
            }
            SetEnableHttpEndpoint();

            if (data.enableNotification) {
                enableNotification.data("kendoSwitch").check(true);
            } else {
                enableNotification.data("kendoSwitch").check(false);
            }
            SetEnableNotification();

            $("#HttpEndpoint").val(data.httpEndpoint);

            if (data.httpEndpointTypeId === 1) {
                $("#POST").prop('checked', true);
            } else {
                $("#GET").prop('checked', true);
            }

            $("input[name=NotificationTypeId][value=" + data.notificationTypeId + "]")
                .prop('checked', true);
            $("#NotificationSubject").val(data.notificationSubject);

            const notificationBody = $("#NotificationBody").data("kendoTextArea");
            notificationBody.value(unescape(data.notificationBody));
            $("#NotificationDestination").val(data.notificationDestination);

            ReadyExisting(data);
        });
}
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
        foreColor: forePicker.val(),
        backColor: backPicker.val(),
        priority: priority.data("kendoDropDownList").value(),
        enableHttpEndpoint: enableHttpEndpoint.prop("checked"),
        enableNotification: enableNotification.prop("checked"),
        httpEndpoint: $("#HttpEndpoint").val(),
        httpEndpointTypeId: $('input[name=HttpEndpointTypeId]:checked').val(),
        notificationTypeId: $('input[name=NotificationTypeId]:checked').val(),
        notificationSubject: $("#NotificationSubject").val(),
        notificationBody: notificationBody.data("kendoTextArea").value(),
        notificationDestination: $("#NotificationDestination").val()
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

//# sourceURL=CaseWorkflowStatus.js