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

const ChartData = [];
const PendingJSONObjects = [];
let InProgress = 0;

function UpdateChart() {
    const map = $("#map").data("kendoMap"); // get existing instance, don't reinitialise

    $('#Status').text('Total Plotted: ' +
        map.markers.items.length +
        '; Pending Plotting: ' +
        PendingJSONObjects.length +
        '.');

    if (PendingJSONObjects.length > 0 && InProgress === 0) {
        InProgress = 1;
        const grid = $("#grid").data("kendoGrid");

        const pendingJsonObjectsSplice = PendingJSONObjects.splice(0, 100);
        for (let i = 0; i < pendingJsonObjectsSplice.length; i++) {
            const jsonObject = pendingJsonObjectsSplice[i];

            if ($("#MapEntityOnce").prop('checked')) {
                for (let item in map.markers.items) {
                    if (Object.prototype.hasOwnProperty.call(map.markers.items, item)) {
                        if (map.markers.items[item].options.keyValue === (jsonObject.key + ":" + jsonObject.keyValue)) {
                            map.markers.remove(map.markers.items[item]);
                        }
                    }
                }
            }

            map.markers.add({
                location: [jsonObject.latitude, jsonObject.longitude],
                titleField: jsonObject.key + ":" + jsonObject.keyValue,
                shape: "pinTarget",
                colorField: jsonObject.backColor,
                keyValue: jsonObject.key + ":" + jsonObject.keyValue,
                tooltip: {
                    content: jsonObject.key + ":" + jsonObject.keyValue,
                }
            });

            const dsLen = grid.dataSource.data().length;
            if (dsLen > 10) {
                grid.dataSource.remove(grid.dataSource.at(dsLen - 1));
            }
            grid.dataSource.insert(0, jsonObject);

            MergeArray(jsonObject.activationRuleSummary, jsonObject.responseElevation);
        }

        const chart = $("#chart").data("kendoChart");
        chart.dataSource.read();
        chart.refresh();

        const rows = grid.tbody.children();
        for (let j = 0; j < rows.length; j++) {
            const row = $(rows[j]);
            const dataItem = grid.dataItem(row);
            row.css("background-color", dataItem.get("backColor"));
            row.css("color", dataItem.get("foreColor"));
        }
        InProgress = 0;
    }
}

function MergeArray(name, value) {
    const arrayLength = ChartData.length;
    let found = false;
    for (let i = 0; i < arrayLength; i++) {
        if (name === ChartData[i].ActivationRule) {
            ChartData[i].Aggregate = ChartData[i].Aggregate + value;
            ChartData[i].Frequency = ChartData[i].Frequency + 1;
            found = true;
        }
    }
    if (found !== true) {
        ChartData.push({
            "ActivationRule": name,
            "Frequency": 1,
            "Aggregate": value
        });
    }
}

function createChart() {
    $("#chart").kendoChart({
        legend: {
            position: "top"
        },
        dataSource: {
            data: ChartData
        },
        series: [
            {
                type: "column",
                field: "Frequency",
                stack: true,
                name: "Activation Frequency",
                color: "#cc6e38"
            }, {
                type: "line",
                field: "Aggregate",
                name: "Aggregate Response Elevation",
                color: "#ec5e0a",
                axis: "ResponseElevationContent"
            }
        ],
        valueAxes: [
            {
                title: {text: "Frequency"}
            }, {
                name: "ResponseElevationContent",
                title: {text: "Aggregate Response Elevation"}
            }
        ],
        categoryAxis: {
            field: "ActivationRule"
        }
    });
}

function createMap() {
    $("#map").kendoMap({
        center: [30.268107, -97.744821],
        urlTemplate: "https://#= subdomain #.tile.openstreetmap.org/#= zoom #/#= x #/#= y #.png",
        zoom: 3,
        markerActivate: function (e) {
            if (e.marker.options.colorField !== '#ffffff') {
                $(e.marker.element.context).css("background-color", e.marker.options.colorField);
            }
        },
        layers: [
            {
                type: "tile",
                urlTemplate: "https://#= subdomain #.tile.openstreetmap.org/#= zoom #/#= x #/#= y #.png",
                subdomains: ["a", "b", "c"],
                attribution: "&copy; <a href='https://osm.org/copyright'>OpenStreetMap contributors</a>"
            }
        ],
        markers: []
    });
}

$(document).ready(function () {
    $("#ReplayFrom").kendoDateTimePicker({
        value: new Date(Date.now() - 60000),
        dateInput: true,
        parseFormats: ["yyyy-MM-ddThh:mm:ss"]
    });

    $("#ReplayTo").kendoDateTimePicker({
        value: new Date(),
        dateInput: true,
        parseFormats: ["yyyy-MM-ddThh:mm:ss"]
    });

    $("#Active").kendoSwitch();

    $("#MapEntityOnce").kendoSwitch();

    $("#Replay").kendoButton().click(function () {
        const dateFromControl = $("#ReplayFrom").data("kendoDateTimePicker");
        const dateToControl = $("#ReplayTo").data("kendoDateTimePicker");

        $.ajax({
            url: '../api/ActivationWatcher/Replay',
            type: "GET",
            data: {
                dateFrom: dateFromControl.value()?.toISOString() ?? null,
                dateTo: dateToControl.value()?.toISOString() ?? null
            },
            success: function (data) {
                //Not implemented.
            }
        });
    });

    createChart();
    setInterval(UpdateChart, 1000);
    createMap();

    $("#grid").kendoGrid({
        dataSource: {
            data: [],
            schema: {
                model: {
                    fields: {
                        createdDate: {type: "date"},
                        key: {type: "string"},
                        keyValue: {type: "string"},
                        activationRuleSummary: {type: "string"},
                        responseElevation: {type: "number"},
                        responseElevationContent: {type: "string"},
                        backColor: {type: "string"},
                        foreColor: {type: "string"}
                    }
                }
            }
        },
        height: 400,
        sortable: false,
        columns: [
            {
                field: "createdDate",
                title: "Created Date"
            },
            {
                field: "key",
                title: "Key"
            },
            {
                field: "keyValue",
                title: "Key Value"
            },
            {
                field: "activationRuleSummary",
                title: "Activation Rule Summary"
            }, {
                field: "responseElevation",
                title: "Response Elevation"
            }, {
                field: "responseElevationContent",
                title: "Response Elevation Content"
            }
        ]
    });
});

$(function () {
        const connection = new signalR.HubConnectionBuilder().withUrl("/watcherHub").withAutomaticReconnect().build();

        connection.on("ReceiveMessage", function (name, message) {
            if (($("#Active").prop('checked') && name === 'RealTime') || name === 'Replay') {
                const jsonObject = JSON.parse(message);
                PendingJSONObjects.push(jsonObject);
            }
        });

        connection.start().then(function () {
            $.ajax({
                url: "../api/RegisterSignalRConnection/" +
                    connection.connection.connectionId
            });
        });
    }
);