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

var endpoint = "/api/EntityAnalysisModelActivationRule";
var parentKeyName = "entityAnalysisModelId";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var enableSuppression = $("#EnableSuppression").kendoSwitch();
var visible = $("#Visible").kendoSwitch();
var enableReprocessing = $("#EnableReprocessing").kendoSwitch();

var enableCaseWorkflow = $("#EnableCaseWorkflow").kendoSwitch({
    change: function () {
        ExpandCollapseCases();
    }
});

var enableBypass = $("#EnableBypass").kendoSwitch({
    change: function () {
        ExpandCollapseCasesBypass();
    }
});

var enableResponseElevation = $("#EnableResponseElevation").kendoSwitch({
    change: function () {
        ExpandCollapseResponseElevation();
    }
});

var sendToActivationWatcher = $("#SendToActivationWatcher").kendoSwitch({
    change: function () {
        ExpandCollapseResponseElevationActivationWatcher();
    }
});

var enableNotification = $("#EnableNotification").kendoSwitch({
    change: function () {
        ExpandCollapseNotification();
    }
});

var enableTTLCounter = $("#EnableTTLCounter").kendoSwitch({
    change: function () {
        ExpandCollapseTTLCounter();
    }
});

var reviewStatusId = $("#ReviewStatusId").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var caseWorkflowGuid = $("#CaseWorkflowGuid").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value",
    change: function () {
        const value = this.value();
        PopulateCasesWorkflowsStatus(value);
    }
});

var caseWorkflowStatusGuid = $("#CaseWorkflowStatusGuid").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

const caseKey = $("#CaseKey").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var responseElevationKey = $("#ResponseElevationKey").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var entityAnalysisModelGuidTtlCounter = $("#EntityAnalysisModelGuidTtlCounter").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value",
    change: function () {
        const value = this.value();
        PopulateTTLCounters(value);
    }
});

var entityAnalysisModelTtlCounterGuid = $("#EntityAnalysisModelTtlCounterGuid").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var activationSample = $("#ActivationSample").kendoSlider({
    increaseButtonTitle: "Right",
    decreaseButtonTitle: "Left",
    min: 0,
    max: 1,
    smallStep: 0.01,
    largeStep: 0.05
});

var responseElevationContent = $("#ResponseElevationContent").kendoTextArea();

var notificationBody = $("#NotificationBody").kendoTextArea();

var responseElevationForeColor = $('#ResponseElevationForeColor').kendoColorPicker({
    value: "#000000",
    buttons: false
});

const responseElevationBackColor = $('#ResponseElevationBackColor').kendoColorPicker({
    value: "#ffffff",
    buttons: false
});

var responseElevation = $("#ResponseElevation").kendoNumericTextBox();

var priority = $("#Priority").kendoNumericTextBox({
    format: "n1",
    decimals: 1,
    step: 0.1,
    min: 0
});

var bypassSuspendSample = $("#BypassSuspendSample").kendoSlider({
    increaseButtonTitle: "Right",
    decreaseButtonTitle: "Left",
    min: 0,
    max: 1,
    smallStep: 0.01,
    largeStep: 0.05
});

var bypassSuspendValue = $("#BypassSuspendValue").kendoNumericTextBox({
    step: 1
});

function ExpandCollapseCases() {
    if ($('#EnableCaseWorkflow').prop('checked')) {
        $("#CaseWorkflowTable").show();
    } else {
        $("#CaseWorkflowTable").hide();
    }
    ExpandCollapseCasesBypass();
}

function ExpandCollapseCasesBypass() {
    if ($('#EnableBypass').prop('checked')) {
        $("#BypassSuspendTable").show();
    } else {
        $("#BypassSuspendTable").hide();
    }
}

function ExpandCollapseResponseElevation() {
    if ($('#EnableResponseElevation').prop('checked')) {
        $("#ResponseElevationTable").show();
    } else {
        $("#ResponseElevationTable").hide();
    }
    ExpandCollapseResponseElevationActivationWatcher();
}

function ExpandCollapseResponseElevationActivationWatcher() {
    if ($('#SendToActivationWatcher').prop('checked')) {
        $("#SendToActivationWatcherTable").show();
    } else {
        $("#SendToActivationWatcherTable").hide();
    }
}

function ExpandCollapseNotification() {
    if ($('#EnableNotification').prop('checked')) {
        $("#NotificationTable").show();
    } else {
        $("#NotificationTable").hide();
    }
}

function ExpandCollapseTTLCounter() {
    if ($('#EnableTTLCounter').prop('checked')) {
        $("#TTLCounterTable").show();
    } else {
        $("#TTLCounterTable").hide();
    }
}

