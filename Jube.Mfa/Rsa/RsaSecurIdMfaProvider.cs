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

namespace Jube.Mfa.Rsa
{
    using System.Net.Http.Headers;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Text;
    using log4net;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Request;
    using Response;
    
    public sealed class RsaSecurIdMfaProvider : IMfaProvider
    {
        private const string AuthnAttemptIdMetadataKey = "AuthnAttemptId";
        private const string InResponseToMetadataKey = "InResponseTo";

        private static readonly MediaTypeHeaderValue JsonMediaType = new("application/json");

        private readonly string clientId;
        private readonly string clientKey;
        private readonly string endpoint;
        private readonly HttpClient httpClient;
        private readonly ILog log;

        public RsaSecurIdMfaProvider(RsaSecurIdOptions options, ILog log)
        {
            endpoint = options.Endpoint;
            clientKey = options.ClientKey;
            clientId = options.ClientId;
            this.log = log;
            httpClient = new HttpClient(CreateHandler(options.OutboundHttpCertificateBypass,
                options.OutboundHttpCertificateThumbprint));

            if (log.IsInfoEnabled)
            {
                log.Info($"MFA Instantiated ClientId: {clientId}");
            }
        }

        public async Task<MfaVerificationResult> VerifyAsync(MfaVerificationRequest request,
            CancellationToken cancellationToken = default)
        {
            var authenticationRequest = new AuthenticationRequest
            {
                ClientId = clientId,
                SubjectName = request.SubjectName,
                Context = BuildContext(request.Metadata),
                SubjectCredentials = request.Factors
                    .Select(factor => new SubjectCredentials
                    {
                        MethodId = factor.MethodId,
                        CollectedInputs =
                        [
                            new CollectedInputs
                            {
                                Name = factor.MethodId,
                                Value = factor.Value
                            }
                        ]
                    })
                    .ToList()
            };

            var json = JsonConvert.SerializeObject(authenticationRequest, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            if (log.IsInfoEnabled)
            {
                log.Info($"MFA ClientId: {clientId}; SubjectName: {request.SubjectName}; POST Request Json: {json}");
            }

            try
            {
                using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
                content.Headers.ContentType = JsonMediaType;

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                httpRequest.Content = content;
                httpRequest.Headers.TryAddWithoutValidation("client-key", clientKey);
                httpRequest.Headers.Accept.ParseAdd("*/*");

                using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    log.Info($"MFA ClientId: {clientId}; SubjectName: {request.SubjectName};" +
                             $" Http Status Code: {(int)response.StatusCode}; Response Json: {responseJson}.");

                    throw new HttpRequestException($"RSA AM returned {(int)response.StatusCode}: {responseJson}");
                }

                if (log.IsInfoEnabled)
                {
                    log.Info($"MFA ClientId: {clientId}; SubjectName: {request.SubjectName}; Response Json: {responseJson}");
                }

                var authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(responseJson);
                var isSuccessful = authenticationResponse?.AttemptResponseCode == "SUCCESS";

                if (log.IsInfoEnabled)
                {
                    log.Info($"MFA ClientId: {clientId}; SubjectName: {request.SubjectName}; Valid: {isSuccessful}");
                }

                return new MfaVerificationResult
                {
                    IsSuccessful = isSuccessful,
                    ResponseCode = authenticationResponse?.AttemptResponseCode,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["AttemptReasonCode"] = authenticationResponse?.AttemptReasonCode
                    }
                };
            }
            catch (Exception ex)
            {
                log.Error($"MFA ClientId: {clientId}; SubjectName: {request.SubjectName}; Endpoint: {endpoint}; " +
                          $"VerifyAsync failed. {Describe(ex)}", ex);
                throw;
            }
        }

        private static Context BuildContext(IReadOnlyDictionary<string, object?> metadata)
        {
            var context = new Context();

            if (metadata.TryGetValue(AuthnAttemptIdMetadataKey, out var authnAttemptId) && authnAttemptId is not null)
            {
                context.AuthnAttemptId = authnAttemptId.ToString()!;
            }

            if (metadata.TryGetValue(InResponseToMetadataKey, out var inResponseTo) && inResponseTo is not null)
            {
                context.InResponseTo = inResponseTo.ToString()!;
            }

            return context;
        }

        private static HttpClientHandler CreateHandler(bool outboundHttpCertificateBypass,
            string? outboundHttpCertificateThumbprint)
        {
            var handler = new HttpClientHandler();

            if (outboundHttpCertificateBypass)
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }
            else if (!String.IsNullOrWhiteSpace(outboundHttpCertificateThumbprint))
            {
                handler.ServerCertificateCustomValidationCallback = (_, cert, _, errors) =>
                {
                    if (errors == SslPolicyErrors.None)
                    {
                        return true;
                    }

                    return errors == SslPolicyErrors.RemoteCertificateChainErrors
                           && cert is not null
                           && String.Equals(cert.GetCertHashString(), outboundHttpCertificateThumbprint,
                               StringComparison.OrdinalIgnoreCase);
                };
            }

            return handler;
        }

        private static string Describe(Exception ex)
        {
            var sb = new StringBuilder();
            for (var e = ex; e is not null; e = e.InnerException)
            {
                sb.Append(e.GetType().Name).Append(": ").Append(e.Message);
                switch (e)
                {
                    case HttpRequestException hre: sb.Append($" [{hre.HttpRequestError}, Status={hre.StatusCode}]"); break;
                    case SocketException se: sb.Append($" [SocketError={se.SocketErrorCode}]"); break;
                    case AuthenticationException: sb.Append(" [TLS handshake]"); break;
                }
                sb.Append(" --> ");
            }
            return sb.ToString();
        }
    }
}
