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

// noinspection JSUnresolvedVariable,ES6ConvertVarToLetConst

var endpoint = "/api/ExhaustiveSearchInstance";
var parentKeyName = "entityAnalysisModelId";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var dateFields = [];
var HasCreatedChildTables = 0;
var LastScore = -1;
var Params;
var GetEndpointParams = {};
var Guid;
var interval;

var anomalyProbability = $("#AnomalyProbability").kendoSlider({
    increaseButtonTitle: "Right",
    decreaseButtonTitle: "Left",
    min: 0,
    max: 1,
    value: 0.02,
    smallStep: 0.01,
    largeStep: 0.05
});

function generateGrid(gridData) {
    if (dateFields.length > 0) {
        // noinspection JSUnusedLocalSymbols
        const parseFunction = function (response) {
            for (let i = 0; i < response.length; i++) {
                for (let fieldIndex = 0; fieldIndex < dateFields.length; fieldIndex++) {
                    let record = response[i];
                    record[dateFields[fieldIndex]] = kendo.parseDate(record[dateFields[fieldIndex]]);
                }
            }
            return response;
        }
    }

    generateModel(gridData[0]);
    generateColumns(gridData[0]);
}

function generateColumns(gridData) {
    const columns = [];
    for (let property in gridData) {
        if (Object.prototype.hasOwnProperty.call(gridData, property)) {
            const column = {};
            column["width"] = "400px";
            column["field"] = property;
            columns.push(column);
        }
    }

    return columns;
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

function CreateModelPerformanceTables() {
    // noinspection HtmlUnknownAttribute
    $("#grid").kendoGrid({
        autoBind: false,
        dataSource: {
            transport: {
                read: {
                    url:
                        "/api/GetExhaustiveSearchInstancePromotedTrialInstanceQuery/" +
                        id,
                    dataType: "json"
                },
                schema: {
                    model: {
                        fields: {
                            id: {
                                type: "number"
                            },
                            active:
                                {
                                    type: "boolean"
                                },
                            createdDate: {
                                type: "date"
                            },
                            score: {
                                type: "number"
                            },
                            topologyComplexity: {
                                type: "number"
                            }
                        }
                    }
                }
            }
        },
        height: 500,
        detailInit: detailInit,
        dataBound: function () {
            $(".toggle").each(function () {
                const toggleSwitch = $(this);
                toggleSwitch.kendoSwitch({
                    change: function (e) {
                        $.ajax({
                            url: "/api/ExhaustiveSearchInstancePromotedTrialInstance",
                            type: "PUT",
                            contentType: "application/json; charset=utf-8",
                            dataType: "json",
                            data: JSON.stringify(
                                {
                                    active: e.checked,
                                    id: e.sender.element.attr(
                                        "id")
                                }
                            )
                        });
                    }
                });
            });
        },
        columns: [
            {
                template:
                    '<input id=#=id# type="checkbox" class="toggle" #= (active==true) ? checked="checked" : "" # />',
                title: "Active"
            },
            {
                field: "createdDate",
                title: "Created Date"
            },
            {
                field: "score",
                title: "Score"
            },
            {
                field: "topologyComplexity",
                title: "Topology Complexity"
            }
        ]
    });

    $("#statistics").kendoGrid({
        autoBind: false,
        dataSource: {
            transport: {
                read: {
                    url: "/api/GetExhaustiveSearchInstanceVariableQuery/" +
                        id,
                    dataType: "json"
                },
                schema: {
                    model: {
                        fields: {
                            id: {
                                type: "number"
                            },
                            CompletedDate: {
                                type: "date"
                            },
                            Score: {
                                type: "number"
                            },
                            field: {
                                type: "string"
                            }
                        }
                    }
                }
            }
        },
        height: 500,
        detailInit: detailInitStatistics,
        dataBound: function () {

            const grid = this;
            $(".histogram").each(function () {
                const chart = $(this);
                const tr = chart.closest('tr');
                const model = grid.dataItem(tr);
                chart.kendoChart({
                    legend: {
                        visible: false
                    },
                    dataSource: {
                        data: model.histogramValues
                    },
                    series: [
                        {
                            field: "frequency",
                            name: "Frequency"
                        }
                    ],
                    valueAxis: {
                        labels: {
                            format: "{0}"
                        }
                    },
                    tooltip: {
                        visible: true,
                        template: "Bin: ${category} Frequency: ${value}"
                    },
                    categoryAxis: {
                        field: "bin",
                        visible: false
                    }
                });
            });
        },
        columns: [
            {
                field: "name",
                title: "Name",
                width: "300px"
            },
            {
                template: '<div class="histogram" style="height:200px"></div>',
                width: 350
            },
            {
                field: "correlation",
                title: "Correlation",
                width: "110px"
            },
            {
                field: "mean",
                title: "Mean",
                width: "110px"
            },
            {
                field: "standardDeviation",
                title: "Standard Deviation",
                width: "110px"
            },
            {
                field: "maximum",
                title: "Maximum",
                width: "110px"
            },
            {
                field: "minimum",
                title: "Minimum",
                width: "110px"
            },
            {
                field: "kurtosis",
                title: "Kurtosis",
                width: "110px"
            },
            {
                field: "skewness",
                title: "Skewness",
                width: "110px"
            },
            {
                field: "normalisationType",
                title: "Normalisation Type",
                width: "110px"
            },
            {
                field: "distinctValues",
                title: "Distinct Values",
                width: "110px"
            },
            {
                field: "iqr",
                title: "IQR",
                width: "110px"
            }
        ]
    });

    function detailInit(e) {
        $("<div/>").appendTo(e.detailCell).kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        dataType: "json",
                        url:
                            "/api/GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery/" +
                            e.data.id
                    },
                    schema: {
                        model: {
                            fields: {
                                name: {
                                    type: "string"
                                },
                                variableMean: {
                                    type: "number"
                                },
                                variableStandardDeviation: {
                                    type: "number"
                                },
                                variableMaximum: {
                                    type: "number"
                                },
                                variableMinimum: {
                                    type: "number"
                                },
                                prescriptionMean: {
                                    type: "number"
                                },
                                prescriptionStandardDeviation: {
                                    type: "number"
                                },
                                prescriptionMaximum: {
                                    type: "number"
                                },
                                prescriptionMinimum: {
                                    type: "number"
                                },
                                sensitivity: {
                                    type: "number"
                                }
                            }
                        }
                    }
                }
            },
            columns: [
                {
                    field: "name",
                    title: "Name",
                    width: "200px"
                },
                {
                    field: "variableMean",
                    title: "Variable Mean",
                    width: "110px"
                },
                {
                    field: "variableStandardDeviation",
                    title: "Variable Standard Deviation",
                    width: "110px"
                },
                {
                    field: "variableMaximum",
                    title: "Variable Maximum",
                    width: "110px"
                },
                {
                    field: "variableMinimum",
                    title: "Variable Minimum",
                    width: "110px"
                },
                {
                    field: "prescriptionMean",
                    title: "Prescription Mean",
                    width: "110px"
                },
                {
                    field: "prescriptionStandardDeviation",
                    title: "Prescription Standard Deviation",
                    width: "110px"
                },
                {
                    field: "prescriptionMaximum",
                    title: "Prescription Maximum",
                    width: "110px"
                },
                {
                    field: "prescriptionMinimum",
                    title: "Prescription Minimum",
                    width: "110px"
                },
                {
                    field: "sensitivity",
                    title: "Sensitivity",
                    width: "110px"
                }
            ]
        });
    }

    function setColor() {
        const rows = this.tbody.children();
        for (let j = 0; j < rows.length; j++) {
            const row = $(rows[j]);
            const dataItem = this.dataItem(row);
            const correlation = dataItem.get("correlation");

            let foreColor;
            if (correlation > 0.9) {
                foreColor = '#ff0000';
            } else if (correlation > 0.7) {
                foreColor = '#FFA500';
            } else {
                foreColor = '#008000';
            }

            row.css("color", foreColor);
        }
    }

    function detailInitStatistics(e) {
        $("<div/>").appendTo(e.detailCell).kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        dataType: "json",
                        url:
                            "/api/GetExhaustiveSearchInstanceTrialInstanceVariableVarianceQuery/" +
                            e.data.id
                    },
                    schema: {
                        model: {
                            fields: {
                                name: {
                                    type: "string"
                                },
                                correlation: {
                                    type: "number"
                                },
                                correlationAbsRank: {
                                    type: "number"
                                }
                            }
                        }
                    }
                }
            },
            dataBound: setColor,
            columns: [
                {
                    field: "name",
                    title: "Name",
                    width: "200px"
                },
                {
                    field: "correlation",
                    title: "Correlation",
                    width: "110px"
                }
            ]
        });
    }

    $("#chart").kendoChart({
        autoBind: false,
        dataSource: {
            transport: {
                read: {
                    url:
                        "/api/GetExhaustiveSearchInstancePromotedTrialInstanceLearningCurveQuery/" +
                        id,
                    dataType: "json"
                },
                schema: {
                    model: {
                        fields: {
                            createdDate: {
                                type: "date"
                            },
                            score: {
                                type: "number"
                            }
                        }
                    }
                }
            }
        },
        title: {
            text: "Learning Curve"
        },
        legend: {
            visible: false
        },
        chartArea: {
            height: 500
        },
        seriesDefaults: {
            type: "line",
            style: "smooth",
            labels: {
                visible: true,
                format: "{0}%",
                background: "transparent"
            }
        },
        series: [
            {
                type: "line",
                field: "score",
                aggregate: "max",
                categoryField: "createdDate"
            }
        ],
        valueAxis: {
            labels: {
                format: "{0}"
            },
            line: {
                visible: false
            }
        },
        categoryAxis: {
            majorGridLines: {
                visible: false
            },
            baseUnit: "fit",
            visible: false
        }
    });

    $("#tabstrip").kendoTabStrip({
        animation: {
            open: {
                effects: "fadeIn"
            }
        }
    });
}

