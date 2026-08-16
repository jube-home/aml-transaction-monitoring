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

const endpoint = "/api/GetEntityAnalysisModelSample";

let entityAnalysisModel;
let sample;
let dateFrom;
let dateTo;

$(document).ready(function () {
    entityAnalysisModel = $("#EntityAnalysisModel").kendoDropDownList({
        dataTextField: 'name',
        dataValueField: 'guid',
        dataSource: {
            type: 'json',
            transport: {
                read: '/api/EntityAnalysisModel'
            }
        }
    });
    entityAnalysisModel.data("kendoDropDownList").wrapper.width(300);

    dateFrom = $("#DateFrom").kendoDateTimePicker({
        value: new Date(Date.now() - 86400000),
        format: "MM/dd/yyyy HH:mm",
        interval: 15
    });
    dateFrom.data("kendoDateTimePicker").wrapper.width(300);

    dateTo = $("#DateTo").kendoDateTimePicker({
        value: new Date(),
        format: "MM/dd/yyyy HH:mm",
        interval: 15
    });
    dateTo.data("kendoDateTimePicker").wrapper.width(300);

    sample = $("#Sample").kendoSlider({
        increaseButtonTitle: "Right",
        decreaseButtonTitle: "Left",
        min: 0,
        max: 1,
        value: 0.02,
        smallStep: 0.01,
        largeStep: 0.05
    });

    $("#Download").kendoButton({
        click: async function (e) {
            const button = $(e.sender.element).data("kendoButton");
            clearErrors();
            button.enable(false);

            try {
                const resp = await fetch(endpoint, {
                    method: "POST",
                    headers: {"Content-Type": "application/json"},
                    body: JSON.stringify({
                        entityAnalysisModelGuid: entityAnalysisModel.data("kendoDropDownList").value(),
                        sample: sample.data("kendoSlider").value(),
                        dateFrom: dateFrom.data("kendoDateTimePicker").value()?.toISOString() ?? null,
                        dateTo: dateTo.data("kendoDateTimePicker").value()?.toISOString() ?? null
                    })
                });

                if (resp.status === 400) {
                    DisplayServerValidationErrors(await resp.json());
                    return;
                }

                if (!resp.ok) {
                    displayError(`An unexpected server error occurred (HTTP ${resp.status}). Please try again.`);
                    return;
                }

                const blob = await resp.blob();

                if (blob.size === 0) {
                    displayError("The server returned an empty file. Please try again.");
                    return;
                }

                const disposition = resp.headers.get("Content-Disposition");
                const filename = disposition?.match(/filename[^;=\n]*=["']?([^"';\n]*)["']?/)?.[1]?.trim()
                    ?? "export.jemp";

                const url = URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                a.download = filename;
                a.click();
                setTimeout(() => URL.revokeObjectURL(url), 1000);

            } catch (err) {
                displayError("A network error occurred. Please check your connection and try again.");
                console.error("Download failed:", err);
            } finally {
                button.enable(true);
            }
        }
    });

    function clearErrors() {
        $("#ErrorMessage").empty();
    }

    function displayError(message) {
        DisplayServerValidationErrors({
            errors: {
                _global: {errorMessage: message, propertyName: null}
            }
        });
    }

    function DisplayServerValidationErrors(responseObject) {
        const errorMessage = $("#ErrorMessage");
        errorMessage.empty();

        const container = $(`
        <div class="server-error-box">
            <div class="server-error-title">Validation Errors:</div>
        </div>
    `);

        for (let key in responseObject.errors) {
            const e = responseObject.errors[key];

            container.append(`
            <div class="server-error-line">${e.errorMessage}</div>
        `);

            highlightFieldError(e.propertyName, e.errorMessage);
        }

        errorMessage.append(container);
    }

    function highlightFieldError(fieldId, errorMessage) {
        let field = $(`#${fieldId}`);
        const fieldByName = $(`input[name='${fieldId}']`);

        if (fieldByName.length > 1 && fieldByName.attr('type') === 'radio') {
            field = fieldByName;

            field.each(function () {
                $(this).addClass("field-error-highlight");
                $(this).attr("title", errorMessage);
                $(this).tooltip && $(this).tooltip();
            });
            return;
        }

        if (!field.length) return;

        const widget = field.data("kendoNumericTextBox")
            || field.data("kendoSwitch")
            || field.data("kendoTextBox")
            || field.data("kendoMaskedTextBox")
            || field.data("kendoDropDownList")
            || field.data("kendoComboBox")
            || field.data("kendoMultiSelect")
            || field.data("kendoDatePicker")
            || field.data("kendoDateTimePicker");

        if (widget) {
            const wrapper = widget.wrapper || widget.element.closest(".k-widget");
            if (wrapper && wrapper.length) {
                wrapper.addClass("field-error-highlight");
                wrapper.attr("title", errorMessage);
                wrapper.tooltip && wrapper.tooltip();
                return;
            }
        }

        field.addClass("field-error-highlight");
        field.attr("title", errorMessage);
        field.tooltip && field.tooltip();
    }
});

//# sourceURL=EntityAnalysisModelSample.js