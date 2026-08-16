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

var dateFields = [];
var Params;
var Visualisation;
var VisualisationRegistryGuid;
var ShowParams;

function KendoDataToJSONArray(kendoData) {
    const resArray = new Array(kendoData.length);
    const ownProps = Object.keys(kendoData);
    let i = ownProps.length;
    while (i--)
        resArray[i] = [ownProps[i], kendoData[ownProps[i]]];

    const dataArray = [resArray.length];

    let j;
    for (j = 0; j < resArray.length; j++) {
        dataArray[j] = resArray[j][1];
    }

    return JSON.stringify(dataArray);
}

function IsDisabledHeaderPage() {
    const field = 'DisableSiteMaster';
    const url = window.location.href.toLowerCase();
    if (url.indexOf('?' + field.toLowerCase() + '=') !== -1)
        return true;
    else if (url.indexOf('&' + field.toLowerCase() + '=') !== -1)
        return true;
    return false;
}

function generateStringifyTemplate(objectToStringify) {
    return JSON.stringify(objectToStringify);
}

function generateColumns(series) {
    const columns = [];
    for (let i = 0; i < series.length; i++) {
        const column = {};
        column["width"] = "300px;";
        column["field"] = series[i].name;
        if (series[i].dataType === 6) {
            column["template"] = '#=generateStringifyTemplate(' + series[i].name + ')#';
        }
        columns.push(column);
    }
    return columns;
}

function generateGrid(gridData, gridName, series, autoBind) {
    let gridDiv = $(gridName);
    let height;
    if (gridDiv.parent().children().length > 1) {
        height = gridDiv.parent().height() / 2
    } else {
        height = gridDiv.parent().height()
    }

    gridDiv.kendoGrid({
        dataSource: gridData,
        columns: generateColumns(series),
        toolbar: ["excel"],
        excel: {
            fileName: "Visualisation.xlsx",
            proxyURL: "https://proxy.jube.io",
            filterable: true,
            allPages: true
        },
        autoBind: autoBind,
        height: height
    });
}

