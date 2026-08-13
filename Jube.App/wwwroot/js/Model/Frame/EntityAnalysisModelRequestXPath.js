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

var endpoint = "/api/EntityAnalysisModelRequestXPath";
var parentKeyName = "entityAnalysisModelId";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var divDefaultValueCheckbox = $("#DivDefaultValueCheckbox");
var defaultValueString = $("#DefaultValueString");
var defaultNumeric = $("#DefaultValueNumeric").kendoNumericTextBox();
var defaultCheckbox = $("#DefaultValueCheckbox").kendoSwitch();

var searchKey = $("#SearchKey").kendoSwitch({
    change: function () {
        ExpandCollapseSearchKey();
    }
});

var dataTypeId = $("#DataTypeId").kendoDropDownList(
    {
        change: setDefaultValueStyle
    }
);

var searchKeyCache = $("#SearchKeyCache").kendoSwitch({
    change: function () {
        ExpandCollapseSearchKey();
    }
});

var enableSuppression = $("#EnableSuppression").kendoSwitch();

var cache = $("#Cache").kendoSwitch();

var searchKeyCacheSample = $("#SearchKeyCacheSample").kendoSwitch();

var searchKeyCacheValue = $("#SearchKeyCacheValue").kendoNumericTextBox({
    format: "#",
    decimals: 0
});

var searchKeyCacheFetchLimit = $("#SearchKeyCacheFetchLimit").kendoNumericTextBox({
    format: "#",
    decimals: 0
});

var searchKeyCacheTtlValue = $("#SearchKeyCacheTtlValue").kendoNumericTextBox({
    format: "#",
    decimals: 0
});

var searchKeyTtlIntervalValue = $("#SearchKeyTtlIntervalValue").kendoNumericTextBox({
    format: "#",
    decimals: 0
});

var searchKeyFetchLimit = $("#SearchKeyFetchLimit").kendoNumericTextBox({
    format: "#",
    decimals: 0
});

function getDefaultValueStyle() {
    let value = 0;
    switch (dataTypeId.data("kendoDropDownList").value()) {
        case "1":
            value = defaultValueString.val();
            break;
        case "2":
        case "3":
        case "4":
        case "6":
        case "7":
            value = (defaultNumeric.data("kendoNumericTextBox").value() || 0).toString();
            break;
        case "5":
            if (defaultCheckbox.prop("checked")) {
                value = "1";
            } else {
                value = "0";
            }
            break;
    }
    return value;
}

function destroyNumeric() {
    const numericForDestruction = defaultNumeric.data("kendoNumericTextBox");
    const origin = numericForDestruction.element.show();

    origin.insertAfter(numericForDestruction.wrapper);

    numericForDestruction.destroy();
    numericForDestruction.wrapper.remove();
}

function setDefaultValueStyle() {
    switch (dataTypeId.data("kendoDropDownList").value()) {
        case "1":
            defaultValueString.show();
            defaultNumeric.data("kendoNumericTextBox").wrapper.hide();
            divDefaultValueCheckbox.hide();
            break;
        case "2":
            defaultValueString.hide();
            destroyNumeric();
            defaultNumeric.kendoNumericTextBox({
                format: "#"
            });
            defaultNumeric.data("kendoNumericTextBox").wrapper.show();
            divDefaultValueCheckbox.hide();
            break;
        case "3":
        case "6":
        case "7":
            defaultValueString.hide();
            destroyNumeric();
            defaultNumeric.kendoNumericTextBox();
            defaultNumeric.data("kendoNumericTextBox").wrapper.show();
            divDefaultValueCheckbox.hide();
            break;
        case "4":
            defaultValueString.hide();
            destroyNumeric();
            defaultNumeric.kendoNumericTextBox();
            defaultNumeric.data("kendoNumericTextBox").wrapper.show();
            divDefaultValueCheckbox.hide();
            break;
        case "5":
            defaultValueString.hide();
            defaultNumeric.data("kendoNumericTextBox").wrapper.hide();
            divDefaultValueCheckbox.show();
            break;
    }

    setEncryptionVisibility();
}

function setEncryptionVisibility() {
    const $encryptionRow = $("#EncryptionRow");
    if (dataTypeId.data("kendoDropDownList").value() === "1") {
        $encryptionRow.show();
    } else {
        $encryptionRow.hide();
    }
}

function ExpandCollapseSearchKey() {
    SetSearchKey();
    SetSearchKeyCache();
}

function SetSearchKey() {
    const $searchKeySubTable = $("#SearchKeySubTable");
    if ($('#SearchKey').prop('checked')) {
        $searchKeySubTable.show();
    } else {
        $searchKeySubTable.hide();
    }
    SetSearchKeyCache();
}

function SetSearchKeyCache() {
    const $searchKeyCacheSubTable = $("#SearchKeyCacheSubTable");
    if (searchKeyCache.prop('checked') && searchKeyCache.prop('checked')) {
        $searchKeyCacheSubTable.show();
    } else {
        $searchKeyCacheSubTable.hide();
    }
}