function RefreshModelSummary(force) {
    $.get("../api/ExhaustiveSearchInstance/" + id,
        function (data) {
            LoadModelSummery(data, force);
        });
}

function LoadModelSummery(data, force) {
    $('#Refresh').show();
    let stop = $('#Stop').show();
    let currentStatus = $('#CurrentStatus');
    switch (data.statusId) {
        case 0:
            currentStatus.text('Awaiting Server');
            break;
        case 1:
            currentStatus.text('Query for base case data');
            break;
        case 2:
            currentStatus.text('Calculating Statistics for base case');
            break;
        case 3:
            currentStatus.text('Normalising Data for unsupervised learning');
            break;
        case 4:
            currentStatus.text('Unsupervised training');
            break;
        case 5:
            currentStatus.text('Unsupervised training recall');
            break;
        case 6:
            currentStatus.text('Unsupervised training statistics creation');
            break;
        case 7:
            currentStatus.text('Query for class filter data');
            break;
        case 8:
            currentStatus.text('Statistics for class filter data');
            break;
        case 9:
            currentStatus.text('Shuffling data');
            break;
        case 10:
            currentStatus.text('Normalising data');
            break;
        case 11:
            currentStatus.text('Over Sampling \ Symmetric Sampling');
            break;
        case 12:
            currentStatus.text('Storing final data.');
            break;
        case 13:
            currentStatus.text('Correlation analysis');
            break;
        case 14:
            currentStatus.text('Multicollinearity analysis');
            break;
        case 15:
            currentStatus.text('Supervised Learning');
            break;
        case 16:
            currentStatus.text('Finished');
            break;
        case 17:
            currentStatus.text('Finished no class available');
            break;
        case 18:
            currentStatus.text('Stopping');
            stop.data("kendoButton").enable(false);
            break;
        case 19:
            currentStatus.text('Stopped');
            stop.data("kendoButton").enable(false);
            break;
        default:
            currentStatus.text('Stopped for reasons unexpected');
    }

    Guid = data.guid;

    $('#CreatedUser').text(data.createdUser);
    $('#CreatedDate').text(new Date(data.createdDate).toLocaleString());
    $('#CompletedDate').text(new Date(data.completedDate).toLocaleString());
    $('#UpdatedDate').text(new Date(data.updatedDate).toLocaleString());
    $('#Models').text(data.models);
    $('#TopologyComplexity').text(data.topologyComplexity);
    $('#ModelsSinceBest').text(data.modelsSinceBest);
    $('#Score').text(data.score);

    if (data.score > LastScore || force) {
        LastScore = data.score;

        const $kpi = $('#KPI');
        const $tabstrip = $('#tabstrip');
        if (data.statusId < 14) {
            $kpi.hide();
            $tabstrip.hide();
        } else {
            $kpi.show();
            $tabstrip.show();
            $("#RightChart").kendoChart({
                title: {
                    text: "Predicted vs. Actual"
                },
                dataSource: {
                    transport: {
                        read: {
                            url:
                                "api/GetExhaustiveSearchInstancePromotedTrialInstancePredictedActual/" +
                                id,
                            dataType: "json"
                        }
                    }
                },
                legend: {
                    visible: false
                },
                seriesDefaults: {
                    type: "scatter"
                },
                series: [
                    {
                        xField: "predicted",
                        yField: "actual"
                    }
                ],
                xAxis: {
                    labels: {
                        format: "{0}"
                    },
                    title: {
                        text: "Predicted"
                    }
                },
                yAxis: {
                    labels: {
                        format: "{0}"
                    },
                    title: {
                        text: "Actual"
                    }
                }
            });

            $("#LeftChart").kendoChart({
                autoBind: true,
                dataSource: {
                    transport: {
                        read: {
                            url:
                                "/api/GetExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram/" +
                                id,
                            dataType: "json"
                        },
                        schema: {
                            model: {
                                fields: {
                                    bin: {
                                        type: "number"
                                    },
                                    frequency: {
                                        type: "number"
                                    }
                                }
                            }
                        }
                    }
                },
                title: {
                    text: "Errors"
                },
                legend: {
                    visible: false
                },
                chartArea: {
                    height: 500
                },
                series: [
                    {
                        field: "frequency",
                        categoryField: "bin"
                    }
                ],
                valueAxis: {
                    labels: {
                        format: "{0}"
                    },
                    line: {
                        visible: false
                    }
                },
                categoryAxis: {
                    majorGridLines: {
                        visible: false
                    },
                    baseUnit: "fit"
                }
            });
        }

        $("#Confusion").show();
        $("#ROCChart").kendoChart({
            title: {
                text: "ROC"
            },
            dataSource: {
                transport: {
                    read: {
                        url:
                            "api/GetExhaustiveSearchInstancePromotedTrialInstanceRoc/" +
                            id,
                        dataType: "json"
                    }
                }
            },
            legend: {
                visible: false
            },
            seriesDefaults: {
                type: "scatterLine"
            },
            series: [
                {
                    xField: "fpr",
                    yField: "tpr"
                }
            ],
            xAxis: {
                labels: {
                    format: "{0}"
                },
                title: {
                    text: "False Positive Rate"
                }
            },
            yAxis: {
                labels: {
                    format: "{0}"
                },
                title: {
                    text: "True Positive Rate"
                }
            }
        });

        $.get("api/GetExhaustiveSearchInstancePromotedTrialInstanceConfusion/" + id,
            function (data) {
                $("#TP").html(data.truePositive);
                $("#FP").html(data.falsePositive);
                $("#FN").html(data.falseNegative);
                $("#TN").html(data.trueNegative);

                $("#TPRowTotal").html(data.truePositiveRowTotal);
                $("#TPColumnTotal").html(data.truePositiveColumnTotal);
                $("#TPTableTotal").html(data.truePositiveTableTotal);

                $("#FPRowTotal").html(data.falsePositiveRowTotal);
                $("#FPColumnTotal").html(data.falsePositiveColumnTotal);
                $("#FPTableTotal").html(data.falsePositiveTableTotal);

                $("#FNRowTotal").html(data.falseNegativeRowTotal);
                $("#FNColumnTotal").html(data.falseNegativeColumnTotal);
                $("#FNTableTotal").html(data.falseNegativeTableTotal);

                $("#TNRowTotal").html(data.trueNegativeRowTotal);
                $("#TNColumnTotal").html(data.trueNegativeColumnTotal);
                $("#TNTableTotal").html(data.trueNegativeTableTotal);

                $("#TableTotal").html(data.tableTotal);

                $("#PositiveRow").html(data.positiveRowTotal);
                $("#PositiveColumn").html(data.positiveColumnTotal);

                $("#NegativeRow").html(data.negativeRowTotal);
                $("#NegativeColumn").html(data.negativeColumnTotal);

                $("#PositiveRowTableTotal").html(data.positiveRowTableTotal);
                $("#PositiveColumnTableTotal").html(data.positiveColumnTableTotal);

                $("#NegativeRowTableTotal").html(data.negativeRowTableTotal);
                $("#NegativeColumnTableTotal").html(data.negativeColumnTableTotal);
            });

        BuildSliders(id, Guid);

        if (HasCreatedChildTables === 0) {
            CreateModelPerformanceTables();
            HasCreatedChildTables = 1;
        }

        $("#grid").data("kendoGrid").dataSource.read();

        let statistics = $("#statistics");
        let records = statistics.data("kendoGrid").dataSource.view().length;
        if (records === 0) {
            statistics.data("kendoGrid").dataSource.read();
        }

        $("#chart").data("kendoChart").dataSource.read();
    }
}

