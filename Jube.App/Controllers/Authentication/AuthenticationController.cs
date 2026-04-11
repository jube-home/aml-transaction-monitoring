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
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Code;
    using Data.Context;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Service.Authentication;
    using Service.Dto.Authentication;
    using Service.Exceptions.Authentication;
    using Validations.Authentication;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class AuthenticationController : Controller
    {
        private readonly IHttpContextAccessor contextAccessor;

        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly Authentication service;

        public AuthenticationController(DynamicEnvironment dynamicEnvironment,
            IHttpContextAccessor contextAccessor, ILog log)
        {
            this.dynamicEnvironment = dynamicEnvironment;
            dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(this.dynamicEnvironment.AppSettings("ConnectionString"), log);
            this.contextAccessor = contextAccessor;
            service = new Authentication(dbContext);
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

        [HttpPost("ByUserNamePassword")]
        [ProducesResponseType(typeof(AuthenticationResponseDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<AuthenticationResponseDto>> ByUserNamePasswordAsync(
            [FromBody] AuthenticationRequestDto model, CancellationToken token = default)
        {
            var validator = new AuthenticationRequestDtoValidator();
            var results = await validator.ValidateAsync(model, token);
            if (!results.IsValid)
            {
                return BadRequest(results);
            }

            try
            {
                model.UserAgent = contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
                model.LocalIp = contextAccessor.HttpContext?.Connection.LocalIpAddress?.ToString();
                model.UserAgent = Request.Headers.UserAgent.ToString();

                await service.AuthenticateByUserNamePasswordAsync(model, dynamicEnvironment.AppSettings("PasswordHashingKey"), token);
            }
            catch (PasswordExpiredException)
            {
                return Forbid();
            }
            catch (PasswordNewMustChangeException)
            {
                return Forbid();
            }
            catch (Exception)
            {
                return Unauthorized();
            }

            var authenticationDto = SetAuthenticationCookie(model);
            return Ok(authenticationDto);
        }

        private AuthenticationResponseDto SetAuthenticationCookie(AuthenticationRequestDto model)
        {
            var token = Jwt.CreateToken(model.UserName,
                dynamicEnvironment.AppSettings("JWTKey"),
                dynamicEnvironment.AppSettings("JWTValidIssuer"),
                dynamicEnvironment.AppSettings("JWTValidAudience")
            );

            var expiration = DateTime.UtcNow.AddMinutes(15);

            var authenticationDto = new AuthenticationResponseDto
            {
                Token = token,
                Expiration = expiration
            };

            var cookieOptions = new CookieOptions
            {
                Expires = expiration,
                HttpOnly = true
            };

            Response.Cookies.Append("authentication",
                authenticationDto.Token, cookieOptions);

            return authenticationDto;
        }

        [AllowAnonymous]
        [HttpPost("ChangePassword")]
        [ProducesResponseType(typeof(AuthenticationResponseDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequestDto model, CancellationToken token = default)
        {
            if (User.Identity == null)
            {
                return Ok();
            }

            var validator = new ChangePasswordRequestDtoValidator();

            var results = await validator.ValidateAsync(model, token);

            if (!results.IsValid)
            {
                return BadRequest(results);
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

            return Ok();
        }
    }
}
