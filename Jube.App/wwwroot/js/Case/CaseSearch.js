// noinspection ES6ConvertVarToLetConst,JSUnresolvedVariable,HtmlUnknownAttribute,JSObsoletePrivateAccessSyntax,JSUnusedLocalSymbols

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

var currentCaseWorkflowGuid;
var currentCaseWorkflowFilterGuid;
var currentCaseSessionGuid;
var dateFields = [];
var caseId;
var jsonChangedFromFilterDefault;

function OnChange(e) {
    var grid = e.sender;
    var currentDataItem = grid.dataItem(this.select());
    if (currentDataItem["Id"] > 0) {
        caseId = currentDataItem["Id"];
        $('#Fetch').show();
        $('#FetchSet').text('Selected Case Id = ' + caseId);
    }
}

function GenerateGrid(gridData) {
    var model = GenerateModel(gridData[0]);
    var columns = GenerateColumns(gridData[0]);

    if (dateFields.length > 0) {
        var parseFunction = function (response) {
            for (var i = 0; i < response.length; i++) {
                for (var fieldIndex = 0; fieldIndex < dateFields.length; fieldIndex++) {
                    var record = response[i];
                    record[dateFields[fieldIndex]] = kendo.parseDate(record[dateFields[fieldIndex]]);
                }
            }
            return response;
        };
    }

    $("#grid").kendoGrid({
        dataSource: {
            data: gridData,
            schema: {
                model: model
            }
        },
        columns: columns,
        groupable: true,
        toolbar: ["excel"],
        excel: {
            fileName: "Cases.xlsx",
            proxyURL: "https://proxy.jube.io",
            filterable: true,
            allPages: true
        },
        selectable: true,
        change: OnChange,
        dataBound: SetColor,
        height: 500
    });
}

function FormatColumnName(data) {
    return data;
}

function GenerateColumns(gridData) {
    var columns = [];
    for (var property in gridData) {
        if (Object.prototype.hasOwnProperty.call(gridData, property)) {
            var column = {};
            column["width"] = "400px;";
            column["field"] = property;
            column["title"] = FormatColumnName(property);
            if (property === 'ForeColor' || property === 'BackColor') {
                column["hidden"] = true;
            }
            columns.push(column);
        }
    }
    return columns;
}

function GenerateModel(gridData) {
    var model = {};
    model.id = "Id";
    var fields = {};
    for (var property in gridData) {
        if (Object.prototype.hasOwnProperty.call(gridData, property)) {
            var propType = typeof gridData[property];

            if (propType === "number") {
                fields[property] = {
                    type: "number",
                    validation: {
                        required: true
                    }
                };
            } else if (propType === "boolean") {
                fields[property] = {
                    type: "boolean",
                    validation: {
                        required: true
                    }
                };
            } else if (propType === "string") {
                var parsedDate = kendo.parseDate(gridData[property]);
                if (parsedDate) {
                    fields[property] = {
                        type: "date",
                        validation: {
                            required: true
                        }
                    };
                    dateFields.push(property);
                } else {
                    fields[property] = {
                        validation: {
                            required: true
                        }
                    };
                }
            } else {
                fields[property] = {
                    validation: {
                        required: true
                    }
                };
            }

        }
    }
    model.fields = fields;

    return model;
}

function DestroyGrid() {
    var grid = $('#grid').data('kendoGrid');
    if (typeof grid !== "undefined") {
        grid.destroy();
        $("#grid").empty();
    }
}

function ExecuteCasesInSession() {
    DestroyGrid();
    $.get("../api/SessionCaseSearchCompiledSql/ByGuid/" + currentCaseSessionGuid,
        function (data) {
            GenerateGrid(data);
        });
}

function SetColor() {
    var grid = $('#grid').data('kendoGrid');
    var rows = grid.tbody.children();
    for (var j = 0; j < rows.length; j++) {
        var row = $(rows[j]);
        var dataItem = grid.dataItem(row);
        var backColor = dataItem.get("BackColor");
        var foreColor = dataItem.get("ForeColor");

        row.css("background-color", backColor);
        row.css("color", foreColor);
    }
}

