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
    const exhaustiveSwitch = $("#Exhaustive").kendoSwitch({ checked: true }).data("kendoSwitch");
    const suppressionsSwitch = $("#Suppressions").kendoSwitch({ checked: true }).data("kendoSwitch");
    const listsSwitch = $("#Lists").kendoSwitch({ checked: true }).data("kendoSwitch");
    const dictionariesSwitch = $("#Dictionaries").kendoSwitch({ checked: true }).data("kendoSwitch");
    const visualisationsSwitch = $("#Visualisations").kendoSwitch({ checked: true }).data("kendoSwitch");
    const rolesSwitch = $("#Roles").kendoSwitch({ checked: true }).data("kendoSwitch");

    $("#Download").kendoButton({
        click: async function () {
            const data = {
                Password: $("#Password").val(),
                Exhaustive: exhaustiveSwitch.check(),
                Suppressions: suppressionsSwitch.check(),
                Lists: listsSwitch.check(),
                Dictionaries: dictionariesSwitch.check(),
                Visualisations: visualisationsSwitch.check(),
                Roles: rolesSwitch.check(),
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

        const params = new URLSearchParams({
            Exhaustive: exhaustiveSwitch.check(),
            Suppressions: suppressionsSwitch.check(),
            Lists: listsSwitch.check(),
            Dictionaries: dictionariesSwitch.check(),
            Visualisations: visualisationsSwitch.check(),
            Roles: rolesSwitch.check()
        });

        window.open("/api/Preservation/ExportPeek?" + params.toString());
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
                Password: $("#Password").val()
            };
        }
    });
});
