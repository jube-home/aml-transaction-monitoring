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
    let isChange = false;
    let passwordResetRendered = false;
    let negotiateMfaPending = false;
    let wirePasswordHash = false;

    const $formAuthenticate = $("#FormAuthenticate");
    const $userName = $("#UserName");
    const $password = $("#Password");
    const $mfaRow = $("#MfaRow");
    const $mfa = $("#Mfa");
    const $messageAuthenticate = $("#MessageAuthenticate");
    const $login = $("#Login");

    if (mfa) {
        ShowMfaRow(false);
    }

    if (negotiateAuthentication) {
        $.ajax({
            url: "../api/Authentication/ByNegotiate",
            type: "GET",
            xhrFields: {
                withCredentials: true
            },
            statusCode: {
                200: function () {
                    document.location.replace("/");
                },
                202: function () {
                    negotiateMfaPending = true;
                    $formAuthenticate.show();
                    ShowMfaRow(true);
                },
                401: function () {
                    $formAuthenticate.show();
                }
            },
            error: function (xhr) {
                if (xhr.status !== 401) {
                    console.warn("Negotiate authentication attempt failed; falling back to manual login.");
                }
                $formAuthenticate.show();
            }
        });
    } else {
        $formAuthenticate.show();
    }

    function ShowMfaRow(isNegotiate) {
        if (isNegotiate) {
            $userName.closest("tr").hide();
            $password.closest("tr").hide();
            $userName.removeAttr("required").removeAttr("name");
            $password.removeAttr("required").removeAttr("name");
            $mfa.focus();
        }
        $mfaRow.show();
    }

    async function sha256(input) {
        const encoder = new TextEncoder();
        const data = encoder.encode(input);
        const hashBuffer = await crypto.subtle.digest('SHA-256', data);
        return Array.from(new Uint8Array(hashBuffer))
            .map(b => b.toString(16).padStart(2, '0')).join('');
    }

    function validatePasswordChange() {
        const newPassword = $("#NewPassword").val();
        const verifyNewPassword = $("#VerifyNewPassword").val();
        const $messageChange = $("#MessageChange");

        $messageChange.html("");

        if (newPassword !== verifyNewPassword) {
            $messageChange.css('color', 'red');
            $messageChange.html("</br></br>New passwords do not match.");
            return false;
        }

        if (!newPassword || !verifyNewPassword) {
            $messageChange.css('color', 'red');
            $messageChange.html("</br></br>All fields are required.");
            return false;
        }

        if (wirePasswordHash) {
            const result = PasswordStrength.validate(newPassword);
            if (!result.valid) {
                $messageChange.css('color', 'red');
                $messageChange.html("</br></br>Password requirements:</br></br><ul>" +
                    result.failures.map(f => `<li>${f}</li>`).join('') + "</ul>");
                return false;
            }
        }

        return true;
    }

    $formAuthenticate.kendoValidator({
        errorTemplate: "<span class='errorMessage'>#=message#</span>"
    });

    $("#PasswordResetDiv").hide();
    $("#Message").hide();
    $("#MessageServerValidation").hide();

    const $changeInitial = $("#Change");
    $changeInitial.kendoButton({
        click: async function (e) {
            e.preventDefault();
            if (validatePasswordChange()) {
                const $messageChange = $("#MessageChange");
                $messageChange.css('color', 'green');
                $messageChange.html("Changing.");
                $("#MessageServerValidation").hide();
                $changeInitial.data("kendoButton").enable(false);
                await PostAuthentication();
            }
        }
    }).hide();

    async function PostAuthentication() {
        const userName = $userName.val();

        let password = wirePasswordHash
            ? await sha256($password.val() + userName)
            : $password.val();

        let newPassword = null;
        let repeatNewPassword = null;

        if (isChange) {
            const $newPassword = $("#NewPassword");
            const $verifyNewPassword = $("#VerifyNewPassword");
            newPassword = wirePasswordHash
                ? await sha256($newPassword.val() + userName)
                : $newPassword.val();
            repeatNewPassword = wirePasswordHash
                ? await sha256($verifyNewPassword.val() + userName)
                : $verifyNewPassword.val();
        }

        let url = negotiateMfaPending
            ? "../api/Authentication/ByNegotiateMfa"
            : "../api/Authentication/ByUserNamePassword";

        if (passwordAsymmetricEncryption) {
            password = await encryptPassword(password);
            if (isChange) {
                newPassword = await encryptPassword(newPassword);
                repeatNewPassword = await encryptPassword(repeatNewPassword);
            }
        }

        let data = {
            userName: userName,
            password: password,
            newPassword: newPassword,
            repeatNewPassword: repeatNewPassword,
            PasswordChangeState: isChange,
            mfa: $mfa.val() || null
        };

        $.ajax({
            url: url,
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(data),
            statusCode: {
                200: function () {
                    document.location.replace("/");
                },
                403: function () {
                    if (!passwordResetRendered) {
                        passwordResetRendered = true;
                        $("#PasswordResetContainer").append($("#PasswordResetTemplate").prop("content").cloneNode(true));

                        const $change = $("#Change");
                        $change.kendoButton({
                            click: async function (e) {
                                e.preventDefault();
                                if (validatePasswordChange()) {
                                    const $messageChange = $("#MessageChange");
                                    $messageChange.css('color', 'green');
                                    $messageChange.html("Changing.");
                                    $("#MessageServerValidation").hide();
                                    $change.data("kendoButton").enable(false);
                                    await PostAuthentication();
                                }
                            }
                        });
                    }

                    $messageAuthenticate.css('color', 'green');
                    $messageAuthenticate.html("Password must be changed.");
                    $userName.attr("disabled", "disabled");
                    $password.attr("disabled", "disabled");
                    isChange = true;
                },
                401: function () {
                    $login.data("kendoButton").enable(true);
                    $messageAuthenticate.css('color', 'red');
                    $password.val("");

                    if ($mfaRow.is(":visible")) {
                        $mfa.val("");
                        $mfa.focus();
                        $messageAuthenticate.html("</br></br>Invalid Login.");
                    } else {
                        $messageAuthenticate.html("</br></br>Invalid Login.");
                        passwordResetRendered = false;
                        isChange = false;
                    }
                },
                400: function (response) {
                    let errors = JSON.parse(response.responseText).errors;
                    let errorListString = '';
                    for (let i = 0; i < errors.length; i++) {
                        errorListString += '<li>' + errors[i].errorMessage + '</li>';
                    }

                    if (isChange) {
                        const changeBtn = $("#Change").data("kendoButton");
                        if (changeBtn) changeBtn.enable(true);
                        const $messageChange = $("#MessageChange");
                        $messageChange.css('color', 'red');
                        $messageChange.html('</br></br>Validation errors in password change:</br></br><ul>' + errorListString + '</ul>');
                    } else {
                        const loginBtn = $login.data("kendoButton");
                        if (loginBtn) loginBtn.enable(true);
                        $messageAuthenticate.css('color', 'red');
                        $messageAuthenticate.html('</br></br>Validation errors in authentication:</br></br><ul>' + errorListString + '</ul>');
                    }
                }
            }
        });
    }

    $login.kendoButton({
        click: async function () {
            if ($formAuthenticate.data("kendoValidator").validate()) {
                $login.data("kendoButton").enable(false);
                $messageAuthenticate.css('color', 'green');
                $messageAuthenticate.html("Validating.");

                const userName = $userName.val();

                const response = await fetch(`../api/Authentication/WirePasswordHash`, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({userName: userName})
                });

                const scheme = await response.json();
                wirePasswordHash = scheme.wirePasswordHash;

                await PostAuthentication();
            }
        }
    });
});

//# sourceURL=Authentication.js