function MergeArray(name, value) {
    Params[name] = value;
}

function sliderOnChange(e) {
    const handle = e.sender,
        handleElement = handle.element,
        handleElementId = handleElement.attr('name');

    MergeArray(handleElementId, e.value);

    GetScore();
}

function GetScore() {
    jQuery.ajax({
        type: 'POST',
        contentType: "application/json; charset=utf-8",
        url: "../api/Invoke/ExhaustiveSearchInstance/" + Guid,
        data: JSON.stringify(Params),
        dataType: "json",
        success: SetScore
    });
}

function SetScore(data) {
    $("#Simulation").text('Score: ' + data);
}

function sliderOnSlide(e) {
    const handle = e.sender,
        handleElement = handle.element,
        handleElementId = handleElement.attr('name');

    MergeArray(handleElementId, e.value);
}

function BuildSliders() {
    Params = {};
    $("#SlidersPlaceholder tr").remove();

    $.get('api/GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery/' + id,
        function (data) {
            if (data.length > 0) {
                $.each(data, function (i, value) {
                    if (!data.emptyRange) {
                        const tb = $('<input>', {
                            type: 'text',
                            value: value.mean,
                            id: 'balSlider_' + value.id,
                            name: value.name
                        });

                        const $tr = $('<tr>').append(
                            $("<td style='vertical-align:top;width: 0;'>").text(value.name),
                            $("<td style='width:80%;height:100px;vertical-align:top;'>").append(tb)
                        );

                        $("#SlidersPlaceholder").append($tr);

                        $("#balSlider_" + value.id).kendoSlider({
                            increaseButtonTitle: "Right",
                            decreaseButtonTitle: "Left",
                            min: value.minimum,
                            max: value.maximum,
                            change: sliderOnChange,
                            slide: sliderOnSlide,
                            showButtons: false
                        }).data("kendoSlider");

                        Params[value.name] = tb.val();
                    }
                });

                $("#SlidersPlaceholder").resize();

                GetScore();
            }
        });
}

