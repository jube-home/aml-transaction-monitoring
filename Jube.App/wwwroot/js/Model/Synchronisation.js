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

function Refresh() {
    $("#grid").data("kendoGrid").dataSource.read();
    GetSchedule();
}

function SetColor() {
    const grid = $('#grid').data('kendoGrid');
    const rows = grid.tbody.children();
    for (let j = 0; j < rows.length; j++) {
        const row = $(rows[j]);
        const dataItem = grid.dataItem(row);
        if (dataItem.get("synchronisationPending") && dataItem.get("instanceAvailable")) {
            row.css("color", "orange");
        } else if (dataItem.get("instanceAvailable")) {
            row.css("color", "green");
        } else {
            row.css("color", "red");
        }
    }
}

function GetSchedule() {
    $.get("/api/EntityAnalysisModelSynchronisationSchedule/ByCurrent",
        function (data) {
            $("#datetimepicker").data("kendoDateTimePicker").value(kendo.parseDate(data.scheduleDate));
        });
}

$(document).ready(function () {
    const $scheduleSynchronisation = $("#ScheduleSynchronisation");
    const $synchroniseNow = $("#SynchroniseNow");
    const $refresh = $("#Refresh");
    $scheduleSynchronisation.kendoButton();
    $synchroniseNow.kendoButton();
    $refresh.kendoButton();

    GetSchedule();

    const dataSourceEntity = new kendo.data.DataSource({
        transport: {
            read: {
                url: "/api/GetEntityAnalysisModelSynchronisationNodeStatusEntries",
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
                fields: {
                    instance: {type: "string"},
                    heartbeatDate: {type: "date"},
                    synchronisedDate: {type: "date"}
                }
            }
        }
    });

    $scheduleSynchronisation.click(function () {
        $.ajax({
            url: "/api/EntityAnalysisModelSynchronisationSchedule",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify({ScheduleDate: new Date().toISOString()}),
            success: function (data) {
                const echoDate = new Date(data.scheduleDate);
                $("#datetimepicker").data("kendoDateTimePicker").value(echoDate);
                Refresh();
            }
        });
    });

    $synchroniseNow.click(function () {
        $.ajax({
            url: "/api/EntityAnalysisModelSynchronisationSchedule",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify({}),
            success: function (data) {
                $("#datetimepicker").data("kendoDateTimePicker").value(kendo.parseDate(data.scheduleDate));
                Refresh();
            }
        });
    });

    $refresh.click(function () {
        Refresh();
    });

    $("#grid").kendoGrid({
        dataSource: dataSourceEntity,
        pageable: false,
        height: 360,
        scrollable: true,
        filterable: true,
        dataBound: SetColor,
        columns: [
            {field: "instance", title: "Instance"},
            {field: "heartbeatDate", title: "Heartbeat"},
            {field: "synchronisedDate", title: "Synchronised"}
        ]
    });

    setInterval(Refresh, 10000);

    $("#datetimepicker").kendoDateTimePicker({
        value: new Date(),
        parseFormats: ["yyyy-MM-ddThh:mm:ss"],
        format: "yyyy-MM-dd HH:mm:ss",
        dateInput: true
    });
});

//# sourceURL=Synchronisation.js