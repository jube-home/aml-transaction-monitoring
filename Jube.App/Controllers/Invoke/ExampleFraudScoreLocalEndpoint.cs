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

namespace Jube.App.Controllers.Invoke
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using DynamicEnvironment;
    using FluentValidation.Results;
    using log4net;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;

    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ExampleFraudScoreLocalEndpointController : Controller
    {
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;

        public ExampleFraudScoreLocalEndpointController(ILog log, DynamicEnvironment dynamicEnvironment)
        {
            this.log = log;
            this.dynamicEnvironment = dynamicEnvironment;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<double>> ExampleFraudScoreLocalEndpointAsync()
        {
            try
            {
                if (!dynamicEnvironment.AppSettings("EnablePublicInvokeController")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound();
                }

                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info("Example FraudScore Local Endpoint Recall:  Recall received.");
                }

                var jObject = JObject.Parse(Encoding.UTF8.GetString(ms.ToArray()));

                var responseCodeVolumeRatio = jObject.SelectToken("$.ResponseCodeEqual0Volume");

                if (log.IsInfoEnabled)
                {
                    log.Info($"Example FraudScore Local Endpoint Recall:  Json parsed as {jObject}.  " +
                             "This endpoint will just echo back the sqrt of the ResponseCodeVolumeRatio element." +
                             " More typically this would be an R endpoint and it would recall a variety of models.");
                }

                if (responseCodeVolumeRatio != null)
                {
                    return Math.Sqrt(responseCodeVolumeRatio.ToObject<double>());
                }

                return 0;
            }
            catch (Exception ex)
            {
                log.Error(
                    $"Example FraudScore Local Endpoint Recall:  An error has been raised as {ex}.  Returning 500.");

                return StatusCode(500);
            }
        }
    }
}
