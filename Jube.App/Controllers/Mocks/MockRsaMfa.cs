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

namespace Jube.App.Controllers.Mocks
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dto.Requests;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [Produces("application/json")]
    public class MfaController : Controller
    {
        [HttpPost]
        public Task<ActionResult> PostAsync([FromBody] MockRsaMfaRequestDto requestDto, CancellationToken token = default)
        {
            try
            {
                if (!Request.Headers.TryGetValue("client-key", out var clientKeyValues))
                {
                    return Task.FromResult<ActionResult>(
                        Unauthorized(new
                        {
                            error = "Missing client-key header."
                        }));
                }

                if (clientKeyValues.Count != 1)
                {
                    return Task.FromResult<ActionResult>(
                        Unauthorized(new
                        {
                            error = $"Expected exactly one client-key value, got {clientKeyValues.Count}."
                        }));
                }

                if (clientKeyValues[0] != "SomethingSecretForTheClientKeyHeader")
                {
                    return Task.FromResult<ActionResult>(
                        Unauthorized(new
                        {
                            error = "Invalid client-key."
                        }));
                }

                if (String.IsNullOrEmpty(Request.ContentType) ||
                    !Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<ActionResult>(
                        new ObjectResult(new
                        {
                            error = $"Expected application/json, got '{Request.ContentType}'."
                        })
                        {
                            StatusCode = StatusCodes.Status415UnsupportedMediaType
                        });
                }

                var otp = requestDto?.SubjectCredentials.FirstOrDefault()
                    ?.CollectedInputs
                    .FirstOrDefault()
                    ?.Value;

                var success = otp == "12345678";

                var response = new
                {
                    context = new
                    {
                        authnAttemptId = Guid.NewGuid().ToString(),
                        messageId = Guid.NewGuid().ToString(),
                        inResponseTo = "test5213021196242"
                    },
                    credentialValidationResults = new[]
                    {
                        new
                        {
                            methodId = "SECURID",
                            methodResponseCode = "SUCCESS",
                            methodReasonCode = (string)null,
                            authnAttributes = Array.Empty<object>()
                        }
                    },
                    attemptResponseCode = success ? "SUCCESS" : "NOPE",
                    attemptReasonCode = "CREDENTIAL_VERIFIED",
                    challengeMethods = new
                    {
                        challenges = new[]
                        {
                            new
                            {
                                methodSetId = (string)null,
                                requiredMethods = Array.Empty<object>()
                            }
                        }
                    }
                };

                return Task.FromResult<ActionResult>(Ok(response));
            }
            catch (Exception exception)
            {
                return Task.FromException<ActionResult>(exception);
            }
        }
    }
}
