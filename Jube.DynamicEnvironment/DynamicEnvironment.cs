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

namespace Jube.DynamicEnvironment
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using log4net;
    using log4net.Appender;
    using log4net.Config;
    using log4net.Core;
    using log4net.Layout;
    using log4net.Repository.Hierarchy;

    public class DynamicEnvironment
    {
        private readonly Dictionary<string, string> appSettings;

        public DynamicEnvironment()
        {
            appSettings = new Dictionary<string, string>
            {
                {
                    "ModelSynchronisationWait", "10000"
                },
                {
                    "EnableNotification", "True"
                },
                {
                    "EnableTtlCounter", "True"
                },
                {
                    "ConnectionString", null
                },
                {
                    "ReportConnectionString", null
                },
                {
                    "EnableSearchKeyCache", "True"
                },
                {
                    "EnableCasesAutomation", "True"
                },
                {
                    "CasesAutomationWait", "60000"
                },
                {
                    "EnableEntityModel", "True"
                },
                {
                    "ArchiverPersistThreads", "1"
                },
                {
                    "ModelInvokeAsynchronousThreads", "1"
                },
                {
                    "BulkCopyThreshold", "100"
                },
                {
                    "ActivationWatcherBulkCopyThreshold", "100"
                },
                {
                    "ReprocessingThreads", "1"
                },
                {
                    "ThreadPoolManualControl", "False"
                },
                {
                    "MinThreadPoolThreads", "40"
                },
                {
                    "MaxThreadPoolThreads", "1000"
                },
                {
                    "MaximumModelInvokeAsyncQueue", "10000"
                },
                {
                    "SMTPHost", null
                },
                {
                    "SMTPPort", "587"
                },
                {
                    "SMTPUser", null
                },
                {
                    "SMTPPassword", null
                },
                {
                    "SMTPFrom", null
                },
                {
                    "ClickatellAPIKey", null
                },
                {
                    "HttpAdaptationUrl", "http://localhost:5001"
                },
                {
                    "HttpAdaptationTimeout", "1000"
                },
                {
                    "HttpAdaptationValidateSsl", "False"
                },
                {
                    "ReprocessingBulkLimit", "10000"
                },
                {
                    "EnableMigration", "True"
                },
                {
                    "EnableSanction", "True"
                },
                {
                    "NegotiateAuthentication", "False"
                },
                {
                    "EnableExhaustiveTraining", "True"
                },
                {
                    "EnableReprocessing", "True"
                },
                {
                    "UseMockDataExhaustive", "True"
                },
                {
                    "SanctionLoaderWait", "60000"
                },
                {
                    "EnableSanctionLoader", "False"
                },
                {
                    "ActivationWatcherAllowPersist", "True"
                },
                {
                    "ActivationWatcherPersistThreads", "4"
                },
                {
                    "AMQP", "False"
                },
                {
                    "AMQPUri", null
                },
                {
                    "JWTValidAudience", "http://localhost:5001"
                },
                {
                    "JWTValidIssuer", "http://localhost:5001"
                },
                {
                    "JWTKey", null
                },
                {
                    "PasswordHashingKey", null
                },
                {
                    "PreservationSalt", null
                },
                {
                    "EnablePublicInvokeController", "True"
                },
                {
                    "EnableEngine", "True"
                },
                {
                    "ExhaustiveTrialsLimit", "1000"
                },
                {
                    "ExhaustiveMinVariableCount", "5"
                },
                {
                    "ExhaustiveMaxVariableCount", "30"
                },
                {
                    "ExhaustiveTrainingDataSamplePercentage", "0.6"
                },
                {
                    "ExhaustiveCrossValidationDataSamplePercentage", "0.2"
                },
                {
                    "ExhaustiveTestingDataSamplePercentage", "0.2"
                },
                {
                    "ExhaustiveValidationTestingActivationThreshold", "0.5"
                },
                {
                    "ExhaustiveTopologySinceImprovementLimit", "10"
                },
                {
                    "ExhaustiveLayerDepthLimit", "4"
                },
                {
                    "ExhaustiveLayerWidthLimitInputLayerFactor", "4"
                },
                {
                    "ExhaustiveTopologyComplexityLimit", "10000"
                },
                {
                    "ExhaustiveActivationFunctionExplorationEpochs", "3"
                },
                {
                    "ExhaustiveTopologyExplorationEpochs", "3"
                },
                {
                    "ExhaustiveTopologyFinalisationEpochs", "20"
                },
                {
                    "ExhaustiveSimulationsCount", "100"
                },
                {
                    "EnableCallback", "True"
                },
                {
                    "CallbackTimeout", "3000"
                },
                {
                    "StreamingActivationWatcher", "True"
                },
                {
                    "WaitPollFromActivationWatcherTable", "5000"
                },
                {
                    "WaitTtlCounterDecrement", "60000"
                },
                {
                    "RedisConnectionString", "localhost"
                },
                {
                    "RedisCommandFlag", "0"
                },
                {
                    "CachePruneServer", "True"
                },
                {
                    "WaitCachePrune", "10000"
                },
                {
                    "EnableSandbox", "False"
                },
                {
                    "Landlord", "True"
                },
                {
                    "Log4NetConfigFileLocationName", null
                },
                {
                    "Log4NetLogPath", null
                },
                {
                    "Log4NetLogMaximumFileSize", "100MB"
                },
                {
                    "Log4NetLogMaxSizeRollBackups", "100"
                },
                {
                    "Log4NetLogLevel", "ERROR"
                },
                {
                    "LocalCache", "True"
                },
                {
                    "LocalCacheFill", "True"
                },
                {
                    "LocalCacheBytes", "1073741824"
                },
                {
                    "RedisMessagePackCompression", "True"
                },
                {
                    "RedisStorePayloadCountsAndBytes", "True"
                },
                {
                    "RedisPublishSubscribeEvents", "False"
                }
            };
            
            foreach (var environmentVariable in from DictionaryEntry environmentVariable in
                         Environment.GetEnvironmentVariables()
                     where appSettings.ContainsKey(Convert.ToString(environmentVariable.Key) ?? String.Empty)
                     where Convert.ToString(environmentVariable.Value) !=
                           appSettings[environmentVariable.Key.ToString() ?? String.Empty]
                     select environmentVariable)
            {
                appSettings[environmentVariable.Key.ToString() ?? String.Empty] =
                    Convert.ToString(environmentVariable.Value);
            }

            InstantiateLog4Net();
            ValidateConnectionString();
            ValidateJwtKey();
            ValidatePasswordHashingKey();
        }

        public ILog Log { get; set; }

        private void InstantiateLog4Net()
        {
            Console.WriteLine(@"Console logging for instantiation of logging library:");
            Console.WriteLine();
            Console.WriteLine($@"Log4NetConfigFileLocationName:{Environment.GetEnvironmentVariable("Log4NetConfigFileLocationName")}");
            Console.WriteLine($@"Log4NetLogPath:{Environment.GetEnvironmentVariable("Log4NetLogPath")}");
            Console.WriteLine($@"Log4NetLogMaximumFileSize:{Environment.GetEnvironmentVariable("Log4NetLogMaximumFileSize")}");
            Console.WriteLine($@"Log4NetLogMaxSizeRollBackups:{Environment.GetEnvironmentVariable("Log4NetLogMaxSizeRollBackups")}");
            Console.WriteLine($@"Log4NetLogLevel:{Environment.GetEnvironmentVariable("Log4NetLogLevel")}");

            try
            {
                appSettings.TryGetValue("Log4NetConfigFileLocation", out var path);

                if (path != null)
                {
                    Console.WriteLine(
                        $@"Checking log4net XML serialisation file path {path} exists.");

                    var fileInfo = new FileInfo(path);
                    if (fileInfo.Exists)
                    {
                        try
                        {
                            Console.WriteLine(
                                $@"Configuring log4net XML serialisation from file path {path}.");

                            XmlConfigurator.Configure(fileInfo);

                            Console.WriteLine(
                                $@"Configured log4net XML serialisation from file path {path}.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                $@"A log4net configuration file path: {path} was provided but it failed to parse with error {ex}. Falling back to default logger configuration.");
                            ConfigureLoggerFromEnvironmentVariables();
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            $@"A log4net configuration file path: {path} was provided but no file exists. Falling back to default logger configuration.");

                        ConfigureLoggerFromEnvironmentVariables();
                    }
                }
                else
                {
                    ConfigureLoggerFromEnvironmentVariables();
                }

                Log = LogManager.GetLogger(typeof(ILog));
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $@"Failed to instantiate the logger with error {ex}.  No logging exists for the instance.");
            }
            Console.WriteLine(@"Logging instantiated.  Swapping to log4net instantiation for further logs.");
            Console.WriteLine();
        }

        private void ConfigureLoggerFromEnvironmentVariables()
        {
            var patternLayout = PatternLayout();
            var log4NetLevel = Log4NetLogLevel();
            ConfigureConsole(patternLayout, log4NetLevel);
            ConfigureRollingFileAppender(patternLayout, log4NetLevel);
            ((Hierarchy)LogManager.GetRepository()).Root.Level = Log4NetLogLevel();
        }

        private void ConfigureRollingFileAppender(PatternLayout patternLayout, Level log4NetLevel)
        {
            var log4NetLogFile = Log4NetLogFile();
            if (log4NetLogFile != null)
            {
                Console.WriteLine(@"Configuring software instantiation of log4net RollingFileAppender:");

                var rollingFileAppender = new RollingFileAppender
                {
                    File = log4NetLogFile,
                    Encoding = Encoding.UTF8,
                    Layout = patternLayout,
                    MaximumFileSize = Log4NetMaximumFileSize(),
                    MaxSizeRollBackups = Log4NetMaxSizeRollBackupsInt(),
                    StaticLogFileName = true,
                    RollingStyle = RollingFileAppender.RollingMode.Size,
                    AppendToFile = true,
                    Threshold = log4NetLevel
                };

                rollingFileAppender.ActivateOptions();

                BasicConfigurator.Configure(rollingFileAppender);

                Console.WriteLine(@"Configured software instantiation of log4net RollingFileAppender.");
            }
            else
            {
                Console.WriteLine(@"Log4NetLogPath not specified for RollingFileAppender.");
            }
        }

        private static void ConfigureConsole(PatternLayout patternLayout, Level log4NetLevel)
        {
            var consoleAppender = new ConsoleAppender
            {
                Layout = patternLayout,
                Threshold = log4NetLevel
            };
            consoleAppender.ActivateOptions();
            BasicConfigurator.Configure(consoleAppender);
        }

        private Level Log4NetLogLevel()
        {
            appSettings.TryGetValue("Log4NetLogLevel", out var log4NetLogLevel);
            return log4NetLogLevel switch
            {
                "ERROR" => Level.Error,
                "WARN" => Level.Warn,
                "INFO" => Level.Info,
                "DEBUG" => Level.Debug,
                _ => Level.Error
            };
        }

        private PatternLayout PatternLayout()
        {
            appSettings.TryGetValue("Log4NetPatternLayout", out var log4NetPatternLayout);
            var layout = new PatternLayout(log4NetPatternLayout ??
                                           "%date:%-5level:[%thread]:[%logger::%method]:%line:%message%newline");
            layout.ActivateOptions();
            return layout;
        }

        private string Log4NetMaximumFileSize()
        {
            appSettings.TryGetValue("Log4NetMaximumFileSize", out var log4NetMaximumFileSize);
            log4NetMaximumFileSize ??= "100MB";
            return log4NetMaximumFileSize;
        }

        private int Log4NetMaxSizeRollBackupsInt()
        {
            appSettings.TryGetValue("Log4NetMaxSizeRollBackups", out var log4NetMaxSizeRollBackups);
            if (!Int32.TryParse(log4NetMaxSizeRollBackups, out var log4NetMaxSizeRollBackupsInt))
            {
                log4NetMaxSizeRollBackupsInt = 100;
            }

            return log4NetMaxSizeRollBackupsInt;
        }

        private string Log4NetLogFile()
        {
            appSettings.TryGetValue("Log4NetLogPath", out var log4NetLogPath);

            return log4NetLogPath == null ? null : Path.Combine(log4NetLogPath, $"{Dns.GetHostName()}.log");
        }

        private void ValidateConnectionString()
        {
            if (String.IsNullOrEmpty(appSettings["ConnectionString"]))
            {
                throw new Exception("Missing ConnectionString in Environment Variables.");
            }
        }

        private void ValidatePasswordHashingKey()
        {
            if (String.IsNullOrEmpty(appSettings["PasswordHashingKey"]))
            {
                throw new Exception("Missing PasswordHashingKey in Environment Variables.");
            }
        }

        private void ValidateJwtKey()
        {
            if (String.IsNullOrEmpty(appSettings["JWTKey"]))
            {
                throw new Exception("Missing JWTKey in Environment Variables.");
            }
        }

        public string AppSettings(string[] keys)
        {
            return keys.Select(AppSettings).FirstOrDefault(value => value != null);
        }

        public string AppSettings(string key)
        {
            string value = null;
            if (appSettings.TryGetValue(key, out var setting))
            {
                value = setting;
            }

            return value;
        }
    }
}
