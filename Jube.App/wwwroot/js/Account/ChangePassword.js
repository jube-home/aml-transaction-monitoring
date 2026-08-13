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
    const $change = $("#Change");
    const $messageChange = $("#MessageChange");
    const $existingPassword = $("#ExistingPassword");
    const $newPassword = $("#NewPassword");
    const $verifyNewPassword = $("#VerifyNewPassword");

    async function sha256(input) {
        const encoder = new TextEncoder();
        const data = encoder.encode(input);
        const hashBuffer = await crypto.subtle.digest('SHA-256', data);
        return Array.from(new Uint8Array(hashBuffer))
            .map(b => b.toString(16).padStart(2, '0')).join('');
    }

    $change.kendoButton({
        click: async function (e) {
            e.preventDefault();
            $messageChange.html("");

            const existingPassword = $existingPassword.val();
            const newPassword = $newPassword.val();
            const verifyNewPassword = $verifyNewPassword.val();

            if (!existingPassword || !newPassword || !verifyNewPassword) {
                $messageChange.css('color', 'red');
                $messageChange.show();
                $messageChange.html("<br/><br/>All fields are required.");
                return;
            }

            if (newPassword !== verifyNewPassword) {
                $messageChange.css('color', 'red');
                $messageChange.show();
                $messageChange.html("<br/><br/>New passwords do not match.");
                return;
            }

            if (wirePasswordHash) {
                const result = PasswordStrength.validate(newPassword);
                if (!result.valid) {
                    $messageChange.css('color', 'red');
                    $messageChange.show();
                    $messageChange.html("<br/><br/>Password requirements:<br/><br/><ul>" +
                        result.failures.map(f => `<li>${f}</li>`).join('') + "</ul>");
                    return;
                }
            }

            $change.data("kendoButton").enable(false);
            $messageChange.css('color', 'green');
            $messageChange.show();
            $messageChange.html("<br/><br/>Changing.");

            await PostAuthentication();
        }
    });

    async function PostAuthentication() {
        const response = await fetch(`../api/Authentication/WirePasswordHash`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({userName: userName})
        });

        const scheme = await response.json();
        wirePasswordHash = scheme.wirePasswordHash;

        let password = wirePasswordHash
            ? await sha256($existingPassword.val() + userName)
            : $existingPassword.val();

        let newPassword = wirePasswordHash
            ? await sha256($newPassword.val() + userName)
            : $newPassword.val();

        if (passwordAsymmetricEncryption) {
            password = await encryptPassword(password);
            newPassword = await encryptPassword(newPassword);
        }

        let data = {
            userName: userName,
            password: password,
            newPassword: newPassword
        };

        $.ajax({
            url: "../api/Authentication/ChangePassword",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(data),
            statusCode: {
                200: function () {
                    $messageChange.css('color', 'green');
                    $messageChange.show();
                    $messageChange.html("<br/><br/>Done.");
                    $change.data("kendoButton").enable(true);
                },
                401: function () {
                    $change.data("kendoButton").enable(true);
                    $("#DoneMessage").html("");
                    $messageChange.css('color', 'red');
                    $messageChange.show();
                    $messageChange.html("<br/><br/>Invalid Login. Check existing password and try again.");
                },
                400: function (response) {
                    let errors = JSON.parse(response.responseText).errors;
                    let errorListString = '';
                    for (let i = 0; i < errors.length; i++) {
                        errorListString += '<li>' + errors[i].errorMessage + '</li>';
                    }

                    const changeBtn = $change.data("kendoButton");
                    if (changeBtn) changeBtn.enable(true);

                    $("#DoneMessage").html("");
                    $messageChange.css('color', 'red');
                    $messageChange.show();
                    $messageChange.html('<br/><br/>Validation errors in password change:<br/><br/><ul>' + errorListString + '</ul>');
                }
            }
        });
    }
});
