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
    using System.IdentityModel.Tokens.Jwt;
    using System.Net;
    using System.Threading.Tasks;
    using Code;
    using Data.Context;
    using DynamicEnvironment;
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
    public class SandboxRegistrationController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly SandboxRegistration service;

        public SandboxRegistrationController(DynamicEnvironment dynamicEnvironment)
        {
            this.dynamicEnvironment = dynamicEnvironment;
            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(
                    this.dynamicEnvironment.AppSettings("ConnectionString"));
            service = new SandboxRegistration(dbContext);
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

        [HttpPost("Register")]
        [ProducesResponseType(typeof(SandboxRegistrationResponseDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<AuthenticationResponseDto>> Register(
            [FromBody] SandboxRegistrationRequestDto model)
        {
            if (!dynamicEnvironment.AppSettings("EnableSandbox").Equals("True"))
            {
                return Unauthorized();
            }

            var validator = new SandboxRegistrationRequestDtoValidator();
            var results = await validator.ValidateAsync(model);
            if (!results.IsValid)
            {
                return BadRequest(results);
            }

            try
            {
                var sandboxRegistrationResponseDto =
                    await service.Register(model, dynamicEnvironment.AppSettings("PasswordHashingKey"));
                SetAuthenticationCookie(model);
                return Ok(sandboxRegistrationResponseDto);
            }
            catch (ConflictException)
            {
                return StatusCode(409);
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        private void SetAuthenticationCookie(SandboxRegistrationRequestDto model)
        {
            var token = Jwt.CreateToken(model.UserName,
                dynamicEnvironment.AppSettings("JWTKey"),
                dynamicEnvironment.AppSettings("JWTValidIssuer"),
                dynamicEnvironment.AppSettings("JWTValidAudience")
            );

            var expiration = DateTime.Now.AddMinutes(15);

            var authenticationDto = new AuthenticationResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = expiration
            };

            var cookieOptions = new CookieOptions
            {
                Expires = expiration,
                HttpOnly = true
            };

            Response.Cookies.Append("authentication",
                authenticationDto.Token, cookieOptions);
        }
    }
}