function PopulateTTLCounters(entityAnalysisModelGuid, set) {
    $.ajax({
        url: "../api/EntityAnalysisModel",
        context: {entityAnalysisModelGuid: entityAnalysisModelGuid, entityAnalysisModelTtlCounterGuid: set},
        success: function (data) {
            for (const value of data) {

                let exists = false;
                for (let i = 0; i < entityAnalysisModelGuidTtlCounter.getKendoDropDownList().dataSource.data().length; i++) {
                    let dataItem = entityAnalysisModelGuidTtlCounter.getKendoDropDownList().dataSource.data()[i];
                    if (dataItem.value === value.guid) {
                        exists = true;
                        break;
                    }
                }

                if (!exists) {
                    entityAnalysisModelGuidTtlCounter.getKendoDropDownList().dataSource.add({
                        "value": value.guid,
                        "text": value.name
                    });
                }
            }

            let entityAnalysisModelGuidToSelectTtlCountersWith;
            if (typeof this.entityAnalysisModelGuid !== 'undefined') {
                entityAnalysisModelGuidTtlCounter.data("kendoDropDownList").value(entityAnalysisModelGuid);
                entityAnalysisModelGuidToSelectTtlCountersWith = entityAnalysisModelGuid;
            } else {
                entityAnalysisModelGuidTtlCounter.data("kendoDropDownList").select(0);
                entityAnalysisModelGuidToSelectTtlCountersWith = entityAnalysisModelGuidTtlCounter.data("kendoDropDownList").dataItem().value;
            }

            $.ajax({
                url: "../api/EntityAnalysisModelTtlCounter/ByEntityAnalysisModelGuid/" + entityAnalysisModelGuidToSelectTtlCountersWith,
                context: {entityAnalysisModelId: entityAnalysisModelGuid, entityAnalysisModelTtlCounterGuid: set},
                success: function (data) {
                    if (typeof data !== 'undefined') {
                        const TTLCounters = entityAnalysisModelTtlCounterGuid.getKendoDropDownList();
                        TTLCounters.dataSource.data([]);
                        TTLCounters.text("");
                        TTLCounters.value("");
                        for (const value of data) {
                            TTLCounters.dataSource.add({
                                "value": value.guid,
                                "text": value.name
                            });
                        }
                    }

                    if (typeof this.entityAnalysisModelTtlCounterGuid !== 'undefined') {
                        entityAnalysisModelTtlCounterGuid.data("kendoDropDownList").value(this.entityAnalysisModelTtlCounterGuid);
                    } else {
                        entityAnalysisModelTtlCounterGuid.data("kendoDropDownList").select(0);
                    }

                    if (entityAnalysisModelTtlCounterGuid.data("kendoDropDownList").dataSource.data().length === 0) {
                        ExpandCollapseTTLCounter();
                    }
                }
            });
        }
    });
}

function PopulateCasesWorkflowsStatus(value, set) {
    $.get("../api/CaseWorkflowStatus/ByCaseWorkflowGuid/" + value,
        function (data) {
            let CasesWorkflowStatus = caseWorkflowStatusGuid.getKendoDropDownList();
            if (typeof data !== 'undefined') {
                CasesWorkflowStatus.dataSource.data([]);
                CasesWorkflowStatus.text("");
                CasesWorkflowStatus.value("");
                for (const value of data) {
                    CasesWorkflowStatus.dataSource.add({
                        "value": value.guid,
                        "text": value.name
                    });
                }
            }

            if (typeof set !== 'undefined') {
                CasesWorkflowStatus.value(set);
            } else {
                CasesWorkflowStatus.select(0);
            }
        }
    );
}

