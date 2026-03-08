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

const dataSourceEntity = new kendo.data.DataSource({
    transport: {
        read: {
            url: "/api/EntityAnalysisModelProcessingCounter",
            type: "GET",
            dataType: "json"
        },
        parameterMap: function (options, operation) {
            if (operation !== "read" && options.models) {
                return {models: kendo.stringify(options.models)};
            }
        }
    },
    schema: {
        model: {
            id: "id",
            averageResponseTime: function () {
                let totalTime = this.get("modelTotalResponseTime");
                let invokes = this.get("modelInvoke");
                return invokes > 0 ? (totalTime / invokes) : 0;
            },
            fields: {
                name: {type: "string", editable: false},
                instance: {type: "string", editable: false},
                createdDate: {type: "date", editable: false},
                modelInvoke: {type: "number", editable: false},
                gatewayMatch: {type: "number", editable: false},
                responseElevation: {type: "number", editable: false},
                activationWatcher: {type: "number", editable: false},
                responseElevationLimit: {type: "number", editable: false},
                modelTotalResponseTime: {type: "number", editable: false}
            }
        }
    }
});

$(document).ready(function () {
    const values = [];

    $("#grid").kendoGrid({
        groupable: true,
        dataSource: dataSourceEntity,
        pageable: false,
        height: $(window).height() - 210,
        scrollable: true,
        filterable: true,
        dataBound: function () {
            for (let i = 0; i < this.columns.length; i++) {
                this.autoFitColumn(i);
            }
        },
        columns: [
            {field: "name", title: "Entity Analysis Model"},
            {field: "instance", title: "Instance"},
            {field: "createdDate", title: "Created Date"},
            {field: "modelInvoke", title: "Model Invocation"},
            {field: "gatewayMatch", title: "Gateway Rule Match"},
            {field: "responseElevation", title: "Response Elevation"},
            {field: "activationWatcher", title: "Activation Watcher"},
            {field: "responseElevationLimit", title: "Response Elevation Limit"},
            {field: "modelTotalResponseTime", title: "Total Response Time"},
            {
                field: "averageResponseTime()",
                title: "Average Response Time",
                format: "{0:0}"
            }
        ]
    });
});

//# sourceURL=EntityAnalysisModelProcessingCounter.js