function generateModel(gridData) {
    const model = {};
    model.id = "ID";
    const fields = {};
    for (let property in gridData) {
        if (Object.prototype.hasOwnProperty.call(gridData, property)) {
            const propType = typeof gridData[property];

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
                const parsedDate = kendo.parseDate(gridData[property]);
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

function onClick(e) {
    e.preventDefault();
    Run();
}

function Run() {
    $("#Datasources").empty();

    let valid;
    const paramsValues = [];
    if (ShowParams) {
        valid = $("#form").data("kendoValidator").validate();
        if (valid) {
            for (let i = 0, l = Params.length; i < l; i++) {
                const param = Params[i];
                const paramValue = {};
                let value;
                switch (param.dataTypeId) {
                    case 1:
                        // noinspection JSJQueryEfficiency
                        value = $("#P" + param.id).val();
                        break;
                    case 2:
                        // noinspection JSJQueryEfficiency
                        value =
                            $("#P" + param.id).data("kendoNumericTextBox").value();
                        break;
                    case 3:
                        // noinspection JSJQueryEfficiency
                        value =
                            $("#P" + param.id).data("kendoNumericTextBox").value();
                        break;
                    case 4:
                        // noinspection JSJQueryEfficiency
                        const dateControl = $("#P" + param.id).data("kendoDateTimePicker");
                        value = dateControl.value()?.toISOString() ?? null;
                        break;
                    case 5:
                        // noinspection JSJQueryEfficiency
                        value = !!$("#P" + param.id).is(":checked");
                        break;
                }

                paramValue["id"] = param.id;
                paramValue["value"] = value;
                paramsValues.push(paramValue);
            }
        }
    } else {
        valid = true;
        for (let i = 0, l = Params.length; i < l; i++) {
            const param = Params[i];
            const paramValue = {};
            paramValue["id"] = param.id;
            if (typeof values[param.name] !== 'undefined') {
                paramValue["value"] = values[param.name];
            } else {
                paramValue["value"] = param.defaultValue;
            }
            paramsValues.push(paramValue);
        }
    }

    if (valid) {
        const serviceRootDatasources =
            "../api/VisualisationRegistryDatasource/ByVisualisationRegistryIdActiveOnly/" + Visualisation.id;

        $.get(serviceRootDatasources,
            function (datasources) {
                const containers = [];

                for (let i = 0, l = datasources.length; i < l; i++) {
                    const datasourceTile = datasources[i];
                    const gridName = "Grid" + datasourceTile.id;
                    // noinspection JSJQueryEfficiency
                    const grid = $('#' + gridName).data('kendoGrid');
                    let divIdTile;

                    if (datasourceTile.includeGrid) {
                        if (typeof grid !== "undefined") {
                            grid.destroy();
                            $('#' + gridName).empty();
                        } else {
                            if (datasourceTile.includeDisplay) {
                                divIdTile = "<div id='Display" +
                                    datasourceTile.id +
                                    "' style='height:50%; width:100%'></div>";

                                divIdTile = divIdTile + "<div id='" + gridName + "' style='width:100%'></div>";
                            } else {
                                divIdTile = "<div id='" + gridName + "' style='width:100%'></div>";
                            }
                        }
                    } else {
                        if (datasourceTile.includeDisplay) {
                            divIdTile = "<div id='Display" +
                                datasourceTile.id +
                                "' style='height:100%; width:100%'></div>";
                        }
                    }

                    containers.push({
                        colSpan: datasourceTile.columnSpan,
                        rowSpan: datasourceTile.rowSpan,
                        header: {
                            text: datasourceTile.name
                        },
                        bodyTemplate: divIdTile
                    });

                }

                $("#Datasources").kendoTileLayout({
                    containers: containers,
                    columns: Visualisation.columns,
                    columnsWidth: Visualisation.columnWidth,
                    rowsHeight: Visualisation.rowHeight,

                    resize: function (e) {
                        kendo.resize(e.container, true);
                    }
                });

                for (let i = 0, l = datasources.length; i < l; i++) {
                    const datasource = datasources[i];

                    const serviceRootSeries =
                        "../api/VisualisationRegistryDatasourceSeries/ByVisualisationRegistryDatasourceId/" +
                        datasource.id;

                    $.ajax({
                        url: serviceRootSeries,
                        context: datasource,
                        type: "GET",
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        success: function (series) {
                            let schema = 'schema: {model:{fields:{';
                            let fields = '';
                            if (series.length > 0) {
                                let firstField = true;
                                for (let j = 0, m = series.length; j < m; j++) {
                                    const field = series[j];

                                    if (firstField) {
                                        firstField = false;
                                    } else {
                                        fields = fields + ',';
                                    }

                                    fields = fields + field.name + ': {type:"';

                                    switch (field.dataTypeId) {
                                        case 1:
                                            fields = fields + 'string';
                                            break;
                                        case 2:
                                            fields = fields + 'number';
                                            break;
                                        case 3:
                                            fields = fields + 'number';
                                            break;
                                        case 4:
                                            fields = fields + 'date';
                                            break;
                                        case 5:
                                            fields = fields + 'boolean';
                                            break;
                                        case 6:
                                            fields = fields + 'string';
                                            break;
                                    }

                                    fields = fields + '"}';
                                }
                            }
                            schema = schema + fields + '}}}';

                            let requestEndString = "";

                            if (this.includeGrid) {
                                requestEndString =
                                    ",requestEnd: function(e) {generateGrid(e.sender,'#Grid" +
                                    this.id +
                                    "',series,false);}";
                            }

                            let evalGetDatasourceData;

                            let evalDatasourceChartConstructor;

                            if (this.includeDisplay === false) {
                                evalGetDatasourceData = "var ds" +
                                    this.id +
                                    " = new kendo.data.DataSource({transport: {read: {dataType: 'json',contentType: 'application/json' ,type:'POST',data:paramsValues,url: '../api/GetByVisualisationRegistryDatasourceCommandExecutionQuery/" +
                                    this.id +
                                    "'},parameterMap: function(data) {return KendoDataToJSONArray(data);}}," +
                                    schema +
                                    "});";

                                eval(evalGetDatasourceData);
                                if (this.includeGrid) {
                                    eval(evalGetDatasourceData);

                                    const evalGenerateGrid = "generateGrid(ds" +
                                        this.id +
                                        ",'#Grid" +
                                        this.id +
                                        "',series,true);";

                                    eval(evalGenerateGrid);
                                }

                            } else {
                                switch (this.visualisationTypeId) {
                                    case 1:
                                        evalGetDatasourceData = "var ds" +
                                            this.id +
                                            " = new kendo.data.DataSource({transport: {read: {dataType: 'json', contentType: 'application/json', type:'POST',data:paramsValues,url: '../api/GetByVisualisationRegistryDatasourceCommandExecutionQuery/" +
                                            this.id +
                                            "'},parameterMap: function(data) {return KendoDataToJSONArray(data);}}," +
                                            schema +
                                            requestEndString +
                                            ", change: function(e){$('#Display" +
                                            this.id +
                                            "').data('kendoChart').refresh();}});";

                                        eval(evalGetDatasourceData);

                                        evalDatasourceChartConstructor = "$('#Display" +
                                            this.id +
                                            "').kendoChart(" +
                                            unescape(this.visualisationText) +
                                            ");";

                                        eval(evalDatasourceChartConstructor);

                                        const evalApplyDataSource = "$('#Display" +
                                            this.id +
                                            "').data('kendoChart').dataSource = ds" +
                                            this.id;

                                        eval(evalApplyDataSource);

                                        const evalReadDataSource = "$('#Display" +
                                            this.id +
                                            "').data('kendoChart').dataSource.read();";

                                        eval(evalReadDataSource);

                                        break;
                                    case 2:
                                        evalGetDatasourceData = "var ds" +
                                            this.id +
                                            " = new kendo.data.DataSource({transport: {read: {dataType: 'json',contentType: 'application/json', type:'POST',data:paramsValues,url: '../api/GetByVisualisationRegistryDatasourceCommandExecutionQuery/" +
                                            this.id +
                                            "'},parameterMap: function(data) {return KendoDataToJSONArray(data);}}" +
                                            requestEndString +
                                            "});";

                                        eval(evalGetDatasourceData);

                                        evalDatasourceChartConstructor = "$('#Display" +
                                            this.id +
                                            "').kendoMap(" +
                                            unescape(this.visualisationText) +
                                            ");";

                                        eval(evalDatasourceChartConstructor);

                                        const evalSetDataSource = "$('#Display" +
                                            this.id +
                                            "').data('kendoMap').layers[1].setDataSource(ds" +
                                            this.id +
                                            ");";

                                        eval(evalSetDataSource);

                                        break;
                                    case 3:
                                        $.ajax({
                                            type: "POST",
                                            url:
                                                "../api/GetByVisualisationRegistryDatasourceCommandExecutionQuery/" +
                                                this.id,
                                            data: kendo.stringify(paramsValues),
                                            dataType: 'json',
                                            contentType: 'application/json',
                                            context: {
                                                visualisationText: this.visualisationText,
                                                id: this.id,
                                                includeGrid: this.includeGrid
                                            },
                                            success: function (data) {
                                                let first = true;
                                                const html = unescape(this.visualisationText);
                                                const tokens = html.match(/\[@(.*?)]/g);
                                                for (let i = 0; i < data.length; i++) {
                                                    let finalHtml = html;
                                                    $.each(tokens,
                                                        function (j, tokenValue) {
                                                            const token = tokenValue.slice(2, -2);
                                                            finalHtml =
                                                                finalHtml.replace(tokens[j], data[i][token]);
                                                        });
                                                    if (first) {
                                                        first = false;
                                                    } else {
                                                        // noinspection JSJQueryEfficiency
                                                        $("#Display" + this.id).append('<div class="bottomPaddedGeneral"></div>');
                                                    }

                                                    // noinspection JSJQueryEfficiency
                                                    $("#Display" + this.id).append(finalHtml);
                                                }

                                                if (this.includeGrid) {
                                                    const evalGetDatasourceData = "var ds" +
                                                        this.id +
                                                        " = new kendo.data.DataSource({data: data," +
                                                        schema +
                                                        "});";
                                                    eval(evalGetDatasourceData);
                                                    const evalGenerateGrid = "generateGrid(ds" +
                                                        this.id +
                                                        ",'#Grid" +
                                                        this.id +
                                                        "',series,true);";
                                                    eval(evalGenerateGrid);
                                                }
                                            }
                                        });

                                        break;
                                }
                            }
                        }
                    });
                }
            });
    }
}

function getUrlVars() {
    const vars = [];
    let hash;
    const hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
    for (let i = 0; i < hashes.length; i++) {
        hash = hashes[i].split('=');
        vars.push(hash[0]);
        vars[hash[0]] = hash[1];
    }
    return vars;
}

function InitVisualisation() {
    $.get("../api/VisualisationRegistry/GetByGuidActiveOnly/" + VisualisationRegistryGuid,
        function (dataVisualisation) {
            Visualisation = dataVisualisation;
            const serviceRootParams = "../api/VisualisationRegistryParameter/ByVisualisationRegistryIdActiveOnly/" +
                dataVisualisation.id;

            $.get(serviceRootParams,
                function (dataParams) {
                    Params = dataParams;
                    let paramsDiv = $("#Params");
                    if (dataParams.length > 0 && ShowParams) {
                        paramsDiv.append("<table>");
                        const l = dataParams.length;

                        for (let i = 0; i < l; i++) {
                            const param = dataParams[i];
                            paramsDiv.append("<tr>");
                            paramsDiv
                                .append(
                                    "<td style='padding: 3px 3px 10px;'>" +
                                    param.name +
                                    "</td>");

                            let required = '';
                            if (param.required) {
                                required = 'required';
                            }

                            switch (param.dataTypeId) {
                                case 1:
                                    paramsDiv.append("<td><input id=P" +
                                        param.id +
                                        " name='" +
                                        param.name +
                                        "' type='text' style='width:300px;' class='k-textbox' " +
                                        required +
                                        " value='" +
                                        param.defaultValue +
                                        "'/> <span class='k-invalid-msg' data-for='" +
                                        param.name +
                                        "'></span></td>");
                                    break;
                                case 2:
                                    paramsDiv.append("<td><input id=P" +
                                        param.id +
                                        " name='" +
                                        param.name +
                                        "' style='width:300px;' " +
                                        required +
                                        " value='" +
                                        param.defaultValue +
                                        "' /> <span class='k-invalid-msg' data-for='" +
                                        param.name +
                                        "'></span></td>");

                                    // noinspection JSJQueryEfficiency
                                    $("#P" + param.id).kendoNumericTextBox(
                                        {
                                            min: 0,
                                            max: 32767,
                                            format: "#",
                                            decimals: 0
                                        }
                                    );
                                    break;
                                case 3:
                                    paramsDiv.append("<td><input id=P" +
                                        param.id +
                                        " name='" +
                                        param.name +
                                        "' style='width:300px;' " +
                                        required +
                                        " value='" +
                                        param.defaultValue +
                                        "'/> <span class='k-invalid-msg' data-for='" +
                                        param.name +
                                        "'></span></td>");

                                    // noinspection JSJQueryEfficiency
                                    $("#P" + param.id).kendoNumericTextBox();
                                    break;
                                case 4:
                                    paramsDiv.append("<td><input id='P" +
                                        param.id +
                                        "' name='" +
                                        param.name +
                                        "' style='width:300px;' " +
                                        required +
                                        "/> <span class='k-invalid-msg' data-for='" +
                                        param.name +
                                        "'></span></td>");

                                    const d = new Date();

                                    const newDate = kendo.date.addDays(d, param.defaultValue);

                                    // noinspection JSJQueryEfficiency
                                    $("#P" + param.id).kendoDateTimePicker({
                                        value: newDate,
                                        dateInput: true
                                    });
                                    break;
                                case 5:
                                    let checked = '';
                                    if (param.checked !== '1') {
                                        checked = 'checked';
                                    }

                                    paramsDiv.append("<td><input id='P" +
                                        param.id +
                                        "' name='" +
                                        param.name +
                                        "' type='checkbox' class='toggle' " +
                                        checked +
                                        " /></td>");

                                    // noinspection JSJQueryEfficiency
                                    $("#P" + param.id).kendoSwitch();

                                    break;
                            }

                            paramsDiv.append("</tr>");
                        }

                        paramsDiv.append("</table>");
                        paramsDiv.append("<div class='bottomPaddedGeneral topPaddedGeneral'><button id='ExecuteDatasources'>Run</button></div>");

                        $("#form").kendoValidator();
                    } else {
                        paramsDiv.hide();
                        Run();
                    }

                    $("#ExecuteDatasources").kendoButton(
                        {
                            click: onClick
                        });

                });
        }
    );
}

$(document).ready(function () {
    if (getUrlVars()["Guid"]) {
        ShowParams = true;
        VisualisationRegistryGuid = getUrlVars()["Guid"];
        InitVisualisation(VisualisationRegistryGuid, true);
    }
});