if (typeof id === "undefined") {
    //clearInterval(interval);
    $('#Refresh').hide();
    $('#Stop').hide();
    ReadyNew();

    initExhaustiveFilterBuilder(parentKey);
    ExpandCollapseAnomaly();
    ExpandCollapseFilter();

} else {
    $.get(endpoint + "/" + id,
        function (data) {
            ReadyExisting(data);

            const builderCoderData = {
                filterTokens: data.filterTokens,
                filterJson: JSON.parse(data.filterJson)
            };

            const $anomaly = $("#Anomaly");
            if (data.anomaly) {
                $anomaly.data("kendoSwitch").check(true);
            } else {
                $anomaly.data("kendoSwitch").check(false);
            }

            anomalyProbability.data("kendoSlider").value(data.anomalyProbability)

            const $filter = $("#Filter");
            if (data.filter) {
                $filter.data("kendoSwitch").check(true);
            } else {
                $filter.data("kendoSwitch").check(false);
            }

            const $reportTable = $("#ReportTable");
            if (data.reportTable) {
                $reportTable.data("kendoSwitch").check(true);
            } else {
                $reportTable.data("kendoSwitch").check(false);
            }

            ExpandCollapseAnomaly();
            ExpandCollapseFilter();

            initExhaustiveFilterBuilder(GetSelectedParentID(), builderCoderData);

            RefreshModelSummary();
            $("#Status").toggle();
            //interval = setInterval(RefreshModelSummary, 30000);
        });
}

