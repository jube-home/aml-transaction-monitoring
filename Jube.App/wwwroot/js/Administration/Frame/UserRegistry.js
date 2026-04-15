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

const endpoint = "/api/UserRegistry";
const parentKeyName = "roleRegistryId";
const validationFail = "There is invalid data in the form. Please check fields and correct.";

const store = {
    list() {
        return $.ajax({
            url: "/api/UserRegistryApiKey/ByUserRegistryId/" + id,
            method: "GET",
        });
    },

    add(name, description) {
        return $.ajax({
            url: "/api/UserRegistryApiKey",
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({name, description, userRegistryId: id}),
        });
    },

    remove(id) {
        return $.ajax({
            url: `/api/UserRegistryApiKey/${id}`,
            method: "DELETE",
        });
    },
};

const ds = new kendo.data.DataSource({
    transport: {
        read(options) {
            if (typeof id === "undefined") return options.success([]);
            store.list()
                .done(data => options.success(data))
                .fail(xhr => options.error(xhr));
        },
    },
    schema: {
        model: {
            id: "id",
            fields: {
                name: {type: "string"},
                description: {type: "string"},
                apiKeyDisplay: {type: "string"},
                createdDate: {type: "date"},
            }
        }
    }
});

const gridElement = $("#ApiKeyGrid").kendoGrid({
    dataSource: ds,
    sortable: true,
    columns: [
        {field: "name", title: "Name", width: 200, template: tmplName},
        {field: "description", title: "Description", width: 250, template: tmplDescription},
        {field: "apiKeyDisplay", title: "Api Key", width: 340, template: tmplKey, sortable: false},
        {
            field: "createdDate", title: "Created Date", width: 120,
            template: row => row.id === "__new__" ? "" : escHtml(row.createdDate)
        },
        {title: "", width: 160, sortable: false, template: tmplActions},
    ],
    dataBound() {
        this.tbody.find("tr").each(function () {
            const uid = $(this).data("uid");
            const item = uid && ds.getByUid(uid);
            if (item && item.id === "__new__") $(this).addClass("k-new-row");
        });
    },
}).hide();

const grid = gridElement.data("kendoGrid");

var isAddingRow = false;
var revealedId = null;
var revealedKey = null;

var passwordLocked = $("#PasswordLocked").kendoSwitch().data("kendoSwitch");
passwordLocked.enable(false);

var roleRegistryId = $("#RoleRegistryId").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var resetButton = $("#Reset").kendoButton({
    click: function () {
        $("#Processing").show();
        $("#PasswordTable").hide();
        getPasswordAndShow();
    }
}).hide();

var addKey = $("#AddKey").kendoButton({
    click: beginAdd
}).hide();

function tmplName(row) {
    if (row.id === "__new__")
        return '<input class="new-row-input" id="inp-name" placeholder="Key name"/>';
    return escHtml(row.name);
}

function tmplDescription(row) {
    if (row.id === "__new__")
        return '<input class="new-row-input" id="inp-desc" placeholder="Description (optional)"/>';
    return escHtml(row.description);
}

function tmplKey(row) {
    if (row.id === "__new__") {
        return '<span style="color:#aaa;font-style:italic;font-size:12px">generated on save</span>';
    }

    if (row.id === revealedId && revealedKey) {
        return `<div class="key-reveal-wrap" style="display:flex;width:100%;box-sizing:border-box;">
                  <input class="key-reveal-input" style="flex:1;min-width:0; margin-right:4px;" readonly value="${escHtml(revealedKey)}">
                  <button class="btn-copy-key ButtonDefault" data-key="${escHtml(revealedKey)}">Copy</button>
                </div>`;
    }

    return `<span style="font-family:monospace;font-size:12px">${escHtml(row.apiKeyDisplay)}</span>`
        + `<span class="key-placeholder">••••••••••••••••••••</span>`;
}

function tmplActions(row) {
    if (row.id === "__new__")
        return '<button class="btn-save ButtonDefault" style="margin-right:4px;">Save</button><button class="btn-cancel-row">Cancel</button>';

    return `<button class="btn-delete ButtonDefault" style="margin-right:4px;" data-id="${escHtml(row.id)}">Revoke</button>`;
}

function getPasswordAndShow() {
    $.ajax({
        url: "/api/UserRegistry/SetPassword",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify({id: id}),
        success: function (data) {
            $("#Processing").hide();
            $("#Password").val(data.password);
            $("#PasswordExpiry").html(new Date(data.passwordExpiryDate).toLocaleString());
            $("#PasswordTable").show();
            resetButton.enable(true);
            passwordLocked.value(false);
        },
        error: function (xhr) {
            console.error("Error: " + xhr.statusText);
        }
    });
}

$("#PasswordTable").hide();
$("#Processing").hide();

