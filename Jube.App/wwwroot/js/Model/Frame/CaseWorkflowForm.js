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

var endpoint = "/api/CaseWorkflowForm";
var parentKeyName = "caseWorkflowId";
var childKeyName = "caseWorkflowFormGuid";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var CoderHasChanged;
var CompileInProgress = 0;

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

var notificationBody = $("#NotificationBody").kendoTextArea();

$.getScript('/js/ace/ace.js', function () {
    ace.config.set('basePath', '/js/ace/');
    const coder = ace.edit('coder');
    coder.setTheme('ace/theme/sqlserver');
    coder.getSession().setMode('ace/mode/html');
    coder.getSession().on('change', CoderChanged);
    coder.setReadOnly(false);

    if (typeof id === "undefined") {
        SetEnableHttpEndpoint();
        SetEnableNotification();
        ReadyNew();
    } else {
        $.get(endpoint + "/" + id,
            function (data) {
                const editor = ace.edit("coder");
                editor.setValue(data.html);

                const $enableHttpEndpoint = $("#EnableHttpEndpoint");
                if (data.enableHttpEndpoint) {
                    $enableHttpEndpoint.data("kendoSwitch").check(true);
                } else {
                    $enableHttpEndpoint.data("kendoSwitch").check(false);
                }
                SetEnableHttpEndpoint();

                const $enableNotification = $("#EnableNotification");
                if (data.enableNotification) {
                    $enableNotification.data("kendoSwitch").check(true);
                } else {
                    $enableNotification.data("kendoSwitch").check(false);
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
                notificationBody.value(data.notificationBody);
                $("#NotificationDestination").val(data.notificationDestination);

                ReadyExisting(data);
            });
    }
});

setInterval(CoderChangedCompile, 1000);

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

function CoderChanged() {
    CoderHasChanged = 1;
}

function CoderChangedCompile() {
    if (CoderHasChanged === 1) {
        CoderHasChanged = 0;
        if (CompileInProgress !== 1) {
            CompileInProgress = 1;

            const editor = ace.edit("coder");
            $("#CompiledHTML").html(editor.getValue());

            CompileInProgress = 0;
        }
    }
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
    const editor = ace.edit("coder");
    return {
        html: editor.getValue(),
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

//# sourceURL=CasesWorkflowsForm.js