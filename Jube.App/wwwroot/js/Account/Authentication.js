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

var isChange;
var passwordResetRendered = false;

$(document).ready(function () {
    $("#FormAuthenticate").kendoValidator({
        errorTemplate: "<span class='errorMessage'>#=message#</span>"
    });

    $("#FormChange").kendoValidator({
        errorTemplate: "<span class='errorMessage'>#=message#</span>",
        rules: {
            verifyPasswords: function (input) {
                if (input.is("#VerifyNewPassword")) {
                    return input.val() === $("#NewPassword").val();
                }
                return true;
            }
        }
    });

    $("#PasswordResetDiv").hide();
    $("#Message").hide();
    $("#MessageServerValidation").hide();

    $("#Change").kendoButton({
        click: function (e) {
            if ($("#FormChange").data("kendoValidator").validate()) {
                $("#MessageChange").css('color', 'green');
                $("#MessageChange").html("Changing.");
                $("#MessageServerValidation").hide();
                $("#Change").data("kendoButton").enable(false);
                PostAuthentication();
            }
        }
    }).hide();

    function PostAuthentication() {
        let data = {
            userName: $("#UserName").val(),
            password: $("#Password").val(),
            newPassword: $("#NewPassword").val(),
            repeatNewPassword: $("#VerifyNewPassword").val(),
            PasswordChangeState: isChange
        };

        $.ajax({
            url: "../api/Authentication/ByUserNamePassword",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(data),
            statusCode: {
                200: function (response) {
                    document.location.replace("/");
                },
                403: function (response) {
                    if (!passwordResetRendered) {
                        passwordResetRendered = true;
                        $("#PasswordResetContainer").append($("#PasswordResetTemplate").prop("content").cloneNode(true));
                        $("#FormChange").kendoValidator({
                            errorTemplate: "<span class='errorMessage'>#=message#</span>",
                            rules: {
                                verifyPasswords: function (input) {
                                    if (input.is("#VerifyNewPassword")) {
                                        return input.val() === $("#NewPassword").val();
                                    }
                                    return true;
                                }
                            }
                        });
                        $("#Change").kendoButton({
                            click: function (e) {
                                if ($("#FormChange").data("kendoValidator").validate()) {
                                    $("#MessageChange").css('color', 'green');
                                    $("#MessageChange").html("Changing.");
                                    $("#MessageServerValidation").hide();
                                    $("#Change").data("kendoButton").enable(false);
                                    PostAuthentication();
                                }
                            }
                        });
                    }
                    $("#MessageAuthenticate").html("green");
                    $("#MessageAuthenticate").html("Password must be changed.");
                    $("#UserName").attr("disabled", "disabled");
                    $("#Password").attr("disabled", "disabled");
                    isChange = true;
                },
                401: function (response) {
                    $("#Login").data("kendoButton").enable(true);
                    $("#MessageAuthenticate").css('color', 'red');

                    let errorString = '</br></br>Invalid Login.';
                    $("#MessageAuthenticate").html(errorString);
                },
                400: function (response) {
                    let errors = JSON.parse(response.responseText).errors;
                    let i = 0;
                    let errorListString = '';
                    while (i < errors.length) {
                        errorListString = errorListString + '<li>' + errors[i].errorMessage + '</li>';
                        i++;
                    }

                    if (isChange) {
                        $("#MessageChange").html("");
                        $("#Change").data("kendoButton").enable(true);
                        let errorString = '</br></br>Validation errors in password change:</br></br><ul>' + errorListString + '</ul>';
                        $("#MessageChange").html(errorString);
                        $("#FormChange").data("kendoValidator").reset();
                    } else {
                        $("#Login").data("kendoButton").enable(true);
                        $("#MessageAuthenticate").css('color', 'red');
                        let errorString = '</br></br>Validation errors in authentication:</br></br><ul>' + errorListString + '</ul>';
                        $("#MessageAuthenticate").html(errorString);
                        $("#FormChange").data("kendoValidator").reset();
                    }
                }
            }
        });
    }

    $("#Login").kendoButton({
        click: function (e) {
            if ($("#FormAuthenticate").data("kendoValidator").validate()) {
                $("#Login").data("kendoButton").enable(false);
                $("#MessageAuthenticate").css('color', 'green');
                $("#MessageAuthenticate").html("Validating.");
                PostAuthentication();
            }
        }
    });
});

//# sourceURL=Authentication.js