$.get("/api/RoleRegistry",
    function (data) {
        for (const value of data) {
            roleRegistryId.data("kendoDropDownList").dataSource.add({
                "value": value.id,
                "text": value.name
            });
        }

        if (typeof id === "undefined") {
            roleRegistryId.data("kendoDropDownList").value(parentKey);
            passwordLocked.value(true);
            ReadyNew();
        } else {
            $.get(endpoint + "/" + id,
                function (data) {
                    $("#Email").val(data.email);
                    roleRegistryId.data("kendoDropDownList").value(data.roleRegistryId);

                    passwordLocked.value(data.passwordLocked);

                    resetButton.show();
                    gridElement.show();
                    addKey.show();

                    ReadyExisting(data);
                });
        }
    }
);

$(function () {
    deleteButton
        .click(function () {
            if (confirm('Are you sure you want to delete?')) {
                Delete(endpoint, id);
            }
        });
});

function getData() {
    return {
        name: $("#Name").val(),
        email: $("#Email").val(),
        roleRegistryId: roleRegistryId.data("kendoDropDownList").value()
    };
}

$(function () {
    addButton
        .click(function () {
            if (validator.validate()) {
                Create(endpoint, getData(), "id", parentKeyName);

                resetButton.show();
                gridElement.show();
                addKey.show();
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

$(function () {
    updateButton
        .click(function () {
            if (validator.validate()) {
                Update(endpoint, getData(), "id", parentKeyName);

                resetButton.show();
                gridElement.show();
                addKey.show();
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

function beginAdd() {
    if (isAddingRow) return;
    isAddingRow = true;
    clearReveal();
    ds.insert(0, {id: "__new__", name: "", description: "", prefix: "", createdDate: ""});
    grid.refresh();
    setTimeout(() => $("#inp-name").focus(), 40);
}

function commitAdd() {
    const name = $("#inp-name").val().trim();
    if (!name) {
        $("#inp-name").css("border-color", "#c0392b").focus();
        return;
    }

    $("#inp-name, #inp-desc").prop("disabled", true);
    $(".btn-save").text("Saving…").prop("disabled", true);

    const desc = $("#inp-desc").val().trim();

    store.add(name, desc)
        .done((response) => {
            const entry = {
                id: response.id,
                name: response.name,
                description: response.description,
                apiKeyDisplay: response.apiKeyDisplay,       // prefix for display after reveal
                createdDate: response.createdDate,
            };
            const fullKey = response.apiKeyDisplay;        // full key returned once from server

            ds.remove(ds.get("__new__"));
            ds.insert(0, entry);

            revealedId = entry.id;
            revealedKey = fullKey;

            isAddingRow = false;
            grid.refresh();
        })
        .fail((xhr) => {
            alert("Could not create API key. Please try again.");
            $("#inp-name, #inp-desc").prop("disabled", false);
            $(".btn-save").text("Save").prop("disabled", false);
        });
}

function cancelAdd() {
    ds.remove(ds.get("__new__"));
    isAddingRow = false;
    grid.refresh();
}

function clearReveal() {
    if (revealedId && revealedKey) {
        const item = ds.get(revealedId);
        if (item) {
            item.set("apiKeyDisplay", revealedKey.substring(0, 8));
        }
    }
    revealedId = null;
    revealedKey = null;
}

gridElement.on("click", ".btn-save", function (e) {
    e.preventDefault();
    commitAdd();
});

gridElement.on("click", ".btn-copy-key", function (e) {
    e.preventDefault();
    const key = $(this).data("key");
    navigator.clipboard.writeText(key);
});

gridElement.on("click", ".btn-cancel-row", cancelAdd);

gridElement.on("click", ".btn-delete", function (e) {
    const id = $(this).data("id");
    const btn = $(this);
    if (!confirm("Permanently revoke this API key?")) {
        e.preventDefault();
        return;
    }

    btn.text("Revoking…").prop("disabled", true);

    store.remove(id)
        .done(() => {
            if (id === revealedId) clearReveal();
            if (typeof id !== "undefined") ds.read();
        })
        .fail(xhr => {
            console.error("Failed to revoke API key", xhr);
            alert("Could not revoke API key. Please try again.");
            btn.text("Revoke").prop("disabled", false);
        });
});

gridElement.on("keydown", function (e) {
    if (!isAddingRow) return;
    if (e.key === "Enter") commitAdd();
    if (e.key === "Escape") cancelAdd();
});

ds.bind("change", function (e) {
    if (e.action !== "itemchange" && !isAddingRow) {
        clearReveal();
    }
});

function escHtml(str) {
    return String(str ?? "")
        .replace(/&/g, "&amp;").replace(/</g, "&lt;")
        .replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

//# sourceURL=UserRegistry.js