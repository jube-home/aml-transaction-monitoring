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

var endpoint = "/api/EntityAnalysisModelGatewayRule";
var parentKeyName = "entityAnalysisModelId";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var priority = $("#Priority").kendoNumericTextBox({
    format: "n3",
    decimals: 3,
    step: 0.001
});

var gatewaySample = $("#GatewaySample").kendoSlider({
    increaseButtonTitle: "Right",
    decreaseButtonTitle: "Left",
    min: 0,
    max: 1,
    smallStep: 0.01,
    largeStep: 0.05,
    value: 1,
    tickPlacement: "none"
});

var maxResponseElevation = $("#MaxResponseElevation").kendoNumericTextBox({
    format: "#",
    decimals: 0,
    step: 1
});

var resetButton = $("#Reset").kendoButton().hide();

if (typeof id === "undefined") {
    initBuilderCoder(2, GetSelectedParentID());
    ReadyNew();
} else {
    $.get(endpoint + "/" + id,
        function (data) {
            const builderCoderData = {
                ruleTextCoder: data.coderRuleScript,
                ruleType: data.ruleScriptTypeId,
                ruleTextBuilder: data.builderRuleScript,
                ruleJsonBuilder: JSON.parse(data.json)
            };

            initBuilderCoder(2, GetSelectedParentID(), builderCoderData);

            priority.data("kendoNumericTextBox").value(data.priority);
            maxResponseElevation.data("kendoNumericTextBox").value(data.maxResponseElevation);
            gatewaySample.data("kendoSlider").value(data.gatewaySample);

            const percent = data.evaluationCounter === 0
                ? 0
                : Math.round((data.activationCounter / data.evaluationCounter) * 100);

            $("#Counters").html(`${data.activationCounter} / ${data.evaluationCounter} (${percent}%)`);
            $("#LastCounters").html(new Date(data.activationCounterDate));
            resetButton.show();

            ReadyExisting(data);
        });
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


function GetData() {
    const builderCoder = getBuilderCoder();
    return {
        entityAnalysisModelId: parentKey,
        builderRuleScript: builderCoder.ruleTextBuilder,
        coderRuleScript: builderCoder.ruleTextCoder,
        ruleScriptTypeId: builderCoder.ruleType,
        json: builderCoder.ruleJsonBuilder,
        maxResponseElevation: maxResponseElevation.val(),
        gatewaySample: gatewaySample.data("kendoSlider").value(),
        priority: priority.val()
    };
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

$(function () {
    resetButton
        .click(function () {
            if (confirm('Are you sure you want to reset the counters?')) {
                $.ajax({
                    url: endpoint + "/" + id + "/Reset",
                    type: "POST",
                    success: function () {
                        $("#Counters").html("0 / 0 (0%)");
                        $("#LastCounters").html("");
                    },
                    error: function () {
                        $("#ErrorMessage").html(processingFailed);
                    }
                });
            }
        });
});

//# sourceURL=EntityAnalysisModelGatewayRule.js