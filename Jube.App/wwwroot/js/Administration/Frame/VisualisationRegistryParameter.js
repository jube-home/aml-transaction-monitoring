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

var endpoint = "/api/VisualisationRegistryParameter";
var parentKeyName = "visualisationRegistryId";
var childKeyName = "visualisationRegistryParameterGuid";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var dataType = $("#DataType").kendoDropDownList(
    {
        change: setDefaultValueStyle
    }
);

var defaultValueNumeric = $("#DefaultValueNumeric").kendoNumericTextBox();

var defaultValueCheckbox = $("#DefaultValueCheckbox").kendoSwitch();

var required = $("#Required").kendoSwitch();

var defaultValueString = $("#DefaultValueString");

var divDefaultValueCheckbox = $("#DivDefaultValueCheckbox");

function setDefaultValueStyle() {
    switch (dataType.data("kendoDropDownList").value()) {
        case "1":
            defaultValueString.show();
            defaultValueNumeric.data("kendoNumericTextBox").wrapper.hide();
            divDefaultValueCheckbox.hide();
            break;
        case "2":
            defaultValueString.hide();
            destroyNumeric();
            defaultValueNumeric.kendoNumericTextBox({
                format: "#"
            });
            defaultValueNumeric.data("kendoNumericTextBox").wrapper.show();
            divDefaultValueCheckbox.hide();
            break;
        case "3":
            defaultValueString.hide();
            destroyNumeric();
            defaultValueNumeric.kendoNumericTextBox();
            defaultValueNumeric.data("kendoNumericTextBox").wrapper.show();
            divDefaultValueCheckbox.hide();
            break;
        case "4":
            defaultValueString.hide();
            destroyNumeric();
            defaultValueNumeric.kendoNumericTextBox();
            defaultValueNumeric.data("kendoNumericTextBox").wrapper.show();
            divDefaultValueCheckbox.hide();
            break;
        case "5":
            defaultValueString.hide();
            defaultValueNumeric.data("kendoNumericTextBox").wrapper.hide();
            divDefaultValueCheckbox.show();
            break;
    }
}

function getDefaultValueStyle() {
    let value = 0;
    switch (dataType.data("kendoDropDownList").value()) {
        case "1":
            value = defaultValueString.val();
            break;
        case "2":
        case "3":
        case "4":
            value = (defaultValueNumeric.data("kendoNumericTextBox").value() || 0).toString();
            break;
        case "5":
            if (defaultValueCheckbox.prop("checked")) {
                value = "1";
            } else {
                value = "0";
            }
            break;
    }
    return value;
}

function destroyNumeric() {
    const numeric = defaultValueNumeric.data("kendoNumericTextBox");
    const origin = numeric.element.show();

    origin.insertAfter(numeric.wrapper);

    numeric.destroy();
    numeric.wrapper.remove();
}

if (typeof id === "undefined") {
    setDefaultValueStyle();
    ReadyNew();
} else {
    $.get(endpoint + "/" + id,
        function (data) {
            if (data.required) {
                required.data("kendoSwitch").check(true);
            } else {
                required.data("kendoSwitch").check(false);
            }

            dataType.data("kendoDropDownList").value(data.dataTypeId);

            setDefaultValueStyle();

            switch (data.dataTypeId) {
                case 1:
                    defaultValueString.val(data.defaultValue);
                    break;
                case 2:
                case 3:
                case 4:
                    defaultValueNumeric.data("kendoNumericTextBox").value(data.defaultValue);
                    break;
                case 5:
                    if (data.defaultValue === "1") {
                        defaultValueCheckbox.data("kendoSwitch").check(true);
                    } else {
                        defaultValueCheckbox.data("kendoSwitch").check(false);
                    }
                    break;
            }

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

$(function () {
    addButton
        .click(function () {
            if (validator.validate()) {
                let data = {
                    required: required.prop("checked"),
                    dataTypeId: dataType.val(),
                    name: $("#Name").val(),
                    defaultValue: getDefaultValueStyle()
                };

                Create(endpoint, data, "id", parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

$(function () {
    updateButton
        .click(function () {
            if (validator.validate()) {
                let data = {
                    required: required.prop("checked"),
                    dataTypeId: dataType.val(),
                    name: $("#Name").val(),
                    defaultValue: getDefaultValueStyle()
                };

                Update(endpoint, data, "id", parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

//# sourceURL=VisualisationRegistryParameter.js