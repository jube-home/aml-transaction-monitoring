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

const endpoint = "/api/EntityAnalysisModelReprocessingRule";
const endpointChildren = "/api/EntityAnalysisModelReprocessingRuleInstance";
const endpointValues = "/api/EntityAnalysisModelReprocessingRuleKvp";
const parentKeyName = "entityAnalysisModelId";
const validationFail = "There is invalid data in the form. Please check fields and correct.";

var values = [{
    "value": 0,
    "text": "Not Allocated"
}, {
    "value": 1,
    "text": "Allocated"
}, {
    "value": 2,
    "text": "Initial Count"
}, {
    "value": 3,
    "text": "Processing"
}, {
    "value": 4,
    "text": "Completed"
}];

var reprocessingValue = $("#ReprocessingValue").kendoNumericTextBox({
    format: "#",
    decimals: 0
});

var priority = $("#Priority").kendoNumericTextBox({
    format: "n3",
    decimals: 3,
    step: 0.001
});

var reprocessingSample = $("#ReprocessingSample").kendoSlider({
    increaseButtonTitle: "Right",
    decreaseButtonTitle: "Left",
    min: 0,
    max: 100,
    smallStep: 1,
    largeStep: 5
});

var reprocess = $("#Reprocess").kendoButton().click(function () {
    if (confirm('Are you sure you want to Reprocess?')) {
        $.ajax({
            url: endpointChildren + "/ByExistingUpdateUncompleted",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify({
                entityAnalysisModelReprocessingRuleId: id
            }),
            success: function () {
                GetInstances();
            }
        });
    }
});

var grid = $("#grid").kendoGrid({
    dataSource: {
        transport: {
            read: {
                url: endpointChildren + "/ByEntityAnalysisModelReprocessingId",
                data: function () {
                    return {
                        entityAnalysisModelReprocessingID: id
                    }
                },
                type: "GET",
                dataType: "json"
            },
            destroy: {
                url: function (e) {
                    return endpointChildren + "/" + e.id;
                },
                type: "DELETE"
            }
        },
        schema: {
            model: {
                id: "id",
                fields: {
                    statusId: {type: "number", editable: false},
                    createdDate: {type: "date", editable: false},
                    startedDate: {type: "date", editable: false},
                    referenceDate: {type: "date", editable: false},
                    availableCount: {type: "string", editable: false},
                    sampledCount: {type: "string", editable: false},
                    matchedCount: {type: "string", editable: false},
                    processedCount: {type: "string", editable: false},
                    completedDate: {type: "date", editable: false}
                }
            }
        }
    },
    height: 600,
    sortable: false,
    scrollable: true,
    autoBind: false,
    editable: "inline",
    columns: [
        {command: ["destroy"], title: "Delete", width: "150px"},
        {
            field: "statusId",
            values: values,
            title: "Status",
            width: 150
        },
        {
            field: "createdDate",
            title: "Created Date",
            width: 300
        },
        {
            field: "startedDate",
            title: "Started Date",
            width: 300
        },
        {
            field: "referenceDate",
            title: "Reference Date",
            width: 300
        },
        {
            field: "availableCount",
            title: "Available Count",
            width: 150
        },
        {
            field: "sampledCount",
            title: "Sampled Count",
            width: 150
        },
        {
            field: "matchedCount",
            title: "Matched Count",
            width: 150
        },
        {
            field: "processedCount",
            title: "Processed Count",
            width: 150
        },
        {
            field: "completedDate",
            title: "Completed Date",
            width: 300
        },
        {
            field: "errorCount",
            title: "Error Count",
            width: 150
        }
    ]
});

function GetInstances() {
    $('#GridWrap').show();
    reprocess.show();

    const grid = $('#grid').data('kendoGrid');
    grid.dataSource.read();
}

if (typeof id === "undefined") {
    $("#AddValue").hide();
    $("#FilesDiv").hide();
    $("#ListValuesDiv").hide();
    initBuilderCoder(2, parentKey);
    ReadyNew();
    reprocess.hide();
    $('#GridWrap').hide();
} else {
    $.get(endpoint + "/" + id,
        function (data) {
            const builderCoderData = {
                ruleTextBuilder: data.builderRuleScript,
                ruleTextCoder: data.coderRuleScript,
                ruleType: data.ruleScriptTypeId,
                ruleJsonBuilder: JSON.parse(data.json)
            };

            reprocessingValue.data("kendoNumericTextBox").value(data.reprocessingValue);

            priority.data("kendoNumericTextBox").value(data.priority);

            $("input[name=ReprocessingInterval][value=" +
                data.reprocessingInterval +
                "]").prop('checked', true);

            reprocessingSample.data("kendoSlider").value(data.reprocessingSample);

            initBuilderCoder(2, parentKey, builderCoderData);
            GetInstances();
            ReadyExisting(data);

            reprocess.show();
            $('#GridWrap').show();
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
    const builderCoder = getBuilderCoder();

    return {
        builderRuleScript: builderCoder.ruleTextBuilder,
        coderRuleScript: builderCoder.ruleTextCoder,
        ruleScriptTypeId: builderCoder.ruleType,
        json: builderCoder.ruleJsonBuilder,
        reprocessingSample: reprocessingSample.data("kendoSlider").value(),
        reprocessingValue: reprocessingValue.val(),
        reprocessingInterval: $('input[name=ReprocessingInterval]:checked').val(),
        priority: priority.val()
    };
}

function Callback() {
    reprocess.show();
    $('#GridWrap').show();
    return true;
}

$(function () {
    addButton
        .click(function () {
            if (validator.validate()) {
                Create(endpoint, GetData(), "id", parentKeyName, Callback());
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

//# sourceURL=EntityAnalysisModelReprocessingRule.js