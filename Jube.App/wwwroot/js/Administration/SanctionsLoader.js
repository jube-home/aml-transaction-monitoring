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
    const sanctionEntrySource = $("#SanctionEntrySource").kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value"
    }).data("kendoDropDownList");

    $.get("../api/SanctionEntrySource",
        function (data) {
            if (typeof data !== 'undefined') {
                const items = data.map(value => ({
                    value: value.id,
                    text: value.name
                }));

                sanctionEntrySource.setDataSource(new kendo.data.DataSource({data: items}));
                sanctionEntrySource.text("");
                sanctionEntrySource.value("");
            }

            if (typeof set !== 'undefined') {
                sanctionEntrySource.value(set);
            } else {
                sanctionEntrySource.select(0);
            }
        });

    $("#Files").kendoUpload({
        async: {
            saveUrl: "/api/SanctionEntrySource/Import", autoUpload: false, multiple: false
        },
        validation: {
            allowedExtensions: [".csv"]
        },
        upload: function (e) {
            e.data = {
                Id: sanctionEntrySource.value()
            };
        }
    });
});