function ExpandCollapseAnomaly() {
    const $anomalyTable = $("#AnomalyTable");
    if ($('#Anomaly').prop('checked')) {
        $anomalyTable.show();
    } else {
        $anomalyTable.hide();
    }
}

function ExpandCollapseFilter() {
    const $filterTable = $("#FilterTable");
    if ($('#Filter').prop('checked')) {
        $filterTable.show();
    } else {
        $filterTable.hide();
    }
}

$("#Anomaly").kendoSwitch({
    change: function () {
        ExpandCollapseAnomaly();
    }
});

$("#Filter").kendoSwitch({
    change: function () {
        ExpandCollapseFilter();
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
    let data = {};

    if ($('#Filter').prop('checked')) {
        const builderCoder = getExhaustiveFilter();
        data["filter"] = true;
        data["filterSql"] = builderCoder.filterSql;
        data["filterJson"] = builderCoder.filterJson;
        data["filterTokens"] = builderCoder.filterTokens;
    }

    if ($('#Anomaly').prop('checked')) {
        data["anomaly"] = true;
        data["AnomalyProbability"] = anomalyProbability.data("kendoSlider").value();
    }

    return data;
}

$(function () {
    $("#Refresh").kendoButton()
        .click(function () {
            RefreshModelSummary(true);
        });
});

function SetCurrentStatusStopping(data) {
    $("#Stop").data("kendoButton").enable(false);
    let currentStatus = $('#CurrentStatus');
    currentStatus.text("Stopping");
}

$(function () {
    $("#Stop").kendoButton()
        .click(function () {
            jQuery.ajax({
                type: 'PUT',
                contentType: "application/json; charset=utf-8",
                url: "../api/ExhaustiveSearchInstance/stop/" + Guid,
                success: SetCurrentStatusStopping
            });
        });
});

$(function () {
    addButton
        .click(function () {
            if (validator.validate()) {
                Create(endpoint, GetData(), "id", parentKeyName, LoadModelSummery);
                $("#Status").toggle();
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

//# sourceURL=Exhaustive.js