if (typeof GetSelectedChildID() === "undefined") {
    setDefaultValueStyle();
    ReadyNew();
    SetSearchKey();
    SetSearchKeyCache();
} else {
    $.get(endpoint + "/" + GetSelectedChildID(),
        function (data) {
            searchKeyCacheFetchLimit.data("kendoNumericTextBox").value(data.searchKeyCacheFetchLimit);
            searchKeyFetchLimit.data("kendoNumericTextBox").value(data.searchKeyFetchLimit);
            searchKeyTtlIntervalValue.data("kendoNumericTextBox").value(data.searchKeyTtlIntervalValue);

            $("input[name=SearchKeyTtlInterval][value=" +
                data.searchKeyTtlInterval +
                "]").prop('checked', true);

            $("input[name=SearchKeyCacheInterval][value=" +
                data.searchKeyCacheInterval +
                "]").prop('checked', true);

            searchKeyCacheValue.data("kendoNumericTextBox").value(data.searchKeyCacheValue);

            $("input[name=SearchKeyCacheTtlInterval][value=" +
                data.searchKeyCacheTtlInterval +
                "]").prop('checked', true);

            $("input[name=EncryptionId][value=" +
                data.encryptionId +
                "]").prop('checked', true);

            $("input[name=SearchInterval][value=" + data.searchInterval + "]")
                .prop('checked', true);

            searchKeyCacheTtlValue.data("kendoNumericTextBox").value(data.searchKeyCacheTtlValue);

            $("#XPath").val(data.xPath);

            if (data.searchKeyCache) {
                searchKeyCache.data("kendoSwitch").check(true);
            } else {
                searchKeyCache.data("kendoSwitch").check(false);
            }

            if (data.searchKeyCacheSample) {
                searchKeyCacheSample.data("kendoSwitch").check(true);
            } else {
                searchKeyCacheSample.data("kendoSwitch").check(false);
            }

            if (data.searchKey) {
                searchKey.data("kendoSwitch").check(true);
            } else {
                searchKey.data("kendoSwitch").check(false);
            }

            if (data.enableSuppression) {
                enableSuppression.data("kendoSwitch").check(true);
            } else {
                enableSuppression.data("kendoSwitch").check(false);
            }

            if (data.cache) {
                cache.data("kendoSwitch").check(true);
            } else {
                cache.data("kendoSwitch").check(false);
            }

            dataTypeId.data("kendoDropDownList").value(data.dataTypeId);
            $("#Version").html(data.version);
            $("#CreatedDate").html(new Date(data.createdDate).toLocaleString());
            $("#CreatedUser").html(data.createdUser);

            setDefaultValueStyle();

            switch (data.dataTypeId) {
                case 1:
                    defaultValueString.val(data.defaultValue);
                    break;
                case 2:
                case 3:
                case 4:
                case 6:
                case 7:
                    defaultNumeric.data("kendoNumericTextBox").value(data.defaultValue);
                    break;
                case 5:
                    if (data.defaultValue === "1") {
                        defaultCheckbox.data("kendoSwitch").check(true);
                    } else {
                        defaultCheckbox.data("kendoSwitch").check(false);
                    }
                    break;
            }

            ReadyExisting(data);
            SetSearchKey();
            SetSearchKeyCache();
        });
    $("#Add").hide();
}

$(function () {
    $("#Delete")
        .click(function () {
            if (confirm('Are you sure you want to delete?')) {
                Delete(endpoint, id);
            }
        });
});

function GetData() {
    return {
        entityAnalysisModelId: parentKey,
        name: $("#Name").val(),
        xPath: $("#XPath").val(),
        dataTypeId: dataTypeId.val(),
        searchKey: searchKey.prop("checked"),
        enableSuppression: enableSuppression.prop("checked"),
        searchKeyCache: searchKeyCache.prop("checked"),
        searchKeyCacheSample: searchKeyCacheSample.prop("checked"),
        searchKeyCacheFetchLimit: searchKeyCacheFetchLimit.val(),
        searchKeyCacheValue: searchKeyCacheValue.val(),
        searchKeyCacheInterval: $('input[name=SearchKeyCacheInterval]:checked')
            .val(),
        searchKeyCacheTtlValue: searchKeyCacheTtlValue.val(),
        searchKeyCacheTtlInterval: $(
            'input[name=SearchKeyCacheTtlInterval]:checked')
            .val(),
        responsePayload: $("#ResponsePayload").prop("checked"),
        payloadLocation: $('input[name=PayloadLocation]:checked').val(),
        searchKeyFetchLimit: searchKeyFetchLimit.val(),
        searchKeyTtlIntervalValue: searchKeyTtlIntervalValue.val(),
        searchKeyTtlInterval: $('input[name=SearchKeyTtlInterval]:checked').val(),
        defaultValue: getDefaultValueStyle(),
        cache: cache.prop("checked"),
        encryptionId: $('input[name=EncryptionId]:checked').val()
    };
}

$(function () {
    $("#Add")
        .click(function () {
            const validator = $("#form").kendoValidator().data("kendoValidator");
            if (validator.validate()) {
                Create(endpoint, GetData(), "id", parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

$(function () {
    $("#Update")
        .click(function () {
            const validator = $("#form").kendoValidator().data("kendoValidator");
            if (validator.validate()) {
                Update(endpoint, GetData(), "id", parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

//# sourceURL=EntityAnalysisModelRequestXPath.js