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

var endpoint = "/api/CaseWorkflowMacro";
var parentKeyName = "caseWorkflowId";
var childKeyName = "caseWorkflowMacroGuid";
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
    coder.getSession().setMode('ace/mode/javascript');
    coder.setReadOnly(false);

    // noinspection CssUnknownTarget
    $("#Images").kendoDropDownList({
        dataTextField: "name",
        dataValueField: "name",
        valueTemplate:
            '<span class="dropdown-image" style="background-image: url(\'../icons/#:data.name#\')"></span>',
        template:
            '<span class="dropdown-image" style="background-image: url(\'../icons/#:data.name#\')"></span>',
        dataSource: {
            transport: {
                read: {
                    dataType: "json",
                    url: "/api/icons"
                }
            }
        }
    });

    if (typeof id === "undefined") {
        SetEnableHttpEndpoint();
        SetEnableNotification();
        ReadyNew();
    } else {
        $.get(endpoint + "/" + id,
            function (data) {
                const editor = ace.edit("coder");
                editor.setValue(data.javascript);

                const dropdownlist = $("#Images").data("kendoDropDownList");
                dropdownlist.value(data.imageLocation);

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
                notificationBody.value(data.notificationBody);
                $("#NotificationDestination").val(data.notificationDestination);

                ReadyExisting(data);
            });
    }
});

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
    const dropdownlist = $("#Images").data("kendoDropDownList");
    return {
        javascript: editor.getValue(),
        imageLocation: dropdownlist.value(),
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

//# sourceURL=CasesWorkflowsMacro.js