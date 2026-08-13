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

const aggregationColumns = [
    {field: "sum", title: "Sum", format: "{0:n2}"},
    {field: "average", title: "Average", format: "{0:n2}"},
    {field: "count", title: "Count", format: "{0:n0}"},
    {field: "max", title: "Max", format: "{0:n0}"},
    {field: "min", title: "Min", format: "{0:n0}"},
    {field: "first", title: "First", format: "{0:n0}"},
    {field: "last", title: "Last", format: "{0:n0}"},
    {field: "confidence", title: "Confidence", format: "{0:n4}"}
];

const aggregationSchemaFields = {
    sum: {type: "number"},
    average: {type: "number"},
    count: {type: "number"},
    max: {type: "number"},
    min: {type: "number"},
    first: {type: "number"},
    last: {type: "number"},
    confidence: {type: "number"}
};

$(document).ready(function () {
    $("#Distance").kendoSlider({
        increaseButtonTitle: "Right",
        decreaseButtonTitle: "Left",
        value: 2,
        min: 0,
        max: 5,
        smallStep: 1,
        largeStep: 1
    });

    $("#MaxDistanceRatio").kendoSlider({
        increaseButtonTitle: "Right",
        decreaseButtonTitle: "Left",
        value: 0.3,
        min: 0,
        max: 1,
        smallStep: 0.1,
        largeStep: 0.1
    });

    $("#MaxCoverageRatio").kendoSlider({
        increaseButtonTitle: "Right",
        decreaseButtonTitle: "Left",
        value: 2.0,
        min: 0,
        max: 5,
        smallStep: 1,
        largeStep: 1
    });

    $("#Sanctions").kendoGrid({
        dataSource: {
            schema: {
                model: {
                    id: "id",
                    fields: {
                        id: {type: "number"},
                        distance: {type: "number"},
                        value: {type: "string"},
                        sanctionEntrySourceId: {type: "number"},
                        source: {type: "string"},
                        reference: {type: "string"}
                    }
                }
            }
        },
        pageable: false,
        height: 500,
        autoBind: false,
        columns: [
            {field: "id", title: "Id"},
            {field: "distance", title: "Distance"},
            {field: "value", title: "Element"},
            {field: "sanctionEntrySourceId", title: "Source Id"},
            {field: "source", title: "Source"},
            {field: "reference", title: "Reference"}
        ]
    });

    $("#AggregationsTotal").kendoGrid({
        dataSource: {
            schema: {
                model: {
                    fields: aggregationSchemaFields
                }
            }
        },
        pageable: false,
        autoBind: false,
        columns: aggregationColumns
    });

    $("#AggregationsBySource").kendoGrid({
        dataSource: {
            schema: {
                model: {
                    id: "sourceId",
                    fields: $.extend({
                        sourceId: {type: "number"},
                        sourceName: {type: "string"}
                    }, aggregationSchemaFields)
                }
            }
        },
        pageable: false,
        autoBind: false,
        columns: [{field: "sourceId", title: "Source Id"}, {
            field: "sourceName",
            title: "Source"
        }].concat(aggregationColumns)
    });

    $("#Check").kendoButton({
        click: function () {
            $.get("../api/Invoke/Sanction", {
                multiPartString: $("#MultiPartString").val(),
                distance: $("#Distance").data("kendoSlider").value(),
                maxDistanceRatio: $("#MaxDistanceRatio").data("kendoSlider").value(),
                maxCoverageRatio: $("#MaxCoverageRatio").data("kendoSlider").value()
            }, function (response) {
                $("#Sanctions").data("kendoGrid").dataSource.data(response.entries);

                $("#AggregationsTotal").data("kendoGrid").dataSource.data([response.aggregations.total]);
                $("#AggregationsBySource").data("kendoGrid").dataSource.data(response.aggregations.bySource);
            });
        }
    });
});
