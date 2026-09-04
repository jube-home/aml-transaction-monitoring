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

namespace Jube.App.Controllers.Authentication
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Code;
    using Data.Context;
    using Dto;
    using DynamicEnvironment;
    using Jube.Dto.Authentication;
    using log4net;
    using Mfa;
    using Mfa.Rsa;
    using Microsoft.AspNetCore.Authentication.Negotiate;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Service.Authentication;
    using Service.Exceptions.Authentication;
    using Validations.Authentication;

    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthenticationController : Controller
    {
        private readonly IHttpContextAccessor contextAccessor;

        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly Authentication service;

        public AuthenticationController(DynamicEnvironment dynamicEnvironment,
            IHttpContextAccessor contextAccessor, ILog log)
        {
            this.dynamicEnvironment = dynamicEnvironment;
            dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(this.dynamicEnvironment.AppSettings("ConnectionString"), log);
            this.contextAccessor = contextAccessor;
            service = new Authentication(dbContext,
                this.dynamicEnvironment.AppSettings("PasswordAsymmetricEncryption").Equals("True", StringComparison.OrdinalIgnoreCase),
                this.dynamicEnvironment.AppSettings("PasswordAsymmetricEncryptionPrivateKey"));
            this.log = log;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dbContext.Close();
                dbContext.Dispose();
            }

            base.Dispose(disposing);
        }

        [HttpPost("WirePasswordHash")]
        [AllowAnonymous]
        public async Task<IActionResult> WirePasswordHashAsync([FromBody] AuthenticationSchemaRequestDto authenticationSchemaRequestDto)
        {
            if (!dynamicEnvironment.AppSettings("OAuthAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase)
                && !dynamicEnvironment.AppSettings("NegotiateAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase))
            {
                return Ok(new PasswordSchemeDto
                {
                    WirePasswordHash = authenticationSchemaRequestDto.UserName != null && await service.IsWirePasswordHashAsync(authenticationSchemaRequestDto.UserName)
                });
            }

            if (User.Identity is { IsAuthenticated: true })
            {
                Forbid();
            }

            return NotFound();

        }

        [HttpGet("ByNegotiate")]
        [Authorize(AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ByNegotiateAsync(CancellationToken token = default)
        {
            if (dynamicEnvironment.AppSettings("OAuthAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase)
                || !dynamicEnvironment.AppSettings("NegotiateAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase))
            {
                return NotFound();
            }

            var userAgent = contextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
            var localIp = contextAccessor.HttpContext?.Connection.LocalIpAddress?.ToString();

            if (!User.Identity?.IsAuthenticated ?? true)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("ByNegotiate: unauthenticated identity presented, rejecting.");
                }

                return Unauthorized();
            }

            try
            {
                if (User.Identity.Name != null)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"ByNegotiate: resolved identity '{User.Identity.Name}' from Negotiate, proceeding to AuthenticateByNegotiateAsync.");
                    }

                    await service.AuthenticateByNegotiateAsync(User.Identity.Name, localIp, userAgent, true, token);
                }
                else
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn("ByNegotiate: Negotiate authenticated but Identity.Name was null.");
                    }

                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                log.Error($"ByNegotiate: AuthenticateByNegotiateAsync failed for identity '{User.Identity.Name}'.", ex);

                return Forbid();
            }

            if (dynamicEnvironment.AppSettings("EnableMultifactorAuthentication").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(202);
            }

            var model = new AuthenticationRequestDto
            {
                UserName = User.Identity.Name
            };

            var authenticationDto = AuthenticationCookieIssuer.IssueAuthenticationCookies(Response, dynamicEnvironment, model.UserName);
            return Ok(authenticationDto);
        }

        [HttpPost("ByNegotiateMfa")]
        [Authorize(AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ByNegotiateMfaAsync([FromBody] AuthenticationRequestDto model, CancellationToken token = default)
        {
            if (dynamicEnvironment.AppSettings("OAuthAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase)
                || !dynamicEnvironment.AppSettings("NegotiateAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase))
            {
                return NotFound();
            }

            model.UserAgent = contextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
            model.LocalIp = contextAccessor.HttpContext?.Connection.LocalIpAddress?.ToString();

            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Forbid();
            }

            try
            {
                if (User.Identity.Name != null)
                {
                    await service.AuthenticateByNegotiateAsync(User.Identity.Name, model.LocalIp, model.UserAgent, true, token);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception)
            {
                return Forbid();
            }

            if (dynamicEnvironment.AppSettings("EnableMultifactorAuthentication").Equals("True", StringComparison.OrdinalIgnoreCase)
                && User.Identity.Name != null)
            {
                if (String.IsNullOrEmpty(model.Mfa))
                {
                    return StatusCode(202);
                }

                var mfaResult = await VerifyMfaAsync(User.Identity.Name, model.Mfa, token);
                if (!mfaResult.IsSuccessful)
                {
                    return Unauthorized();
                }
            }

            model.UserName = User.Identity.Name;
            var authenticationDto = AuthenticationCookieIssuer.IssueAuthenticationCookies(Response, dynamicEnvironment, model.UserName);
            return Ok(authenticationDto);
        }

        [HttpPost("ByUserNamePassword")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthenticationResponseDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<AuthenticationResponseDto>> ByUserNamePasswordAsync(
            [FromBody] AuthenticationRequestDto model, CancellationToken token = default)
        {
            if (dynamicEnvironment.AppSettings("OAuthAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase)
                || dynamicEnvironment.AppSettings("NegotiateAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase))
            {
                return NotFound();
            }

            var validator = new AuthenticationRequestDtoValidator(dynamicEnvironment);
            var results = await validator.ValidateAsync(model, token);
            if (!results.IsValid)
            {
                return BadRequest(results);
            }

            try
            {
                model.UserAgent = contextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
                model.LocalIp = contextAccessor.HttpContext?.Connection.LocalIpAddress?.ToString();

                await service.AuthenticateByUserNamePasswordAsync(model, dynamicEnvironment.AppSettings("PasswordHashingKey"),
                    Int32.Parse(dynamicEnvironment.AppSettings("PasswordAttempts")), token);
            }
            catch (PasswordExpiredException)
            {
                return Forbid();
            }
            catch (PasswordNewMustChangeException)
            {
                return Forbid();
            }
            catch (PasswordStrengthException ex)
            {
                return BadRequest(new
                {
                    errors = ex.Errors
                        .Select(m => new
                        {
                            errorMessage = m
                        })
                        .ToArray()
                });
            }
            catch (Exception)
            {
                return Unauthorized();
            }

            var authenticatedUserName = User.Identity?.Name ?? model.UserName;
            
            if (String.IsNullOrEmpty(authenticatedUserName))
            {
                return Unauthorized();
            }

            if (dynamicEnvironment.AppSettings("EnableMultifactorAuthentication").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (String.IsNullOrEmpty(model.Mfa))
                {
                    return StatusCode(202);
                }

                var mfaResult = await VerifyMfaAsync(authenticatedUserName, model.Mfa, token);
                
                if (!mfaResult.IsSuccessful)
                {
                    return Unauthorized();
                }
            }

            var authenticationDto = AuthenticationCookieIssuer.IssueAuthenticationCookies(Response, dynamicEnvironment, authenticatedUserName);
            return Ok(authenticationDto);
        }

        [Authorize]
        [HttpPost("ChangePassword")]
        [ProducesResponseType(typeof(AuthenticationResponseDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequestDto model, CancellationToken token = default)
        {
            if (dynamicEnvironment.AppSettings("OAuthAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase)
                || dynamicEnvironment.AppSettings("NegotiateAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase))
            {
                return Forbid();
            }

            if (User.Identity?.Name == null)
            {
                return BadRequest("Authorized but the username is null.");
            }

            try
            {
                await service.ChangePasswordAsync(User.Identity.Name, model,
                    dynamicEnvironment.AppSettings("PasswordHashingKey"), token);
            }
            catch (BadCredentialsException)
            {
                return Unauthorized();
            }
            catch (PasswordStrengthException ex)
            {
                return BadRequest(new
                {
                    errors = ex.Errors
                        .Select(m => new
                        {
                            errorMessage = m
                        })
                        .ToArray()
                });
            }

            return Ok();
        }

        private Task<MfaVerificationResult> VerifyMfaAsync(string userName, string otp, CancellationToken token = default)
        {
            var outboundHttpRsaAmCertificateBypass =
                (dynamicEnvironment.AppSettings("OutboundHttpRsaAmCertificateBypass") ?? "False")
                .Equals("True", StringComparison.CurrentCultureIgnoreCase);

            var options = new RsaSecurIdOptions
            {
                Endpoint = dynamicEnvironment.AppSettings("MultifactorAuthenticationEndpoint"),
                ClientId = dynamicEnvironment.AppSettings("MultifactorAuthenticationApplicationId"),
                ClientKey = dynamicEnvironment.AppSettings("MultifactorAuthenticationClientKey"),
                OutboundHttpCertificateBypass = outboundHttpRsaAmCertificateBypass,
                OutboundHttpCertificateThumbprint = dynamicEnvironment.AppSettings("OutboundHttpRsaAmCertificateThumbprint")
            };

            IMfaProvider mfaProvider = new RsaSecurIdMfaProvider(options, log);

            var request = new MfaVerificationRequest
            {
                SubjectName = userName,
                Factors = [new MfaFactor { MethodId = "SECURID", Value = otp }]
            };

            return mfaProvider.VerifyAsync(request, token);
        }
    }
}