function GetData() {
    const builderCoder = getBuilderCoder();

    let data = {
        entityAnalysisModelId: parentKey,
        builderRuleScript: builderCoder.ruleTextBuilder,
        coderRuleScript: builderCoder.ruleTextCoder,
        ruleScriptTypeId: builderCoder.ruleType,
        json: builderCoder.ruleJsonBuilder,
        responseElevation: responseElevation.data("kendoNumericTextBox").value(),
        enableCaseWorkflow: enableCaseWorkflow.prop("checked"),
        enableTtlCounter: enableTTLCounter.prop("checked"),
        responseElevationContent: responseElevationContent.data("kendoTextArea").value(),
        sendToActivationWatcher: sendToActivationWatcher.prop("checked"),
        responseElevationForeColor: $("#ResponseElevationForeColor").val(),
        responseElevationBackColor: $("#ResponseElevationBackColor").val(),
        activationSample: activationSample.data("kendoSlider").value(),
        responseElevationRedirect: $("#ResponseElevationRedirect").val(),
        reviewStatusId: reviewStatusId.data("kendoDropDownList").value(),
        enableNotification: enableNotification.prop("checked"),
        notificationTypeId: $('input[name=NotificationTypeId]:checked').val(),
        notificationDestination: $("#NotificationDestination").val(),
        notificationSubject: $("#NotificationSubject").val(),
        notificationBody: notificationBody.data("kendoTextArea").value(),
        enableResponseElevation: enableResponseElevation.prop("checked"),
        responseElevationKey: responseElevationKey.data("kendoDropDownList").value(),
        caseKey: caseKey.data("kendoDropDownList").value(),
        bypassSuspendInterval: $('input[name=BypassSuspendInterval]:checked').val(),
        bypassSuspendValue: bypassSuspendValue.data("kendoNumericTextBox").value(),
        bypassSuspendSample: bypassSuspendSample.data("kendoSlider").value(),
        visible: visible.prop("checked"),
        enableReprocessing: enableReprocessing.prop("checked"),
        enableSuppression: enableSuppression.prop("checked"),
        enableBypass: enableBypass.prop("checked"),
        priority: priority.data("kendoNumericTextBox").value()
    };

    if (data.enableCaseWorkflow) {
        if (caseWorkflowGuid.data("kendoDropDownList").dataSource.data().length > 0) {
            data["caseWorkflowGuid"] = caseWorkflowGuid.data("kendoDropDownList").value();
        }

        if (caseWorkflowGuid.data("kendoDropDownList").dataSource.data().length > 0) {
            data["caseWorkflowStatusGuid"] = caseWorkflowStatusGuid.data("kendoDropDownList").value();
        }
    }

    if (data.enableTtlCounter) {
        if (entityAnalysisModelTtlCounterGuid.data("kendoDropDownList").dataSource.data().length > 0) {
            data["entityAnalysisModelTtlCounterGuid"] = entityAnalysisModelTtlCounterGuid.data("kendoDropDownList").value();
        }

        if (entityAnalysisModelGuidTtlCounter.data("kendoDropDownList").dataSource.data().length > 0) {
            data["entityAnalysisModelGuidTtlCounter"] = entityAnalysisModelGuidTtlCounter.data("kendoDropDownList").value();
        }
    }

    return data;
}

responseElevationContent.css("background-color", $("#BackPicker").val());
responseElevationContent.css("color", $("#ForePicker").val());

function PopulateStrings() {
    $.get("/api/GetEntityAnalysisPotentialMultiPartStringNames" + "/" + parentKey,
        function (data) {
            for (const value of data) {
                caseKey.getKendoDropDownList().dataSource.add({
                    "value": value,
                    "text": value
                });

                responseElevationKey.getKendoDropDownList().dataSource.add({
                    "value": value,
                    "text": value
                });
            }
            Ready();
        }
    );
}

function PopulateCaseWorkflows() {
    $.get("/api/CaseWorkflow/ByEntityAnalysisModelId" + "/" + parentKey,
        function (data) {
            for (const value of data) {
                caseWorkflowGuid.getKendoDropDownList().dataSource.add({
                    "value": value.guid,
                    "text": value.name
                });
            }

            PopulateStrings();

            if (caseWorkflowGuid.data("kendoDropDownList").dataSource.data().length === 0) {
                enableCaseWorkflow.data("kendoSwitch").check(false);
                enableCaseWorkflow.data("kendoSwitch").enable(false);
                ExpandCollapseCases();
            }
        }
    );
}

PopulateCaseWorkflows();

