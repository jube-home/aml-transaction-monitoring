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

$(document).ready(function () {
    const exhaustiveSwitch = $("#Exhaustive").kendoSwitch();
    const suppressionsSwitch = $("#Suppressions").kendoSwitch();
    const listsSwitch = $("#Lists").kendoSwitch();
    const dictionariesSwitch = $("#Dictionaries").kendoSwitch();
    const visualisationsSwitch = $("#Visualisations").kendoSwitch();

    $("#Download").kendoButton({
        click: async function (e) {
            const data = {
                Password: $("#Password").val(),
                Exhaustive: exhaustiveSwitch.prop("checked"),
                Suppressions: suppressionsSwitch.prop("checked"),
                Lists: listsSwitch.prop("checked"),
                Dictionaries: dictionariesSwitch.prop("checked"),
                Visualisations: visualisationsSwitch.prop("checked"),
            };

            const resp = await fetch("/api/Preservation/Export", {
                method: "POST",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify(data)
            });

            const blob = await resp.blob();
            const url = URL.createObjectURL(blob);
            const a = document.createElement("a");

            const disposition = resp.headers.get("Content-Disposition");
            const filename = disposition?.match(/filename[^;=\n]*=["']?([^"';\n]*)["']?/)?.[1]
                ?? "export.jemp";

            a.href = url;
            a.download = filename;
            a.click();
            URL.revokeObjectURL(url);
        }
    });

    $("#Peek").on("click", function (e) {
        e.preventDefault();

        let data = {
            Exhaustive: exhaustiveSwitch.prop("checked"),
            Suppressions: suppressionsSwitch.prop("checked"),
            Lists: listsSwitch.prop("checked"),
            Dictionaries: dictionariesSwitch.prop("checked"),
            Visualisations: visualisationsSwitch.prop("checked"),
        };

        window.open("/api/Preservation/ExportPeek?Exhaustive=" + data.Exhaustive
            + "&Suppressions=" + data.Suppressions
            + "&Lists=" + data.Lists
            + "&Dictionaries="
            + data.Dictionaries
            + "&Visualisations=" + data.Visualisations);
    });

    $("#Files").kendoUpload({
        async: {
            saveUrl: "/api/preservation/import", autoUpload: false, multiple: false
        },
        validation: {
            allowedExtensions: [".jemp"]
        },
        upload: function (e) {
            e.data = {
                Password: $("#Password").val(),
                Exhaustive: exhaustiveSwitch.prop("checked"),
                Suppressions: suppressionsSwitch.prop("checked"),
                Lists: listsSwitch.prop("checked"),
                Dictionaries: dictionariesSwitch.prop("checked"),
                Visualisations: visualisationsSwitch.prop("checked"),
            };
        }
    });
});