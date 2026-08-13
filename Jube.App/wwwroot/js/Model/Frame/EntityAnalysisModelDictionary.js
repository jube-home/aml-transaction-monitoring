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

const endpoint = "/api/EntityAnalysisModelDictionary";
const endpointValues = "/api/EntityAnalysisModelDictionaryKvp";
const parentKeyName = "entityAnalysisModelGuid";
const validationFail = "There is invalid data in the form. Please check fields and correct.";

var dataName = $("#DataName").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

function LoadFiles() {
    $("#Files").kendoUpload({
        async: {
            saveUrl: "/api/EntityAnalysisModelDictionaryCsvFileUpload",
            autoUpload: true,
            multiple: false
        },
        success: onSuccess,
        upload: function (e) {
            e.data = {entityAnalysisModelDictionaryId: id};
        }
    });
    $("#FilesDiv").show();
}

function onSuccess() {
    $("#listView").data("kendoListView").dataSource.data([]);
    LoadList();
}

function PopulateStrings() {
    $.get("/api/GetEntityAnalysisPotentialMultiPartStringNames" + "/" + parentKey,
        function (data) {
            for (const value of data) {
                dataName.getKendoDropDownList().dataSource.add({
                    "value": value,
                    "text": value
                });
            }
            Ready();
        }
    );
}

function LoadList() {
    const dataSource = new kendo.data.DataSource({
        transport: {
            read: {
                url: endpointValues + "/ByEntityAnalysisModelDictionaryId",
                type: "GET"
            },
            update: {
                url: endpointValues,
                dataType: "json",
                contentType: "application/json",
                type: "PUT"
            },
            destroy: {
                url: endpointValues,
                type: "DELETE"
            },
            create: {
                url: endpointValues,
                dataType: "json",
                contentType: "application/json",
                type: "POST"
            },
            parameterMap: function (options, operation) {
                if (operation === "read") {
                    return {entityAnalysisModelDictionaryId: id};
                } else if (operation === "update") {
                    return JSON.stringify({
                        entityAnalysisModelDictionaryId: id,
                        id:
                        options.models[0].id,
                        kvpKey: options.models[0].kvpKey,
                        kvpValue: options.models[0].kvpValue,
                        deleteExpiryDate: options.models[0].deleteExpiryDate
                    });
                } else if (operation === "destroy") {
                    return {
                        id:
                        options.models[0].id
                    };
                } else if (operation === "create") {
                    return JSON.stringify({
                        entityAnalysisModelDictionaryId: id,
                        kvpKey: options.models[0].kvpKey,
                        kvpValue: options.models[0].kvpValue,
                        deleteExpiryDate: options.models[0].deleteExpiryDate
                    });
                }
            }
        },
        batch: true,
        schema: {
            model: {
                id: "id",
                fields: {
                    id: {editable: false, nullable: true},
                    kvpKey: "kvpKey",
                    kvpValue: "kvpValue",
                    entityAnalysisModelDictionaryId: "entityAnalysisModelDictionaryId",
                    deleteExpiryDate: {type: "date", defaultValue: null}
                }
            }
        }
    });

    const listView = $("#listView").kendoListView({
        dataSource: dataSource,
        remove: function (e) {
            if (!confirm("Are you sure you want to delete?")) {
                e.preventDefault();
            }
        },
        template: kendo.template($("#template").html()),
        editTemplate: kendo.template($("#editTemplate").html())
    }).data("kendoListView");

    WireListViewValidation(listView, dataSource);

    $("#AddValue").kendoButton().click(function (e) {
        listView.add();
        e.preventDefault();
    });

    $("#ListValuesDiv").show();
}

PopulateStrings();

function Ready() {
    if (typeof id === "undefined") {
        $("#AddValue").hide();
        $("#FilesDiv").hide();
        $("#ListValuesDiv").hide();
        ReadyNew();
    } else {
        $.get(endpoint + "/" + id,
            function (data) {
                parentKey = data[parentKeyName];
                dataName.data("kendoDropDownList").value(data.dataName);
                ReadyExisting(data);
                LoadFiles();
                LoadList();
            });
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
    return {
        dataName: dataName.data("kendoDropDownList").value()
    };
}

$(function () {
    addButton
        .click(function () {
            if (validator.validate()) {
                Create(endpoint, GetData(), "id", parentKeyName);
                LoadFiles();
                LoadList();
                $("#AddValue").show();
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

//# sourceURL=EntityAnalysisModelDictionary.js