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
var currentCreateCaseWorkflowGuid;

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
    const schema = gridData.schema;
    const rows = gridData.rows;

    var model = GenerateModel(schema);
    var columns = GenerateColumns(schema);

    dateFields = Object.entries(schema)
        .filter(([, type]) => type === "date")
        .map(([name]) => name);

    if (dateFields.length > 0) {
        for (var i = 0; i < rows.length; i++) {
            for (var f = 0; f < dateFields.length; f++) {
                rows[i][dateFields[f]] = kendo.parseDate(rows[i][dateFields[f]]);
            }
        }
    }

    $("#grid").kendoGrid({
        dataSource: {
            data: rows,
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

function GenerateColumns(schema) {
    var columns = [];

    for (const [property] of Object.entries(schema)) {
        var column = {};
        column["width"] = "400px";
        column["field"] = property;
        column["title"] = FormatColumnName(property);
        if (property === 'ForeColor' || property === 'BackColor') {
            column["hidden"] = true;
        }
        columns.push(column);
    }

    return columns;
}

function GenerateModel(schema) {
    var model = {};
    model.id = "Id";
    var fields = {};

    for (const [name, type] of Object.entries(schema)) {
        fields[name] = {
            type: type,
            validation: {
                required: true
            }
        };
    }

    model.fields = fields;
    return model;
}

function DestroyGrid() {
    var $grid = $('#grid');
    var grid = $grid.data('kendoGrid');
    if (typeof grid !== "undefined") {
        grid.destroy();
        $grid.empty();
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
            var parentWorkflow = item.parentNode();
            var grandparentModel = parentWorkflow.parentNode();
            PopulateCreateCasePanel(parentWorkflow.guid, typeof grandparentModel !== "undefined" ? grandparentModel.guid : null);

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
            PopulateCreateCasePanel(item.guid, item.parentNode().guid);

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
        PopulateCreateCasePanel(null, null);
        return false;
    }
}

function PopulateCreateCasePanel(caseWorkflowGuid, entityAnalysisModelGuid) {
    currentCreateCaseWorkflowGuid = caseWorkflowGuid;

    const statusDropDownList = $("#CreateCaseWorkflowStatusGuid").data("kendoDropDownList");
    const keyDropDownList = $("#CreateCaseKey").data("kendoDropDownList");

    statusDropDownList.dataSource.data([]);
    statusDropDownList.text("");
    statusDropDownList.value("");

    keyDropDownList.dataSource.data([]);
    keyDropDownList.text("");
    keyDropDownList.value("");

    $("#CreateCaseKeyValue").val("");
    $("#CreateCaseErrorMessage").hide().empty();
    UpdateCreateCaseButtonState();

    const $createCase = $("#CreateCase");
    if (!caseWorkflowGuid || !entityAnalysisModelGuid) {
        $createCase.hide();
        return;
    }

    $createCase.show();

    $.get("../api/CaseWorkflowStatus/ByCasesWorkflowGuidActiveOnly/" + caseWorkflowGuid,
        function (data) {
            // Bind in a single assignment, in the exact order the controller returns
            // (Priority order), so the first entry is the highest priority status.
            statusDropDownList.dataSource.data($.map(data,
                function (value) {
                    return {
                        "value": value.guid,
                        "text": value.name
                    };
                }));

            if (data.length > 0) {
                statusDropDownList.select(0);
            }
        });

    $.get("../api/GetEntityAnalysisPotentialMultiPartStringNames/" + entityAnalysisModelGuid,
        function (data) {
            // Bind in a single assignment, in the exact (alphabetical) order the
            // controller returns.
            keyDropDownList.dataSource.data($.map(data,
                function (value) {
                    return {
                        "value": value,
                        "text": value
                    };
                }));
        });
}

function UpdateCreateCaseButtonState() {
    const caseKey = $("#CreateCaseKey").data("kendoDropDownList").value();
    const caseKeyValue = $("#CreateCaseKeyValue").val();

    $("#CreateCaseButton").data("kendoButton").enable(!!caseKey && !!caseKeyValue);
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

                            var entityAnalysisModelGuidForCreateCase = typeof caseWorkflowItem.parentNode() !== "undefined"
                                ? caseWorkflowItem.parentNode().guid
                                : null;
                            PopulateCreateCasePanel(caseWorkflowItem.guid, entityAnalysisModelGuidForCreateCase);

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

                                        let entityAnalysisModelGuidForFilterCreateCase = typeof caseWorkflowItem.parentNode() !== "undefined"
                                            ? caseWorkflowItem.parentNode().guid
                                            : null;
                                        PopulateCreateCasePanel(caseWorkflowItem.guid, entityAnalysisModelGuidForFilterCreateCase);

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

$(document).ready(function () {
    $("#CreateCaseWorkflowStatusGuid").kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value"
    });

    $("#CreateCaseKey").kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value",
        change: UpdateCreateCaseButtonState
    });

    const $createCaseButton = $("#CreateCaseButton");
    $createCaseButton.kendoButton();

    $("#CreateCaseKeyValue").on("input", UpdateCreateCaseButtonState);

    PopulateCreateCasePanel(null, null);

    $createCaseButton.click(function () {
        $("#CreateCaseErrorMessage").hide().empty();

        const data = {
            caseWorkflowGuid: currentCreateCaseWorkflowGuid,
            caseWorkflowStatusGuid: $("#CreateCaseWorkflowStatusGuid").data("kendoDropDownList").value(),
            caseKey: $("#CreateCaseKey").data("kendoDropDownList").value(),
            caseKeyValue: $("#CreateCaseKeyValue").val()
        };

        $.ajax({
            url: "../api/Case/CreateFromCaseKeyValue",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(data),
            error: function (jqXHR) {
                let message = "Processing failed.  Please contact Support to check logs for the source of the error.";

                if (jqXHR.status === 403) {
                    message = "You do not have permission to create a case for the selected Case Workflow Status.";
                } else if ((jqXHR.status === 404 || jqXHR.status === 400 || jqXHR.status === 409) && jqXHR.responseText) {
                    const responseObject = jQuery.parseJSON(jqXHR.responseText);

                    if (responseObject && responseObject.errors) {
                        const messages = [];
                        for (let key in responseObject.errors) {
                            messages.push(responseObject.errors[key].errorMessage);
                        }
                        message = messages.join("<br/>");
                    } else if (typeof responseObject === "string") {
                        message = responseObject;
                    }
                } else if (jqXHR.status === 404) {
                    message = "The selected Case Workflow could not be found.";
                }

                $("#CreateCaseErrorMessage").html(message).show();
            },
            success: function (data) {
                window.location.href = "/Case/Case?CaseId=" + data.id;
            }
        });
    });
});

//# sourceURL=CaseSearch.js