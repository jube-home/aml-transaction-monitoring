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

const endpoint = "/api/EntityAnalysisModelAbstractionRule";
const parentKeyName = "entityAnalysisModelId";
const validationFail = "There is invalid data in the form. Please check fields and correct.";

var searchFunctionKeyString = $("#SearchFunctionKeyString").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var search = $("#Search").kendoSwitch({
    change: function () {
        ExpandCollapseSearchKey();
    }
});

var offset = $("#Offset").kendoSwitch({
    change: function () {
        ExpandCollapseOffset();
    }
});

var searchFunctionTypeId = $("#SearchFunctionTypeId").kendoDropDownList({
    change: function () {
        SetAggregationParams();
    }
});

var searchKey = $("#SearchKey").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var searchFunctionKeyFloat = $("#SearchFunctionKeyFloat").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var searchValue = $("#SearchValue").kendoNumericTextBox({
    format: "#",
    decimals: 0,
    step: 1
});

var offsetValue = $("#OffsetValue").kendoNumericTextBox({
    format: "#",
    decimals: 0,
    step: 1
});

var searchFunctionKeyDate = $("#SearchFunctionKeyDate").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

function SetAggregationParams(value) {
    const FunctionType = $('#SearchFunctionTypeId');
    const FunctionFieldComboFloat = $('#SearchFunctionKeyFloat').kendoDropDownList();
    const FunctionFieldComboString = $('#SearchFunctionKeyString').kendoDropDownList();
    const FunctionFieldComboDate = $('#SearchFunctionKeyDate').kendoDropDownList();
    const FunctionFieldsTable = $("#FunctionFieldsTable");
    switch (FunctionType.val()) {
        case '1':
            FunctionFieldsTable.hide();
            break;
        case '2':
            FunctionFieldsTable.show();
            FunctionFieldComboString.data("kendoDropDownList").wrapper.show();
            FunctionFieldComboFloat.data("kendoDropDownList").wrapper.hide();
            FunctionFieldComboDate.data("kendoDropDownList").wrapper.hide();

            if (value) {
                FunctionFieldComboString.data("kendoDropDownList").value(value);
            }

            break;
        case '12':
            FunctionFieldsTable.show();
            FunctionFieldComboString.data("kendoDropDownList").wrapper.show();
            FunctionFieldComboFloat.data("kendoDropDownList").wrapper.hide();
            FunctionFieldComboDate.data("kendoDropDownList").wrapper.hide();

            if (value) {
                FunctionFieldComboString.data("kendoDropDownList").value(value);
            }

            break;
        case '13':
            FunctionFieldsTable.show();
            FunctionFieldComboString.data("kendoDropDownList").wrapper.hide();
            FunctionFieldComboFloat.data("kendoDropDownList").wrapper.show();
            FunctionFieldComboDate.data("kendoDropDownList").wrapper.hide();

            if (value) {
                FunctionFieldComboFloat.data("kendoDropDownList").value(value);
            }

            break;
        case '16':
            FunctionFieldsTable.show();
            FunctionFieldComboString.data("kendoDropDownList").wrapper.hide();
            FunctionFieldComboFloat.data("kendoDropDownList").wrapper.hide();
            FunctionFieldComboDate.data("kendoDropDownList").wrapper.show();

            if (value) {
                FunctionFieldComboDate.data("kendoDropDownList").value(value);
            }

            break;
        default:
            FunctionFieldsTable.show();
            FunctionFieldComboString.data("kendoDropDownList").wrapper.hide();
            FunctionFieldComboFloat.data("kendoDropDownList").wrapper.show();
            FunctionFieldComboDate.data("kendoDropDownList").wrapper.hide();

            if (value) {
                FunctionFieldComboFloat.data("kendoDropDownList").value(value);
            }

            break;
    }
}

function ExpandCollapseSearchKey() {
    const $searchTable = $("#SearchTable");
    if ($('#Search').prop('checked')) {
        $searchTable.show();
    } else {
        $searchTable.hide();
    }
}

function ExpandCollapseOffset() {
    const $offsetTable = $("#OffsetTable");
    if ($('#Offset').prop('checked')) {
        $offsetTable.show();
    } else {
        $offsetTable.hide();
    }
}

$.get("../api/GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery/" + parentKey,
    function (data) {
        for (const value of data) {
            switch (value.dataTypeId) {
                case 1:
                    searchFunctionKeyString.getKendoDropDownList().dataSource.add({
                        "value": value.name,
                        "text": value.name
                    });

                    if (value.searchKey) {
                        searchKey.getKendoDropDownList().dataSource.add({
                            "value": value.name,
                            "text": value.name
                        });
                    }
                    break;
                case 3:
                    searchFunctionKeyFloat.getKendoDropDownList().dataSource.add({
                        "value": value.name,
                        "text": value.name
                    });
                    break;
                case 4:
                    // Note: endpoint may not return dataTypeId 4 (date).
                    searchFunctionKeyDate.getKendoDropDownList().dataSource.add({
                        "value": value.name,
                        "text": value.name
                    });
                    break;
                default:
                    break;
            }
        }
        if (typeof id === "undefined") {
            initBuilderCoder(3, parentKey);
            ExpandCollapseOffset();
            SetAggregationParams();
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

                    initBuilderCoder(3, parentKey, builderCoderData);

                    search.data("kendoSwitch").check(data.search);

                    ExpandCollapseSearchKey();

                    searchKey.data("kendoDropDownList").value(data.searchKey);

                    searchValue.data("kendoNumericTextBox").value(data.searchValue);

                    searchFunctionTypeId.data("kendoDropDownList").value(data.searchFunctionTypeId);
                    SetAggregationParams(data.searchFunctionKey);

                    $("input[name=SearchInterval][value=" + data.searchInterval + "]")
                        .prop('checked', true);

                    if (data.offset) {
                        offset.data("kendoSwitch").check(true);
                        $("input[name=OffsetTypeId][value=" + data.offsetTypeId + "]").prop('checked', true);
                        offsetValue.data("kendoNumericTextBox").value(data.offsetValue);
                    } else {
                        offset.data("kendoSwitch").check(false);
                        ExpandCollapseOffset();
                    }
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
    const builderCoder = getBuilderCoder();

    let searchFunctionTypeIdValue = searchFunctionTypeId.data("kendoDropDownList").value();

    let searchFunctionKeyValue;
    if (searchFunctionTypeIdValue === '2' || searchFunctionTypeIdValue === '12') {
        searchFunctionKeyValue = searchFunctionKeyString.data("kendoDropDownList").value();
    } else if (searchFunctionTypeIdValue === '16') {
        searchFunctionKeyValue = searchFunctionKeyDate.data("kendoDropDownList").value();
    } else {
        searchFunctionKeyValue = searchFunctionKeyFloat.data("kendoDropDownList").value();
    }

    return {
        builderRuleScript: builderCoder.ruleTextBuilder,
        coderRuleScript: builderCoder.ruleTextCoder,
        ruleScriptTypeId: builderCoder.ruleType,
        json: builderCoder.ruleJsonBuilder,
        search: search.prop("checked"),
        searchKey: searchKey.data("kendoDropDownList").value(),
        searchValue: searchValue.val(),
        searchInterval: $('input[name=SearchInterval]:checked').val(),
        searchFunctionTypeId: searchFunctionTypeIdValue,
        searchFunctionKey: searchFunctionKeyValue,
        offset: offset.prop("checked"),
        offsetTypeId: $('input[name=OffsetTypeId]:checked').val(),
        offsetValue: offsetValue.val()
    };
}

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

//# sourceURL=EntityAnalysisModelAbstractionRule.js