function OnSelect(e) {
    var kitems = $(e.node).add($(e.node).parentsUntil('.k-treeview', '.k-item'));

    var texts = $.map(kitems,
        function (kitem) {
            return $(kitem).find('>div span.k-in').text();
        });

    var treeview = $("#Tree").getKendoTreeView();
    var item = treeview.dataItem(e.node);

    if (typeof item.parentNode() !== "undefined") {
        if (typeof item.caseWorkflowId !== "undefined") {
            $.get("../api/CaseWorkflowFilter/ByGuid/" + item.guid,
                function (data) {
                    if (typeof data !== "undefined") {
                        const currentBuildJson = {
                            filterJson: JSON.parse(data.filterJson),
                            selectJson: JSON.parse(data.selectJson),

                        };

                        if (data.filterTokens !== "undefined") {
                            currentBuildJson["filterTokens"] = JSON.parse(data.filterTokens);
                        }

                        CompileSqlOnServer(data.filterJson, data.selectJson, null, item.parentNode().guid, item.guid, item.guid, true, true);
                        initCaseFilterBuilder(true, currentCaseWorkflowGuid, currentBuildJson);
                        jsonChangedFromFilterDefault = false;
                    }
                });
        } else {
            $.get("../api/SessionCaseSearchCompiledSql/ByLast/",
                function (data) {
                    if (!data.notFound) {
                        if (typeof data !== "undefined") {
                            const currentBuildJson = {
                                filterJson: JSON.parse(data.filterJson),
                                selectJson: JSON.parse(data.selectJson)
                            };

                            if (data.filterTokens !== "undefined") {
                                currentBuildJson["filterTokens"] = JSON.parse(data.filterTokens);
                            }

                            CompileSqlOnServer(data.filterJson, data.selectJson, data.filterTokens, data.caseWorkflowGuid, data.caseWorkflowFilterGuid, true, true);
                            initCaseFilterBuilder(true, currentCaseWorkflowGuid, currentBuildJson);
                            jsonChangedFromFilterDefault = true;

                            ShowButtons();
                        }
                    }
                });
        }
    } else {
        return false;
    }
}

function ShowButtons() {
    $("#Peek").show();
    $("#Skim").show();
}

function SetParentNodeInTree() {
    var tree = $('#Tree').data('kendoTreeView');
    let selected = tree.select();
    let item = tree.dataItem(selected);

    tree.findByUid(item.parentNode().uid);
    let selectItem = tree.findByUid((item.parentNode().uid));
    tree.select(selectItem);
}

function CompileSqlOnServer(filterJson, selectJson, filterTokens, caseWorkflowGuid, caseWorkflowFilterGuid, refreshGrid, ignoreChanges) {
    const newFilterBuilder = {
        filterJson: filterJson,
        selectJson: selectJson,
        filterTokens: filterTokens,
        caseWorkflowGuid: caseWorkflowGuid,
        caseWorkflowFilterGuid: caseWorkflowFilterGuid
    };

    currentCaseWorkflowGuid = caseWorkflowGuid;
    currentCaseWorkflowFilterGuid = caseWorkflowFilterGuid;

    if (!ignoreChanges) {
        if (!jsonChangedFromFilterDefault) {
            if (typeof initialFilterBuilder !== "undefined") {
                let filterBuilderChanged;
                if (initialFilterBuilder.filterJson !== newFilterBuilder.filterJson
                    || initialFilterBuilder.selectJson !== newFilterBuilder.selectJson) {
                    newFilterBuilder.caseWorkflowFilterGuid = null;
                    SetParentNodeInTree();
                    jsonChangedFromFilterDefault = true;
                }
            }
        }
    }

    $.ajax({
        url: "../api/SessionCaseSearchCompiledSql/",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify(newFilterBuilder),
        success: function (data) {
            if (typeof data !== "undefined") {
                currentCaseSessionGuid = data.guid;
                if (refreshGrid) {
                    ExecuteCasesInSession();
                    ShowButtons();
                }
            }
        }
    });
}