function Ready() {
    if (typeof id === "undefined") {
        initBuilderCoder(5, parentKey);
        ReadyNew();
        PopulateTTLCounters();
        PopulateCasesWorkflowsStatus($("#CaseWorkflowGuid" +
            "").data("kendoDropDownList").value());
        ExpandCollapseTTLCounter();
        ExpandCollapseNotification();
        ExpandCollapseResponseElevationActivationWatcher();
        ExpandCollapseResponseElevation();
        ExpandCollapseCasesBypass();
        ExpandCollapseCases();
        OverrideSetTable();
    } else {
        $.get(endpoint + "/" + id,
            function (data) {
                parentKey = data[parentKeyName];

                const builderCoderData = {
                    ruleTextCoder: data.coderRuleScript,
                    ruleType: data.ruleScriptTypeId,
                    ruleTextBuilder: data.builderRuleScript,
                    ruleJsonBuilder: JSON.parse(data.json)
                };

                initBuilderCoder(5, parentKey, builderCoderData);

                priority.data("kendoNumericTextBox").value(data.priority);

                responseElevation.data("kendoNumericTextBox").value(data.responseElevation);

                if (data.enableCaseWorkflow) {
                    enableCaseWorkflow.data("kendoSwitch").check(true);
                } else {
                    enableCaseWorkflow.data("kendoSwitch").check(false);
                }

                PopulateTTLCounters(data.entityAnalysisModelGuidTtlCounter, data.entityAnalysisModelTtlCounterGuid);

                caseWorkflowGuid.data("kendoDropDownList").value(data.caseWorkflowGuid);
                PopulateCasesWorkflowsStatus(caseWorkflowGuid.data("kendoDropDownList").value(), data.caseWorkflowStatusGuid);

                if (data.enableTtlCounter) {
                    enableTTLCounter.data("kendoSwitch").check(true);
                } else {
                    enableTTLCounter.data("kendoSwitch").check(false);
                }

                if (data.sendToActivationWatcher) {
                    sendToActivationWatcher.data("kendoSwitch").check(true);
                } else {
                    sendToActivationWatcher.data("kendoSwitch").check(false);
                }

                caseWorkflowStatusGuid.data("kendoDropDownList").value(data.caseWorkflowStatusGuid);
                activationSample.data("kendoSlider").value(data.activationSample);
                reviewStatusId.data("kendoDropDownList").value(data.reviewStatusId);

                if (data.enableNotification) {
                    enableNotification.data("kendoSwitch").check(true);
                } else {
                    enableNotification.data("kendoSwitch").check(false);
                }

                if (data.enableResponseElevation) {
                    enableResponseElevation.data("kendoSwitch").check(true);
                } else {
                    enableResponseElevation.data("kendoSwitch").check(false);
                }

                bypassSuspendValue.data("kendoNumericTextBox").value(data.bypassSuspendValue);
                $("input[name=BypassSuspendInterval][value=" + data.bypassSuspendInterval + "]").prop('checked', true);

                bypassSuspendSample.data("kendoSlider").value(data.bypassSuspendSample);

                if (data.visible) {
                    visible.data("kendoSwitch").check(true);
                } else {
                    visible.data("kendoSwitch").check(false);
                }

                if (data.enableReprocessing) {
                    enableReprocessing.data("kendoSwitch").check(true);
                } else {
                    enableReprocessing.data("kendoSwitch").check(false);
                }

                if (data.enableSuppression) {
                    enableSuppression.data("kendoSwitch").check(true);
                } else {
                    enableSuppression.data("kendoSwitch").check(false);
                }

                if (data.enableBypass) {
                    enableBypass.data("kendoSwitch").check(true);
                } else {
                    enableBypass.data("kendoSwitch").check(false);
                }

                $("input[name=NotificationTypeId][value=" + data.notificationTypeId + "]").prop('checked', true);
                responseElevationContent.data("kendoTextArea").value(data.responseElevationContent);

                const ForeColorPicker = $("#ResponseElevationForeColor").data("kendoColorPicker");
                const foreColor = kendo.parseColor(data.responseElevationForeColor);
                ForeColorPicker.value(foreColor);

                const BackColorPicker = $("#ResponseElevationBackColor").data("kendoColorPicker");
                const backColor = kendo.parseColor(data.responseElevationBackColor);
                BackColorPicker.value(backColor);

                $("#ResponseElevationRedirect").val(data.responseElevationRedirect);
                $("#NotificationDestination").val(data.notificationDestination);
                $("#NotificationSubject").val(data.notificationSubject);
                notificationBody.data("kendoTextArea").value(data.notificationBody);
                caseKey.data("kendoDropDownList").value(data.caseKey);
                responseElevationKey.data("kendoDropDownList").value(data.responseElevationKey);

                const percent = data.evaluationCounter === 0
                    ? 0
                    : Math.round((data.activationCounter / data.evaluationCounter) * 100);

                $("#Counters").html(`${data.activationCounter} / ${data.evaluationCounter} (${percent}%)`);
                $("#LastCounters").html(new Date(data.activationCounterDate));

                ReadyExisting(data);

                ExpandCollapseTTLCounter();
                ExpandCollapseNotification();
                ExpandCollapseResponseElevationActivationWatcher();
                ExpandCollapseResponseElevation();
                ExpandCollapseCasesBypass();
                ExpandCollapseCases();
                OverrideSetTable();
            });
    }
}

function OverrideSetTable() {
    let rows = $('#TemplateTable tr').length;
    rows = rows - 6;

    if (typeof id !== "undefined") {
        $("#TemplateTable tr:gt(" + rows + ")").show();
    } else {
        $("#TemplateTable tr:gt(" + rows + ")").hide();
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

$(function () {
    addButton
        .click(function () {
            if (validator.validate() && validateBuilderCoder()) {
                Create(endpoint, GetData(), "id", parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

$(function () {
    updateButton
        .click(function () {
            if (validator.validate() && validateBuilderCoder()) {
                Update(endpoint, GetData(), "id", parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

//# sourceURL=EntityAnalysisModelActivationRule.js
