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

var endpoint = "/api/EntityAnalysisModelAbstractionCalculation";
var parentKeyName = "entityAnalysisModelId";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var abstractionLeft = $("#AbstractionLeft").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var abstractionRight = $("#AbstractionRight").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

function setCalculationType() {
    const $advanced = $("#Advanced");
    const $basic = $("#Basic");
    if ($("input[name='AbstractionCalculationTypeId']:checked").val() === '5') {
        $advanced.show();
        $basic.hide();
    } else {
        $advanced.hide();
        $basic.show();
    }
}

$("input[name='AbstractionCalculationTypeId']").click(function () {
    setCalculationType();
});

$.get("/api/TreeChildren/AbstractionRule?id=" + GetSelectedParentID(),
    function (data) {
        for (const value of data) {
            abstractionLeft.getKendoDropDownList().dataSource.add({
                "value": value.name,
                "text": value.name
            });

            abstractionRight.getKendoDropDownList().dataSource.add({
                "value": value.name,
                "text": value.name
            });
        }

        if (typeof id === "undefined") {
            initBuilderCoder(4, parentKey);
            setCalculationType();
            ReadyNew();
        } else {
            $.get(endpoint + "/" + id,
                function (data) {
                    abstractionLeft.data("kendoDropDownList")
                        .value(data.entityAnalysisModelAbstractionNameLeft);

                    abstractionRight.data("kendoDropDownList")
                        .value(data.entityAnalysisModelAbstractionNameRight);

                    $("input[name=AbstractionCalculationTypeId][value=" + data.abstractionCalculationTypeId + "]")
                        .prop('checked', true)

                    const builderCoderData = {
                        ruleTextCoder: data.functionScript,
                        ruleType: 2
                    };

                    initBuilderCoder(4, parentKey, builderCoderData);
                    setCalculationType();

                    ReadyExisting(data);
                });
        }
    }
);

function GetData() {
    const builderCoder = getBuilderCoder();
    return {
        entityAnalysisModelAbstractionNameLeft: abstractionLeft.data("kendoDropDownList").value(),
        entityAnalysisModelAbstractionNameRight: abstractionRight.data("kendoDropDownList").value(),
        abstractionCalculationTypeId: $('input[name=AbstractionCalculationTypeId]:checked').val(),
        functionScript: builderCoder.ruleTextCoder
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

//# sourceURL=EntityAnalysisModelAbstractionCalculation.js