$(document).ready(function () {
    $("#Fetch").kendoButton({
        click: function (e) {
            window.location.href = '/Case/Case?CaseId=' + caseId;
        }
    }).hide();

    $("#Skim").kendoButton({
        click: function (e) {
            var builderResult = getCasesFilter();
            CompileSqlOnServer(builderResult.filterJson, builderResult.selectJson, builderResult.filterTokens, currentCaseWorkflowGuid, currentCaseWorkflowFilterGuid, false, false);
            window.location.href = '/Case/Case?SessionCaseSearchCompiledSqlControllerGuid=' + currentCaseSessionGuid;
        }
    }).hide();

    $("#Peek").kendoButton({
        click: function (e) {
            var builderResult = getCasesFilter();
            CompileSqlOnServer(builderResult.filterJson, builderResult.selectJson, builderResult.filterTokens, currentCaseWorkflowGuid, currentCaseWorkflowFilterGuid, true, false);
        }
    }).hide();

    const filter = {
        transport: {
            read: {
                url: '../api/CaseWorkflowFilter/ByCasesWorkflowGuidActiveOnly',
                dataType: "json"
            }
        },
        schema: {
            model: {
                id: "guid",
                hasChildren: false
            }
        }
    };

    const workflow = {
        transport: {
            read: {
                url: '../api/CaseWorkflow/ByEntityAnalysisModelGuidActiveOnly',
                dataType: "json"
            }
        },
        schema: {
            model: {
                id: "guid",
                hasChildren: true,
                children: filter
            }
        }
    };

    const model = new kendo.data.HierarchicalDataSource({
        transport: {
            read: {
                url: '../api/EntityAnalysisModel',
                dataType: "json"
            }
        },
        schema: {
            model: {
                id: 'guid',
                hasChildren: true,
                children: workflow
            }
        }
    });

    $.get("../api/SessionCaseSearchCompiledSql/ByLast/",
        function (data) {
            if (!data.notFound) {
                if (typeof data !== "undefined") {
                    currentCaseWorkflowGuid = data.caseWorkflowGuid;
                    currentCaseWorkflowFilterGuid = data.caseWorkflowFilterGuid;
                    currentCaseSessionGuid = data.guid;

                    const currentBuildJson = {
                        filterJson: JSON.parse(data.filterJson),
                        selectJson: JSON.parse(data.selectJson)
                    };

                    initCaseFilterBuilder(true, currentCaseWorkflowGuid, currentBuildJson);
                    ExecuteCasesInSession();
                    ShowButtons();
                }
            }

            var tree = $("#Tree").kendoTreeView({
                dataSource: model,
                dataTextField: "name",
                select: OnSelect,
                dataBound: function (e) {
                    var tree = $("#Tree").getKendoTreeView();
                    tree.expand(".k-item");

                    if (typeof e.node !== "undefined") {
                        var caseWorkflowItem = tree.dataItem(e.node);
                        if (caseWorkflowItem.guid === currentCaseWorkflowGuid
                            && (typeof currentCaseWorkflowFilterGuid === "undefined"
                                || currentCaseWorkflowFilterGuid == null
                                || currentCaseWorkflowFilterGuid === "00000000-0000-0000-0000-000000000000")) {
                            tree.findByUid(caseWorkflowItem.uid);
                            let selectItem = tree.findByUid(caseWorkflowItem.uid);
                            tree.select(selectItem);

                            jsonChangedFromFilterDefault = true;
                        } else {
                            if (caseWorkflowItem.hasChildren && (typeof currentCaseWorkflowFilterGuid !== "undefined"
                                && currentCaseWorkflowFilterGuid !== "00000000-0000-0000-0000-000000000000")) {
                                var caseWorkflowFilterItems = caseWorkflowItem.children.data();
                                for (var i = 0; i < caseWorkflowFilterItems.length; i++) {
                                    let caseWorkflowFilterItem = caseWorkflowFilterItems[i];
                                    if (caseWorkflowFilterItem.guid === currentCaseWorkflowFilterGuid) {
                                        tree.findByUid(caseWorkflowFilterItem.uid);
                                        let selectItem = tree.findByUid(caseWorkflowFilterItem.uid);
                                        tree.select(selectItem);

                                        jsonChangedFromFilterDefault = false;
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }
    );
});

//# sourceURL=CaseSearch.js