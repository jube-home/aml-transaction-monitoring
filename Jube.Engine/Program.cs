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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive;
using Jube.Engine.Helpers;
using Jube.Engine.Invoke;
using Jube.Engine.Model.Processing;
using Jube.Engine.Model.Processing.CaseManagement;
using Jube.Engine.Model.Processing.Payload;
using Jube.Engine.Sanctions;
using log4net;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using EntityAnalysisModel = Jube.Engine.Model.EntityAnalysisModel;
using Tag = Jube.Engine.Model.Archive.Tag;

namespace Jube.Engine;

public class Program
{
    private readonly DefaultContractResolver _contractResolver;
    private readonly Dictionary<string, Assembly> _hashCacheAssembly = new();
    private readonly DynamicEnvironment.DynamicEnvironment _jubeEnvironment;
    private readonly ILog _log;
    private readonly ConcurrentQueue<EntityAnalysisModelInvoke> _pendingEntityInvoke;
    private readonly IModel _rabbitMqChannel;
    private readonly IDatabase _redisDatabase;
    private readonly Dictionary<int, SanctionEntryDto> _sanctionsEntries = new();
    private readonly Random _seeded;
    public readonly EntityAnalysisModelManager EntityAnalysisModelManager = new();
    public readonly ConcurrentQueue<Tag> PendingTagging = new();
    public readonly Dictionary<int, SanctionEntriesSource> SanctionSources = new();
    private Thread _asyncContextThread;
    private Thread _casesAutomationThread;
    private Thread _countersThread;
    private Thread _exhaustiveTrainingThread;
    private DateTime _lastBalanceCountersWritten;
    private DateTime _lastCallbackTimeout;
    private DateTime _lastHttpCountersWritten;
    private Thread _notificationThread;
    private Thread _sanctionsThread;
#pragma warning disable 649
    private bool _stopping; //Reserved for future use.
#pragma warning restore 649
    private Thread _taggingThread;

    // ReSharper disable once NotAccessedField.Local
    private Task _trainingTask;
    public bool EntityModelsHasLoadedForStartup;
    public int HttpCounterAllError;
    public int HttpCounterAllRequests;
    public int HttpCounterCallback;
    public int HttpCounterExhaustive;
    public int HttpCounterModel;
    public int HttpCounterModelAsync;
    public int HttpCounterSanction;
    public int HttpCounterTag;
    public ConcurrentQueue<Notification> PendingNotification;
    public bool SanctionsHasLoadedForStartup;

    public Program(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log, Random seeded,
        IModel rabbitMqChannel, IDatabase redisDatabase,
        ConcurrentQueue<EntityAnalysisModelInvoke> pendingEntityInvoke, DefaultContractResolver contractResolver)
    {
        _log = log;
        _jubeEnvironment = dynamicEnvironment;
        _seeded = seeded;
        _rabbitMqChannel = rabbitMqChannel;
        _redisDatabase = redisDatabase;
        _pendingEntityInvoke = pendingEntityInvoke;
        _contractResolver = contractResolver;

        _log.Info(
            "Start: Loading the Jube Environment Variables by instantiating the object.  Loaded the logging.");
    }

    private int PendingCallbacksTimeoutCounter { get; set; }

    // ReSharper disable once CollectionNeverQueried.Local
    private List<Task> AsyncHttpContextCorrelationTasks { get; set; }

    public void Start()
    {
        try
        {
            ConfigureThreadPool();
            StartSanctionsThread();
            SpinWaitAndConvergeSanctions();
            StartEntityModelServer();
            SpinWaitEntityModels();

            if (_rabbitMqChannel != null)
            {
                StartAmqp();
                StartNotificationsViaAmqp();
            }
            else
            {
                StartNotificationsViaConcurrentQueue();
            }

            StartCaseAutomationServer();
            StartAsyncEntityThreadsInLoop();
            StartCountersAndWarnings();
            StartTaggingStorage();
            StartExhaustiveTrainingServer();

            _log.Info("Start: The start routine has without error completed. Running.  Use cancel token to quit.");
        }
        catch (Exception ex)
        {
            _log.Error($"Start: {ex}");
        }
    }

    private void SpinWaitEntityModels()
    {
        var spinWait = new SpinWait();
        while (true)
            if (!EntityAnalysisModelManager.EntityModelsHasLoadedForStartup)
            {
                spinWait.SpinOnce();
            }
            else
            {
                EntityModelsHasLoadedForStartup = true;
                break;
            }
    }

    private void SpinWaitAndConvergeSanctions()
    {
        var spinWait = new SpinWait();
        while (true)
            if (SanctionsHasLoadedForStartup)
                break;
            else
                spinWait.SpinOnce();
    }

    private void StartSanctionsThread()
    {
        if (!_jubeEnvironment.AppSettings("EnableSanction")
                .Equals("True", StringComparison.OrdinalIgnoreCase)) return;

        _log.Debug("Start: Starting Sanctions routine.");

        ThreadStart startSanctionsThread = Sanctions;
        _sanctionsThread = new Thread(startSanctionsThread)
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };
        _sanctionsThread.Start();

        _log.Info("Start: Starting Sanctions routine.");
    }

    private void StartExhaustiveTrainingServer()
    {
        if (!_jubeEnvironment.AppSettings("EnableExhaustiveTraining")
                .Equals("True", StringComparison.OrdinalIgnoreCase)) return;

        _log.Debug("Start: Starting Exhaustive Training Server.");

        var training = new Training(_log, _seeded, _jubeEnvironment, _contractResolver);
        _trainingTask = Task.Run(training.StartAsync);

        _log.Info("Start: Starting Exhaustive Training Server.");
    }

    private void StartTaggingStorage()
    {
        _log.Debug("Start: Starting Tagging routine.");

        ThreadStart startTaggingThread = Tagging;
        _taggingThread = new Thread(startTaggingThread)
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };
        _taggingThread.Start();

        _log.Debug("Start: Started Tagging routine.");
    }

    private void StartCountersAndWarnings()
    {
        _log.Debug("Start: Starting Counters and Warnings routine.");

        ThreadStart startCountersThread = ManageCounters;
        _countersThread = new Thread(startCountersThread)
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };
        _countersThread.Start();

        _log.Info("Start: Started Counters and Warnings Thread in start routine.");
    }

    private void StartAsyncEntityThreadsInLoop()
    {
        var asyncThreads = int.Parse(_jubeEnvironment.AppSettings("ModelInvokeAsynchronousThreads"));
        AsyncHttpContextCorrelationTasks = [];
        for (var i = 1; i <= asyncThreads; i++)
        {
            _log.Debug($"Starting Async Context routine for thread {i}.");

            AsyncHttpContextCorrelationTasks.Add(Task.Run(AsyncHttpContextCorrelation));

            _log.Debug($"Started Async Context in start routine for thread {i}.");
        }
    }

    private void StartCaseAutomationServer()
    {
        if (!_jubeEnvironment.AppSettings("EnableCasesAutomation")
                .Equals("True", StringComparison.OrdinalIgnoreCase)) return;

        _log.Debug("Entity Start: Starting the Cases Automation Thread.");

        ThreadStart tsCasesAutomationServer = CasesAutomation;
        _casesAutomationThread = new Thread(tsCasesAutomationServer)
        {
            IsBackground = false,
            Priority = ThreadPriority.Normal
        };
        _casesAutomationThread.Start();

        _log.Debug("Entity Start: Started the Cases Automation Thread.");
    }

    private void NotificationRelayFromConcurrentQueue()
    {
        try
        {
            _log.Info("Notification Relay From Concurrent Queue: Will poll for new notifications.");

            while (!_stopping)
            {
                PendingNotification.TryDequeue(out var notification);
                if (notification != null)
                {
                    if (notification.NotificationTypeId == 1)
                    {
                        SendMail.Send(notification.NotificationDestination, notification.NotificationSubject,
                            notification.NotificationBody, _log,
                            _jubeEnvironment);

                        _log.Info(
                            $"Notification Dispatch: Sent via email Body: {notification.NotificationBody},Subject: {notification.NotificationSubject},Type:{notification.NotificationTypeId} and Destination {notification.NotificationDestination}.");
                    }
                    else
                    {
                        var clickatellString =
                            $"https://platform.clickatell.com/messages/http/send?apiKey={_jubeEnvironment.AppSettings("ClickatellAPIKey")}&to={HttpUtility.UrlEncode(notification.NotificationDestination?.Replace("+", "").Replace(" ", ""))}&content={HttpUtility.UrlEncode(notification.NotificationBody)}";

                        try
                        {
                            _log.Info(
                                $"Notification Dispatch: Is about to send Clickatell string of {clickatellString}.");

                            var client = new HttpClient();
                            var response = client.GetAsync(clickatellString);

                            var valueTask = Task.Run(() => response.Result.Content.ReadAsStringAsync());
                            valueTask.Wait();

                            _log.Info(
                                $"Notification Dispatch: Has sent Clickatell string of {clickatellString} and result of {valueTask.Result}.");
                        }
                        catch (Exception ex)
                        {
                            _log.Error(
                                $"Notification Dispatch: Has failed to send Clickatell string of {clickatellString} with error of {ex}.");
                        }
                    }
                }
                else
                {
                    _log.Debug(
                        "Notification Relay From Concurrent Queue: Nothing to relay.  Waiting for a second before trying again.");

                    Thread.Sleep(1000);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Debug($"Notification Relay From Concurrent Queue: Has experienced a fatal error of {ex}. " +
                       "Thread will exit.");
        }
    }

    private void StartNotificationsViaConcurrentQueue()
    {
        if (!_jubeEnvironment.AppSettings("EnableNotification")
                .Equals("True", StringComparison.OrdinalIgnoreCase)) return;

        _log.Debug("Entity Start: Starting the Notifications Thread.");

        PendingNotification = new ConcurrentQueue<Notification>();

        ThreadStart tsNotificationServer = NotificationRelayFromConcurrentQueue;
        _notificationThread = new Thread(tsNotificationServer)
        {
            IsBackground = false,
            Priority = ThreadPriority.Normal
        };
        _notificationThread.Start();

        _log.Debug("Entity Start: Started the Cases Automation Thread.");
    }

    private void StartEntityModelServer()
    {
        _log.Debug(
            $"Start: Checking if this is an entity model server, the conf332iguration key is set to {_jubeEnvironment.AppSettings("EnableEntityModel")}.");

        if (!_jubeEnvironment.AppSettings("EnableEntityModel")
                .Equals("True", StringComparison.OrdinalIgnoreCase)) return;

        _log.Info("Start: Starting the entity subsystem.");

        EntityAnalysisModelManager.Log = _log;
        EntityAnalysisModelManager.ContractResolver = _contractResolver;
        EntityAnalysisModelManager.HashCacheAssembly = _hashCacheAssembly;
        EntityAnalysisModelManager.PendingTagging = PendingTagging;
        EntityAnalysisModelManager.SanctionsEntries = _sanctionsEntries;
        EntityAnalysisModelManager.Seeded = _seeded;
        EntityAnalysisModelManager.JubeEnvironment = _jubeEnvironment;
        EntityAnalysisModelManager.RabbitMqChannel = _rabbitMqChannel;
        EntityAnalysisModelManager.RedisDatabase = _redisDatabase;
        EntityAnalysisModelManager.PendingNotification = PendingNotification;
        EntityAnalysisModelManager.Start();

        _log.Info("Start: Started the entity subsystem.");
    }

    private void ConfigureThreadPool()
    {
        if (_jubeEnvironment.AppSettings("ThreadPoolManualControl")
            .Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            ThreadPool.SetMinThreads(int.Parse(_jubeEnvironment.AppSettings("MinThreadPoolThreads")),
                int.Parse(_jubeEnvironment.AppSettings("MinThreadPoolThreads")));

            _log.Info(
                $"Start: Set the min threads to {_jubeEnvironment.AppSettings("MinThreadPoolThreads")} from the configuration file.");

            ThreadPool.SetMaxThreads(int.Parse(_jubeEnvironment.AppSettings("MaxThreadPoolThreads")),
                int.Parse(_jubeEnvironment.AppSettings("MaxThreadPoolThreads")));

            _log.Info(
                $"Start: Set the max threads to {int.Parse(_jubeEnvironment.AppSettings("MaxThreadPoolThreads"))} from the configuration file.");
        }
        else
        {
            _log.Info("Start: No manual thread pool parameters have been set.");
        }
    }

    private void Tagging()
    {
        try
        {
            var dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(
                    _jubeEnvironment.AppSettings("ConnectionString"));

            var repository = new ArchiveRepository(dbContext);

            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = _contractResolver
            };

            _log.Info("Tagging: Has created a db context and repository reference for the archive table. " +
                      "Will enter loop to look for new tags.");

            while (!_stopping)
            {
                PendingTagging.TryDequeue(out var tag);

                if (tag != null)
                {
                    try
                    {
                        _log.Info(
                            $"Tagging: Found {tag.EntityAnalysisModelInstanceEntryGuid} with Name {tag.Name} and Value {tag.Value}. Fetching the record.");

                        var archive = repository
                            .GetByEntityAnalysisModelInstanceEntryGuidAndEntityAnalysisModelId
                                (tag.EntityAnalysisModelInstanceEntryGuid, tag.EntityAnalysisModelId);

                        if (archive != null)
                        {
                            _log.Info(
                                $"Tagging: Found record for {tag.EntityAnalysisModelInstanceEntryGuid} and model {tag.EntityAnalysisModelId} with id {archive.Id}. " +
                                "Updating body.  Parsing Json.");

                            var jObject = JObject.Parse(archive.Json);

                            _log.Info(
                                $"Tagging: Found record for {tag.EntityAnalysisModelInstanceEntryGuid} and model {tag.EntityAnalysisModelId}.  Parsed Json. Finding tag by name.");

                            foreach (var (key, value) in jObject)
                            {
                                if (key != "tag") continue;

                                if (value != null)
                                {
                                    value[tag.Name] = tag.Value;

                                    _log.Info(
                                        $"Tagging: Found record for {tag.EntityAnalysisModelInstanceEntryGuid} and model {tag.EntityAnalysisModelId}. " +
                                        $"Has added tag {tag.Name} and value {tag.Value} to tag element. " +
                                        "Will update record.");

                                    var stream = new MemoryStream();
                                    var streamWriter = new StreamWriter(stream);
                                    var jsonWriter = new JsonTextWriter(streamWriter);

                                    serializer.Serialize(jsonWriter, jObject);
                                    jsonWriter.Flush();
                                    streamWriter.Flush();
                                    stream.Seek(0, SeekOrigin.Begin);

                                    archive.Json = Encoding.UTF8.GetString(stream.ToArray());

                                    repository.Update(archive);

                                    _log.Info(
                                        $"Tagging: Found record for {tag.EntityAnalysisModelInstanceEntryGuid} and model {tag.EntityAnalysisModelId}. " +
                                        "Has updated json in record.");
                                }

                                break;
                            }
                        }
                        else
                        {
                            _log.Info(
                                $"Tagging: No record for {tag.EntityAnalysisModelInstanceEntryGuid} and model {tag.EntityAnalysisModelId}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Info(
                            $"Tagging: Error on update for {tag.EntityAnalysisModelInstanceEntryGuid} and model {tag.EntityAnalysisModelId} as {ex}.");
                    }
                }
                else
                {
                    _log.Debug("Tagging: Nothing to tag.  Waiting for a second before trying again.");

                    Thread.Sleep(1000);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Tagging: Has experienced an error as {ex}.  The thread will halt and exit.");
        }
    }

    private void CasesAutomation()
    {
        try
        {
            while (!_stopping)
            {
                _log.Info("Start: Is building the database connection.");

                var dbContext =
                    DataConnectionDbContext.GetDbContextDataConnection(
                        _jubeEnvironment.AppSettings("ConnectionString"));
                try
                {
                    _log.Debug(
                        "Case Automation: Has opened the database connection for the case automation server.");

                    var expiredCases = GetExpiredCasesPending(dbContext);

                    foreach (var processExpiredCase in expiredCases)
                    {
                        _log.Info(
                            $"Case Automation: Is about to update case id {processExpiredCase.CaseId} with status of {processExpiredCase.CaseId}.");
                        try
                        {
                            UpdateCaseInDatabase(dbContext, processExpiredCase);

                            _log.Info(
                                $"Case Automation: Has updated case id {processExpiredCase.CaseId} with status of {processExpiredCase.CaseId}.  Will now create an audit event including the old value of {processExpiredCase.OldClosedStatus} and a case key of {processExpiredCase.CaseKey} and Case Key Value of {processExpiredCase.CaseKeyValue}.");

                            InsertCaseEvent(dbContext, processExpiredCase);

                            _log.Info(
                                $"Case Automation: Has created an audit event including the old value of {processExpiredCase.OldClosedStatus} and a case key of {processExpiredCase.CaseKey} and Case Key Value of {processExpiredCase.CaseKeyValue}.");
                        }
                        catch (Exception ex)
                        {
                            _log.Error(
                                $"Case Automation: Has created an error while processing expired cases as {ex} for case ID {processExpiredCase.CaseId} and new closed status {processExpiredCase.NewClosedStatus}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Case Automation: Has created an error inside the loop {ex}");
                }
                finally
                {
                    dbContext.Close();
                    dbContext.Dispose();

                    _log.Debug("Case Automation: Is waiting.");

                    Thread.Sleep(int.Parse(_jubeEnvironment.AppSettings("CasesAutomationWait")));
                    _log.Debug("Case Automation: Is waiting.");
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Case Automation: Has created an error {ex}");
        }
    }

    private void InsertCaseEvent(DbContext dbContext, ExpiredCase processExpiredCase)
    {
        var repository = new CaseEventRepository(dbContext);

        var model = new CaseEvent
        {
            CaseKey = processExpiredCase.CaseKey,
            CaseKeyValue = processExpiredCase.CaseKeyValue,
            CreatedUser = "Administrator",
            CaseEventTypeId = 15,
            Before = processExpiredCase.OldClosedStatus.ToString(),
            After = processExpiredCase.NewClosedStatus.ToString(),
            CaseId = processExpiredCase.CaseId
        };

        repository.Insert(model);
    }

    private void UpdateCaseInDatabase(DbContext dbContext, ExpiredCase processExpiredCase)
    {
        var repository = new CaseRepository(dbContext);

        repository.UpdateExpiredCaseDiary(processExpiredCase.CaseId, processExpiredCase.NewClosedStatus,
            processExpiredCase.OldClosedStatus);
    }

    private IEnumerable<ExpiredCase> GetExpiredCasesPending(DbContext dbContext)
    {
        var repository = new CaseRepository(dbContext);

        _log.Debug("Case Automation: Has instantiated the command object to return all expired cases.");

        var records = repository.GetByExpired();

        _log.Debug("Case Automation: Has executed a reader to return all expired cases.");

        var expiredCases = new List<ExpiredCase>();
        foreach (var record in records)
            try
            {
                var expiredCase = new ExpiredCase
                {
                    CaseId = record.Id
                };

                if (record.CaseKey != null)
                    expiredCase.CaseKey = record.CaseKey;

                if (record.CaseKeyValue != null)
                    expiredCase.CaseKeyValue = record.CaseKeyValue;

                if (record.ClosedStatusId.HasValue)
                {
                    expiredCase.OldClosedStatus = record.ClosedStatusId.Value;

                    switch (expiredCase.OldClosedStatus)
                    {
                        case 0:
                            expiredCase.NewClosedStatus = 0;

                            _log.Info(
                                $"Case Automation: Case ID {expiredCase.CaseId} has an open status and will be maintained.");
                            break;
                        case 1:
                            expiredCase.NewClosedStatus = 0;

                            _log.Info(
                                $"Case Automation: Case ID {expiredCase.CaseId} has a Suspend Open status and will be changed to Open.");
                            break;
                        case 2:
                            expiredCase.NewClosedStatus = 3;

                            _log.Info(
                                $"Case Automation: Case ID {expiredCase.CaseId} has a Suspend Close status and will be changed to Closed.");
                            break;
                        case 4:
                            expiredCase.NewClosedStatus = 3;

                            _log.Info(
                                $"Case Automation: Case ID {expiredCase.CaseId} has a Suspend Bypass status and will be changed to Closed.");
                            break;
                    }
                }
                else
                {
                    expiredCase.NewClosedStatus = 0;

                    _log.Info(
                        $"Case Automation: Case ID {expiredCase.CaseId} has a missing Close status and will be changed to Open.");
                }

                expiredCases.Add(expiredCase);
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"Case Automation: Has created an error while processing expired cases as {ex}");
            }

        _log.Debug(
            "Case Automation: Has closed the reader of expired cases and will now process them.");

        return expiredCases;
    }

    private async Task AsyncHttpContextCorrelation()
    {
        while (!_stopping)
            try
            {
                if (_pendingEntityInvoke.TryDequeue(out var payload))
                {
                    _log.Info(
                        $"Async Http Context Correlation: Found Async with guid of {payload.EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid}.  Is about to start.");

                    await payload.Start().ConfigureAwait(false);

                    _log.Info(
                        $"Async Http Context Correlation: Finished Async with guid of {payload.EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid}.");
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Async Http Context Correlation: Asynchronous processing error as {ex}");

                Thread.Sleep(100);
            }
    }

    private void Sanctions()
    {
        try
        {
            while (!_stopping)
            {
                var dbContext =
                    DataConnectionDbContext.GetDbContextDataConnection(
                        _jubeEnvironment.AppSettings("ConnectionString"));

                try
                {
                    _log.Debug(
                        "Sanctions Cache Loader: Has opened the database connection for retrieving the Sanctions Cache.");

                    var sanctionEntriesSources = GetSanctionsSources(dbContext);
                    LoadSanctionsEntries(dbContext);
                    SanctionsHasLoadedForStartup = true;
                    LoadSanctionsFromFiles(sanctionEntriesSources, dbContext);
                }
                catch (Exception ex)
                {
                    _log.Error($"Sanctions Cache Loader: Error {ex}");
                }
                finally
                {
                    dbContext.Close();
                    dbContext.Dispose();

                    Thread.Sleep(int.Parse(_jubeEnvironment.AppSettings("SanctionLoaderWait")));
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Sanctions Cache Loader: Error{ex}");
        }
    }

    private void LoadSanctionsEntries(DbContext dbContext)
    {
        var repository = new SanctionsEntryRepository(dbContext);

        _log.Debug(
            "Sanctions Cache Loader: Has instantiated the command object to return all Entries from the Sanctions Cache.");

        var records = repository.Get();

        _log.Debug(
            "Sanctions Cache Loader: Has executed a reader to return all entries from the Sanctions Cache.");

        foreach (var record in records)
            try
            {
                if (_sanctionsEntries.ContainsKey(record.Id)) continue;

                var sanctionEntry = new SanctionEntryDto
                {
                    SanctionEntrySourceId = record.SanctionEntrySourceId ?? 0,
                    SanctionEntryReference = record.SanctionEntryReference ?? "NA"
                };

                var sanctionPayloadStrings =
                    record.SanctionEntryElementValue
                        .Split([" "], StringSplitOptions.RemoveEmptyEntries);

                for (var i = 0; i < sanctionPayloadStrings.Length; i++)
                    sanctionPayloadStrings[i] =
                        LevenshteinDistance.Clean(sanctionPayloadStrings[i]);

                sanctionEntry.SanctionElementValue = sanctionPayloadStrings;

                sanctionEntry.SanctionEntryId = record.Id;

                _sanctionsEntries.Add(sanctionEntry.SanctionEntryId, sanctionEntry);
            }
            catch (Exception ex)
            {
                _log.Error($"Sanctions Cache Loader: Error loading a hash value {ex}");
            }
    }

    private void LoadSanctionsFromFiles(IEnumerable<SanctionEntriesSource> sanctionEntriesSources,
        DbContext dbContext)
    {
        if (_jubeEnvironment.AppSettings("EnableSanctionLoader").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            var processSanctionEntriesSources = sanctionEntriesSources.ToList();
            foreach (var processSanctionEntriesSource in
                     processSanctionEntriesSources.Where(processSanctionEntriesSource =>
                         processSanctionEntriesSource.EnableHttpLocation))
                try
                {
                    var client = new HttpClient();
                    var response = client.GetAsync(processSanctionEntriesSource.HttpLocation);
                    var valueTask = Task.Run(() => response.Result.Content.ReadAsStreamAsync());
                    valueTask.Wait();

                    _log.Info($"Sanctions Loader:  HTTP request result is {valueTask.Result}.");

                    var tfp = new TextFieldParser(valueTask.Result)
                    {
                        Delimiters = [processSanctionEntriesSource.Delimiter]
                    };

                    _log.Info(
                        $"Sanctions Loader: Has made a connection to {processSanctionEntriesSource.HttpLocation} has downloaded data and opened it using the Text Field Parser.");

                    ProcessTextFieldParser(dbContext, tfp, processSanctionEntriesSource,
                        processSanctionEntriesSource.Skip);

                    _log.Info(
                        $"Sanctions Loader: Has made a connection to {processSanctionEntriesSource.HttpLocation} has finished using the Text Field Parser.");
                }
                catch (Exception ex)
                {
                    _log.Info(
                        $"Sanctions Loader: Has made a connection to {processSanctionEntriesSource.HttpLocation} has created an error as {ex}.");
                }

            foreach (var processSanctionEntriesSource in processSanctionEntriesSources)
                if (Directory.Exists(processSanctionEntriesSource.DirectoryLocation)
                    && processSanctionEntriesSource.EnableDirectoryLocation)
                {
                    var files = Directory.GetFiles(processSanctionEntriesSource.DirectoryLocation);
                    foreach (var fileWithinLoop in files)
                        try
                        {
                            _log.Info(
                                "Sanctions Loader: Has loaded the database connection. Will now try and open it using the Text Field Parser.");

                            var tfp = new TextFieldParser(fileWithinLoop)
                            {
                                Delimiters = [processSanctionEntriesSource.Delimiter]
                            };

                            ProcessTextFieldParser(dbContext, tfp, processSanctionEntriesSource,
                                processSanctionEntriesSource.Skip);

                            _log.Info(
                                "Sanctions Loader: Has finished looping through the Sanctions and has closed the database connection and the file.");

                            _log.Info($"Sanctions Loader: Is about to delete {fileWithinLoop}.");

                            File.Delete(fileWithinLoop);

                            _log.Info($"Sanctions Loader: Has deleted {fileWithinLoop}.");
                        }
                        catch (Exception ex)
                        {
                            _log.Info($"Sanctions Loader: Error loading record {ex}");
                        }
                }
                else
                {
                    _log.Info(
                        $"Sanctions Loader: Directory does not exist {processSanctionEntriesSource.DirectoryLocation} for {processSanctionEntriesSource.SanctionEntrySourceId}.");
                }
        }
        else
        {
            _log.Info("Sanctions Loader: Sanctions loading is disabled on this server.");
        }
    }

    private void ProcessTextFieldParser(DbContext dbContext, TextFieldParser tfp,
        SanctionEntriesSource processSanctionEntriesSource, int skip)
    {
        _log.Info(
            $"Sanctions Loader: Has loaded the database connection.  Has set the delimiter to {processSanctionEntriesSource.Delimiter}.  Is about to start processing the records.");
        tfp.TextFieldType = FieldType.Delimited;
        var i = 1;
        while (tfp.EndOfData == false)
            try
            {
                _log.Info(
                    $"Sanctions Loader: Has loaded the database connection.  Is processing record {i}.  Will now build the SQL Command object.");

                var data = tfp.ReadFields();

                if (i > skip)
                {
                    if (data.Length > 1)
                    {
                        var repository = new SanctionsEntryRepository(dbContext);

                        var sanctionEntry = new SanctionEntryDto();
                        var insert = new SanctionEntry();

                        var sb = new StringBuilder();
                        var first = true;
                        foreach (var sanctionSourceElementLocation in
                                 processSanctionEntriesSource.MultiPartStringIndex.Split(
                                     ",".ToCharArray()))
                        {
                            if (first)
                                first = false;
                            else
                                sb.Append(' ');

                            int.TryParse(sanctionSourceElementLocation, out var parsedInt);
                            sb.Append(data[parsedInt]);
                        }

                        insert.SanctionEntryElementValue = sb.ToString();
                        insert.SanctionEntrySourceId = processSanctionEntriesSource.SanctionEntrySourceId;
                        insert.SanctionPayload = string.Join(',', data);
                        insert.SanctionEntryReference = data[processSanctionEntriesSource.ReferenceIndex];

                        var hashValue = Hash.GetHash(
                            processSanctionEntriesSource.SanctionEntrySourceId +
                            sb.ToString() +
                            data[processSanctionEntriesSource.ReferenceIndex]);

                        insert.SanctionEntryHash = hashValue;
                        insert.SanctionEntrySourceId = processSanctionEntriesSource.SanctionEntrySourceId;

                        insert = repository.Upsert(insert);

                        if (!_sanctionsEntries.ContainsKey(insert.Id))
                        {
                            var sanctionPayloadStrings =
                                sb.ToString()
                                    .Split([" "], StringSplitOptions.RemoveEmptyEntries);

                            for (var j = 0; j < sanctionPayloadStrings.Length; j++)
                                sanctionPayloadStrings[j] =
                                    LevenshteinDistance.Clean(sanctionPayloadStrings[j]);

                            sanctionEntry.SanctionEntrySourceId =
                                processSanctionEntriesSource.SanctionEntrySourceId;

                            sanctionEntry.SanctionEntryReference =
                                !string.IsNullOrEmpty(data[processSanctionEntriesSource.ReferenceIndex])
                                    ? data[processSanctionEntriesSource.ReferenceIndex]
                                    : "NA";

                            sanctionEntry.SanctionElementValue = sanctionPayloadStrings;
                            sanctionEntry.SanctionEntryId = insert.Id;

                            _sanctionsEntries.Add(insert.Id, sanctionEntry);

                            _log.Info(
                                $"Sanctions Loader: Has loaded records with value of {sb} for source {processSanctionEntriesSource.SanctionEntrySourceId} with reference of {data[processSanctionEntriesSource.ReferenceIndex]} and a hash value of {hashValue}.");
                        }
                        else
                        {
                            _log.Info(
                                $"Sanctions Loader: Has not reloaded records with value of {sb} for source {processSanctionEntriesSource.SanctionEntrySourceId} with reference of {data[processSanctionEntriesSource.ReferenceIndex]} and a hash value of {hashValue} as already exists.");
                        }
                    }
                    else
                    {
                        _log.Info($"Sanctions Loader: record {i} has no data.");
                    }
                }
                else
                {
                    _log.Info(
                        $"Sanctions Loader: Skipped header row {i}");
                }
            }
            catch (Exception ex)
            {
                _log.Info($"Sanctions Loader: Error loading record {ex}");
            }
            finally
            {
                i += 1;
                _log.Info($"Sanctions Loader: Moving to record {i}.");
            }

        tfp.Close();
    }

    private IEnumerable<SanctionEntriesSource> GetSanctionsSources(DbContext dbContext)
    {
        var repository = new SanctionsEntriesSourcesRepository(dbContext);

        _log.Debug(
            "Sanctions Cache Loader: Has instantiated the command object to return all Sources for the Sanctions Cache.");

        var records = repository.Get();

        _log.Debug(
            "Sanctions Cache Loader: Has executed a reader to return all Sources for the Sanctions Cache.");

        var sanctionEntriesSources = new List<SanctionEntriesSource>();

        foreach (var record in records)
            try
            {
                SanctionEntriesSource sanctionEntriesSource;

                if (!SanctionSources.TryGetValue(record.Id, out var source))
                {
                    sanctionEntriesSource = new SanctionEntriesSource
                    {
                        SanctionEntrySourceId = record.Id
                    };
                    SanctionSources.Add(record.Id, sanctionEntriesSource);
                }
                else
                {
                    sanctionEntriesSource = source;
                }

                sanctionEntriesSource.Name = record.Name ?? "";

                if (record.Severity.HasValue)
                {
                }

                if (record.EnableHttpLocation != null)
                    sanctionEntriesSource.EnableHttpLocation = record.EnableHttpLocation == 1;
                else
                    sanctionEntriesSource.EnableHttpLocation = false;

                if (record.EnableDirectoryLocation.HasValue)
                    sanctionEntriesSource.EnableDirectoryLocation = record.EnableDirectoryLocation == 1;
                else
                    sanctionEntriesSource.EnableDirectoryLocation = false;

                if (record.DirectoryLocation != null)
                    sanctionEntriesSource.DirectoryLocation = record.DirectoryLocation;

                if (record.HttpLocation != null)
                    sanctionEntriesSource.HttpLocation = record.HttpLocation;

                sanctionEntriesSource.Delimiter =
                    record.Delimiter.HasValue ? record.Delimiter.Value.ToString() : ",";

                sanctionEntriesSource.Skip = record.Skip ?? 0;

                if (record.MultiPartStringIndex != null)
                    sanctionEntriesSource.MultiPartStringIndex = record.MultiPartStringIndex;

                if (record.ReferenceIndex.HasValue)
                    sanctionEntriesSource.ReferenceIndex = record.ReferenceIndex.Value;

                sanctionEntriesSource.SanctionEntrySourceId = record.Id;

                sanctionEntriesSources.Add(sanctionEntriesSource);
            }
            catch (Exception ex)
            {
                _log.Error($"Sanctions Cache Loader: has created an error as {ex}.");
            }

        return sanctionEntriesSources;
    }

    private void StartNotificationsViaAmqp()
    {
        if (!_jubeEnvironment.AppSettings("EnableNotification")
                .Equals("True", StringComparison.OrdinalIgnoreCase)) return;

        _log.Debug("Notification Dispatch: Starting the Notification Dispatch Routine.");

        var consumer = new EventingBasicConsumer(_rabbitMqChannel);
        consumer.Received += (_, ea) =>
        {
            try
            {
                _log.Info("Notification Dispatch: Message Received.");

                var bodyString = Encoding.UTF8.GetString(ea.Body.ToArray());

                _log.Info("Notification Dispatch: String representation of body received is " + bodyString +
                          " .");

                var json = JObject.Parse(bodyString);
                var notificationBody = json["NotificationBody"]?.ToString();
                var notificationSubject = json["NotificationSubject"]?.ToString();
                var notificationType = Convert.ToInt32(json["NotificationType"]);
                var notificationDestination = json["NotificationDestination"]?.ToString();

                _log.Info(
                    $"Notification Dispatch: Message parsed as Body: {notificationBody},Subject: {notificationSubject},Type:{notificationType} and Destination {notificationDestination}.");

                if (notificationType == 1) //'Email
                {
                    SendMail.Send(notificationDestination, notificationSubject, notificationBody, _log,
                        _jubeEnvironment);

                    _log.Info(
                        $"Notification Dispatch: Sent via email Body: {notificationBody},Subject: {notificationSubject},Type:{notificationType} and Destination {notificationDestination}.");
                }
                else
                {
                    var clickatellString =
                        $"https://platform.clickatell.com/messages/http/send?apiKey={_jubeEnvironment.AppSettings("ClickatellAPIKey")}&to={HttpUtility.UrlEncode(notificationDestination?.Replace("+", "").Replace(" ", ""))}&content={HttpUtility.UrlEncode(notificationBody)}";

                    try
                    {
                        _log.Info(
                            $"Notification Dispatch: Is about to send Clickatell string of {clickatellString}.");

                        var client = new HttpClient();
                        var response = client.GetAsync(clickatellString);

                        var valueTask = Task.Run(() => response.Result.Content.ReadAsStringAsync());
                        valueTask.Wait();

                        _log.Info(
                            $"Notification Dispatch: Has sent Clickatell string of {clickatellString} and result of {valueTask.Result}.");
                    }
                    catch (Exception ex)
                    {
                        _log.Error(
                            $"Notification Dispatch: Has failed to send Clickatell string of {clickatellString} with error of {ex}.");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"Notification Dispatch: Exception created on consumer for notification dispatch {ex}.");
            }
        };
        _rabbitMqChannel.BasicConsume("jubeNotifications", true, consumer);
    }

    private void ManageCounters()
    {
        while (!_stopping)
            try
            {
                foreach (var (key, value) in
                         from modelEntityKvp in EntityAnalysisModelManager.ActiveEntityAnalysisModels
                         where modelEntityKvp.Value.LastCountersChecked.AddMilliseconds(10) < DateTime.Now
                         select modelEntityKvp)
                {
                    ClearResponseElevation(value);
                    ClearFrequency(value);
                    ClearActivationWatcher(value);
                    UpdateQueueBalancesInDatabaseAtModelLevel(value);
                    UpdateCountersInDatabase(value, key);
                }

                UpdateHttpCountersInDatabase();
                TimoutCallbacks();
                UpdateQueueBalancesInDatabase();
            }
            catch (Exception ex)
            {
                _log.Error($"Counter Management: An error in counter management has been observed as {ex}.");
            }
            finally
            {
                _log.Debug(
                    "Counter Management: Counters written.");

                Thread.Sleep(10);
            }
    }

    private void TimoutCallbacks()
    {
        if (_lastCallbackTimeout.AddMilliseconds(100) >= DateTime.Now) return;

        _log.Debug("Callback Timeout Management: Starting to inspect pending callbacks.");

        try
        {
            var callbackTimeout = int.Parse(_jubeEnvironment.AppSettings("CallbackTimeout")) * -1;

            var threshold = DateTime.Now.AddMilliseconds(callbackTimeout);

            _log.Debug(
                $"Callback Timeout Management: Threshold for timeout is {threshold} and it has been offset from now by {callbackTimeout} ms.");

            foreach (var pendingCallback in EntityAnalysisModelManager.PendingCallbacks)
            {
                if (pendingCallback.Value.CreatedDate >= threshold) continue;

                _log.Debug($"Callback Timeout Management: Expired callback {pendingCallback.Key} found.");

                EntityAnalysisModelManager.PendingCallbacks.TryRemove(pendingCallback);
                PendingCallbacksTimeoutCounter += 1;

                _log.Debug(
                    $"Callback Timeout Management: Expired callback {pendingCallback.Key} removed.  Counter incremented {PendingCallbacksTimeoutCounter}.");
            }

            _lastCallbackTimeout = DateTime.Now;
        }
        catch (Exception ex)
        {
            _log.Error(
                $"Callback Timeout Management: Has created an error trying to timeout callbacks {ex}.");
        }
    }

    private void UpdateQueueBalancesInDatabase()
    {
        if (_lastBalanceCountersWritten.AddSeconds(60) >= DateTime.Now) return;

        _log.Debug("Counter Management: Starting to store in memory asynchronous queues in database.");

        var dbContext =
            DataConnectionDbContext.GetDbContextDataConnection(
                _jubeEnvironment.AppSettings("ConnectionString"));
        try
        {
            var repository = new EntityAnalysisAsynchronousQueueBalanceRepository(dbContext);

            _log.Debug(
                "Counter Management: Has opened a database connection to invoke insertion of queue balance.");

            var model = new EntityAnalysisAsynchronousQueueBalance
            {
                AsynchronousInvoke = _pendingEntityInvoke.Count,
                AsynchronousCallback = EntityAnalysisModelManager.PendingCallbacks.Count,
                AsynchronousCallbackTimeout = PendingCallbacksTimeoutCounter,
                CreatedDate = DateTime.Now,
                Instance = Dns.GetHostName()
            };

            PendingCallbacksTimeoutCounter = 0;

            _log.Debug(
                $"Counter Management: has built command to invoke insert of queue balance as Tagging {PendingTagging.Count} and Node {_jubeEnvironment.AppSettings("Node")}.  Has reset expired counters.");

            repository.Insert(model);

            _lastBalanceCountersWritten = DateTime.Now;

            _log.Debug(
                "Counter Management: has updated HTTP processing counters.");
        }
        catch (Exception ex)
        {
            _log.Error(
                $"Counter Management: Invocation of queue balance insertion has an error as {ex}.");
        }
        finally
        {
            dbContext.Close();
            dbContext.Dispose();

            _log.Debug(
                "Counter Management: invocation of insert of HTTP processing counters has finished and the connection is closed.");
        }
    }

    private void UpdateHttpCountersInDatabase()
    {
        if (_lastHttpCountersWritten.AddSeconds(60) < DateTime.Now)
        {
            _log.Debug("Counter Management: Starting to store HTTP counters in the database.");

            var dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(
                    _jubeEnvironment.AppSettings("ConnectionString"));
            try
            {
                var repository = new HttpProcessingCounterRepository(dbContext);

                _log.Debug(
                    "Counter Management: Has opened a database connection to invoke Insert_HTTP_Processing_Counters.");

                var model = new HttpProcessingCounter
                {
                    Instance = Dns.GetHostName(),
                    All = HttpCounterAllRequests,
                    Model = HttpCounterModel,
                    AsynchronousModel = HttpCounterModelAsync,
                    Error = HttpCounterAllError,
                    Tag = HttpCounterTag,
                    Sanction = HttpCounterSanction,
                    Callback = HttpCounterCallback,
                    Exhaustive = HttpCounterExhaustive,
                    CreatedDate = DateTime.Now
                };

                _log.Debug(
                    "Counter Management: has built command for Insert_HTTP_Processing_Counters with All_Activity " +
                    $"{HttpCounterAllRequests},Models {HttpCounterModel}," +
                    $"Async Models {HttpCounterModelAsync},Tag {HttpCounterTag},Errors {HttpCounterAllError}," +
                    $"Exhaustive {HttpCounterExhaustive} and Sanction {HttpCounterSanction}.");

                repository.Insert(model);

                _lastHttpCountersWritten = DateTime.Now;

                _log.Debug(
                    "Counter Management: has updated HTTP processing counters.");
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"Counter Management: error created updating HTTP processing counters as {ex}.");
            }
            finally
            {
                dbContext.Close();
                dbContext.Dispose();

                _log.Debug(
                    "Counter Management: has updated HTTP processing counters and closed the database connection.");
            }

            HttpCounterAllRequests = 0;
            HttpCounterModel = 0;
            HttpCounterModelAsync = 0;
            HttpCounterAllError = 0;
            HttpCounterTag = 0;
            HttpCounterSanction = 0;
            HttpCounterCallback = 0;
            HttpCounterExhaustive = 0;

            _log.Debug("Counter Management: has updated HTTP processing counters reset.");

            _log.Debug(
                $"Counter Management: has updated HTTP processing counters last written date {_lastHttpCountersWritten}.");
        }
        else
        {
            _log.Debug(
                "Counter Management: Model has not stored HTTP counters as the storage period has not lapsed,  every 60 seconds.");
        }
    }

    private void UpdateCountersInDatabase(EntityAnalysisModel value, int key)
    {
        if (value.LastModelInvokeCountersWritten.AddSeconds(60) < DateTime.Now)
        {
            _log.Debug("Counter Management: Starting to store model counters in the database.");

            if (value.ModelInvokeCounter > 0)
            {
                _log.Debug(
                    $"Counter Management: Model {key} Starting to store counters in the database.");

                var dbContext =
                    DataConnectionDbContext.GetDbContextDataConnection(
                        _jubeEnvironment.AppSettings("ConnectionString"));
                try
                {
                    var repository = new EntityAnalysisModelProcessingCounterRepository(dbContext);

                    _log.Debug(
                        $"Counter Management: Model {key} Has opened a database connection to invoke Insert_Entity_Analysis_Models_Processing_Counters.");

                    var model = new EntityAnalysisModelProcessingCounter
                    {
                        ModelInvoke = value.ModelInvokeCounter,
                        GatewayMatch = value.ModelInvokeCounter,
                        ResponseElevation = value.ModelResponseElevationCounter,
                        ResponseElevationSum = value.ModelResponseElevationSum,
                        ResponseElevationValueLimit = value.ResponseElevationValueLimitCounter,
                        ResponseElevationLimit = value.ResponseElevationFrequencyLimitCounter,
                        ResponseElevationValueGatewayLimit = value.ResponseElevationValueGatewayLimitCounter,
                        ActivationWatcher = value.ActivationWatcherCount,
                        EntityAnalysisModelGuid = value.Guid,
                        CreatedDate = DateTime.Now,
                        Instance = Dns.GetHostName()
                    };

                    _log.Debug(
                        $"Counter Management: Model {key} Has built command for Insert_Entity_Analysis_Models_Processing_Counters with Model_Invoke_Counter {value.ModelInvokeCounter},Gateway_Match_Counter {value.ModelInvokeGatewayCounter},Response_Elevation_Counter {value.ModelResponseElevationCounter},Response_Elevation_Sum {value.ModelResponseElevationSum},Balance_Limit_Counter {value.BalanceLimitCounter},Response_Elevation_Value_Limit_Counter {value.ResponseElevationValueLimitCounter},Response_Elevation_Frequency_Limit_Counter {value.ResponseElevationFrequencyLimitCounter},Response_Elevation_Value_Gateway_Limit_Counter{value.ResponseElevationValueGatewayLimitCounter},Response_Elevation_Billing_Sum_Limit_Counter {value.ResponseElevationBillingSumLimitCounter},Parent_Response_Elevation_Value_Limit_Counter{value.ParentResponseElevationValueLimitCounter},Parent_Balance_Limit_Counter {value.ParentBalanceLimitCounter},{value.Id}.");

                    repository.Insert(model);

                    _log.Debug(
                        $"Counter Management: Model {key} Has opened a database connection to invoke Insert_Entity_Analysis_Models_Processing_Counters.");
                }
                catch (Exception ex)
                {
                    _log.Error(
                        $"Counter Management: Model {key} There was an error invoking Insert_Entity_Analysis_Models_Processing_Counters as {ex}.");
                }
                finally
                {
                    dbContext.Close();
                    dbContext.Dispose();

                    _log.Info(
                        $"Counter Management: Model {key} closed the database connection for invoking Insert_Entity_Analysis_Models_Processing_Counters.");
                }

                value.ModelInvokeCounter = 0;
                value.ModelInvokeGatewayCounter = 0;
                value.ModelResponseElevationCounter = 0;
                value.ModelResponseElevationSum = 0;
                value.BalanceLimitCounter = 0;
                value.ResponseElevationValueLimitCounter = 0;
                value.ResponseElevationFrequencyLimitCounter = 0;
                value.ResponseElevationValueGatewayLimitCounter = 0;
                value.ResponseElevationBillingSumLimitCounter = 0;
                value.ParentResponseElevationValueLimitCounter = 0;
                value.ActivationWatcherCount = 0;
                value.ParentBalanceLimitCounter = 0;

                _log.Debug(
                    $"Counter Management: Model {key} has reset all model counters.");
            }
            else
            {
                _log.Debug(
                    $"Counter Management: Model {key} nothing has been processed through the model.");
            }

            value.LastModelInvokeCountersWritten = DateTime.Now;

            _log.Debug(
                $"Counter Management: Model {key} updated last counters written {value.LastModelInvokeCountersWritten}.");
        }
        else
        {
            _log.Debug(
                $"Counter Management: Model {key} has not stored counters as the storage period has not lapsed,  every 60 seconds.");
        }

        value.LastCountersChecked = DateTime.Now;

        _log.Debug(
            $"Counter Management: Model {key} updated last counters checked {value.LastCountersChecked}.");
    }

    private void UpdateQueueBalancesInDatabaseAtModelLevel(EntityAnalysisModel value)
    {
        if (value.LastCountersWritten.AddSeconds(60) < DateTime.Now)
        {
            _log.Debug("Counter Management: Starting to store queue balances in the database.");

            var dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(
                    _jubeEnvironment.AppSettings("ConnectionString"));
            try
            {
                var repository = new EntityAnalysisModelAsynchronousQueueBalanceRepository(dbContext);

                _log.Debug(
                    "Counter Management: Has opened a database connection to invoke Insert_Entity_Analysis_Models_Asynchronous_Queue_Balances.");

                var insert = new EntityAnalysisModelAsynchronousQueueBalance
                {
                    Archive = value.PersistToDatabaseAsync.Count,
                    EntityAnalysisModelGuid = value.Guid,
                    ActivationWatcher = value.PersistToDatabaseAsync.Count,
                    CreatedDate = DateTime.Now,
                    Instance = Dns.GetHostName()
                };

                _log.Debug(
                    $"Counter Management: Has built the command object to invoke Insert_Entity_Analysis_Models_Asynchronous_Queue_Balances with Entity Analysis Model ID {value.Id}, Activation Cases 0, Activation Watcher {value.BillingResponseElevationBalanceEntries.Count}, Billing Response Elevation {value.ActivationWatcherCountJournal.Count},Billing_Response_Elevation_Balance {value.BillingResponseElevationJournal.Count}, Billing_Response_Elevation_Balance {value.BillingResponseElevationJournal.Count}, Node {_jubeEnvironment.AppSettings("Node")}.");

                repository.Insert(insert);

                _log.Debug(
                    "Counter Management: Has opened a database connection to invoke Insert_Entity_Analysis_Models_Asynchronous_Queue_Balances.");
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"Counter Management: There was an error invoking Insert_Entity_Analysis_Models_Asynchronous_Queue_Balances as {ex}.");
            }
            finally
            {
                dbContext.Close();
                dbContext.Dispose();

                _log.Debug(
                    "Counter Management: Closed the database connection for invoking Insert_Entity_Analysis_Models_Asynchronous_Queue_Balances.");
            }

            value.LastCountersWritten = DateTime.Now;

            _log.Debug(
                $"Counter Management: Updated last queue balances written {value.LastCountersWritten}.");
        }
        else
        {
            _log.Debug(
                "Counter Management: Has not stored queue balances as the storage period has not lapsed,  every 60 seconds.");
        }
    }

    private void ClearActivationWatcher(EntityAnalysisModel value)
    {
        _log.Debug(
            "Counter Management: Updating Counters.  Clearing Activation Watcher Count Journal queue.");

        while (value.ActivationWatcherCountJournal.TryPeek(out var activationWatcherDate))
            if (DateTime.Now > DateAndTime.DateAdd(value.MaxActivationWatcherInterval.ToString(),
                    value.MaxActivationWatcherValue, activationWatcherDate))
            {
                if (!value.ActivationWatcherCountJournal.TryDequeue(
                        out activationWatcherDate)) continue;

                value.ActivationWatcherCount -= 1;

                _log.Debug(
                    $"Counter Management: Updating Counters.  Has removed an entry from the Billing Response Elevation frequency Journal queue with activation watcher date of {activationWatcherDate} and decremented the Activation Watcher Count to {value.ActivationWatcherCount}.");
            }
            else
            {
                _log.Debug(
                    "Counter Management: Updating Counters.  No expired counters to be removed from Activation Watcher Count Journal queue.");

                break;
            }
    }

    private void ClearFrequency(EntityAnalysisModel value)
    {
        _log.Debug(
            "Counter Management: Updating Counters.  Clearing Billing Response Elevation frequency Journal queue.");

        while (value.BillingResponseElevationJournal.TryPeek(
                   out var responseElevationBalanceDate))
            if (DateTime.Now < DateAndTime.DateAdd(
                    value.MaxResponseElevationInterval.ToString(),
                    value.MaxResponseElevationValue, responseElevationBalanceDate))
            {
                if (!value.BillingResponseElevationJournal.TryDequeue(
                        out responseElevationBalanceDate)) continue;

                value.BillingResponseElevationCount -= 1;

                _log.Debug(
                    $"Counter Management: Updating Counters.  Has removed an entry from the Billing Response Elevation frequency Journal queue with response elevation date of {responseElevationBalanceDate} and decremented the Response Elevation Count to {value.BillingResponseElevationCount}.");
            }
            else
            {
                _log.Debug(
                    "Counter Management: Updating Counters.  No expired counters to be removed from Billing Response Elevation frequency Journal queue.");

                break;
            }
    }

    private void ClearResponseElevation(EntityAnalysisModel value)
    {
        _log.Debug(
            "Counter Management: Updating Counters.  Clearing Billing Response Elevation Balance Entries queue.");

        while (value.BillingResponseElevationBalanceEntries.TryPeek(
                   out var responseElevationBalanceEntry))
            if (DateTime.Now > DateAndTime.DateAdd(
                    value.MaxResponseElevationInterval.ToString(),
                    value.MaxResponseElevationValue,
                    responseElevationBalanceEntry.CreatedDate))
            {
                if (!value.BillingResponseElevationBalanceEntries.TryDequeue(
                        out responseElevationBalanceEntry)) continue;

                value.BillingResponseElevationBalance -=
                    responseElevationBalanceEntry.Value;

                _log.Debug(
                    $"Counter Management: Updating Counters.  Has removed an entry from the Billing Response Elevation Balance Entries queue with a response elevation of {responseElevationBalanceEntry.Value} and decremented the response elevation balance to {value.BillingResponseElevationBalance}.");
            }
            else
            {
                _log.Debug(
                    "Counter Management: Updating Counters.  No expired counters to be removed from Billing Response Elevation Balance Entries queue.");

                break;
            }
    }

    private void StartAmqp()
    {
        try
        {
            var consumer = new EventingBasicConsumer(_rabbitMqChannel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    _log.Info(
                        "AMQP Inbound:  Has received a message over the AMQP inbound queue.  Will now look for the EntityAnalysisModelGuid header.");

                    if (ea.BasicProperties.Headers != null)
                    {
                        _log.Info(
                            "AMQP Inbound:  Has received a message over the AMQP inbound queue.  Will now look for the EntityAnalysisModelGuid header.");

                        if (ea.BasicProperties.Headers.TryGetValue("EntityAnalysisModelGuid", out var header))
                        {
                            var entityAnalysisModelGuid =
                                Encoding.UTF8.GetString(
                                    (byte[])header);

                            var entityInstanceEntryPayloadStore = new EntityAnalysisModelInstanceEntryPayload
                                { EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid() };

                            EntityAnalysisModel entityAnalysisModel = null;

                            foreach (var (_, value) in
                                     from modelKvp in EntityAnalysisModelManager.ActiveEntityAnalysisModels
                                     where entityAnalysisModelGuid == modelKvp.Value.Guid.ToString()
                                     select modelKvp)
                            {
                                _log.Info(
                                    $"AMQP Inbound: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} matched for Requested Model GUID {entityAnalysisModelGuid}.  Model id is {value.Id}.");

                                entityAnalysisModel = value;
                                break;
                            }

                            if (entityAnalysisModel != null)
                            {
                                if (entityAnalysisModel.Started)
                                    try
                                    {
                                        _log.Info(
                                            $"AMQP Inbound: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} matched for Requested Model GUID {entityAnalysisModelGuid}.  Model invocation build is starting.");

                                        var entityModelInvoke = new EntityAnalysisModelInvoke(_log,
                                            _jubeEnvironment,
                                            _rabbitMqChannel,
                                            _redisDatabase,
                                            PendingNotification,
                                            _seeded,
                                            EntityAnalysisModelManager.ActiveEntityAnalysisModels);

                                        var inputStream =
                                            new MemoryStream(ea.Body.ToArray());

                                        _log.Info(
                                            $"AMQP Inbound: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} matched for Requested Model GUID {entityAnalysisModelGuid}.  Has built model invocation,  is about to invoke the model.");

                                        await entityModelInvoke.ParseAndInvoke(entityAnalysisModel, inputStream, false,
                                            inputStream.Length,
                                            null).ConfigureAwait(false);

                                        _log.Info(
                                            $"AMQP Inbound: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} matched for Requested Model GUID {entityAnalysisModelGuid}.  Has finished invoking the model,  will ACK on the AMQP.");

                                        _rabbitMqChannel.BasicAck(ea.DeliveryTag, false);

                                        _log.Info(
                                            $"AMQP Inbound: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} matched for Requested Model GUID {entityAnalysisModelGuid}.  Has finished invoking the model,  has sent ACK on the AMQP.");
                                    }
                                    catch (Exception ex)
                                    {
                                        _log.Info(
                                            $"AMQP Inbound: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} matched for Requested Model GUID {entityAnalysisModelGuid}.  Has created an error as {ex}.");

                                        _rabbitMqChannel.BasicReject(ea.DeliveryTag, false);
                                    }
                                else
                                    _rabbitMqChannel.BasicReject(ea.DeliveryTag, false);
                            }
                            else
                            {
                                _log.Info(
                                    "AMQP Inbound:  Has received a message over the AMQP inbound queue however the EntityAnalysisModelGuid header is not available in Active Models.");

                                _rabbitMqChannel.BasicReject(ea.DeliveryTag, false);
                            }
                        }
                        else
                        {
                            _log.Info(
                                "AMQP Inbound:  Has received a message over the AMQP inbound queue however the header EntityAnalysisModelGuid is null.");
                        }
                    }
                    else
                    {
                        _log.Info(
                            "AMQP Inbound:  Has received a message over the AMQP inbound queue however the header is null.");

                        _rabbitMqChannel.BasicReject(ea.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(
                        $"AMQP Inbound:  Has received experienced an error in the event consumer as {ex}.");
                }
            };

            _rabbitMqChannel.BasicConsume("jubeInbound", false, consumer);
        }
        catch (Exception ex)
        {
            _log.Error(ex.ToString());
        }
    }

    public IEnumerable<SanctionEntryReturn> HttpHandlerSanctions(string multipartString, int distance)
    {
        return LevenshteinDistance.CheckMultipartString(multipartString, distance, _sanctionsEntries);
    }

    public double ThreadPoolCallBackHttpHandlerExhaustive(Guid guid, JObject jObject)
    {
        double valueRecall = 0;
        try
        {
            _log.Info(
                $"Exhaustive Recall: GUID {guid} callback received for Exhaustive.");

            var foundExhaustive = false;

            try
            {
                foreach (var exhaustive in EntityAnalysisModelManager.ActiveEntityAnalysisModels.Select(model =>
                             model.Value.ExhaustiveModels
                                 .FirstOrDefault(w => w.Guid
                                                      == guid)).Where(exhaustive => exhaustive != null))
                {
                    foundExhaustive = true;

                    var scoreInputs = new double[exhaustive.NetworkVariablesInOrder.Count];
                    for (var i = 0; i < exhaustive.NetworkVariablesInOrder.Count; i++)
                    {
                        var scoreInput = exhaustive.NetworkVariablesInOrder[i];

                        _log.Info(
                            $"Exhaustive Recall: GUID {guid}" +
                            $" looking for match on {scoreInput.Name}.");

                        double valueElement;
                        try
                        {
                            var selectedToken = GetValueFromJson(jObject, scoreInput.Name);
                            if (selectedToken != null)
                            {
                                valueElement = Convert.ToDouble(selectedToken);

                                _log.Info(
                                    $"Exhaustive Recall: GUID {guid} " +
                                    $"has found a value in the payload for {scoreInput.Name} as {valueElement}.");

                                if (scoreInput.NormalisationTypeId == 2
                                   )
                                {
                                    valueElement = (valueElement - scoreInput.Mean) / scoreInput.Sd;

                                    _log.Info(
                                        $"Exhaustive Recall: GUID {guid}" +
                                        $" has a standardization type of 2 for {scoreInput.Name}" +
                                        $" and has been standardized to {valueElement} .");
                                }
                            }
                            else
                            {
                                _log.Info(
                                    $"Exhaustive Recall: GUID {guid}" +
                                    $" could not locate a match for {scoreInput.Name}.");

                                valueElement = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(
                                $"Exhaustive Recall: GUID {guid}" +
                                $" has produced an error on {scoreInput.Name} as {ex}.");

                            valueElement = 0;
                        }

                        scoreInputs[i] = valueElement;
                    }

                    valueRecall = exhaustive.TopologyNetwork.Compute(scoreInputs)[0];
                }
            }
            catch (Exception ex)
            {
                _log.Info(
                    $"Exhaustive Recall: GUID {guid} is in error as {ex} and has flushed an error message to the response stream.");

                throw;
            }

            if (!foundExhaustive)
            {
                _log.Info(
                    $"Exhaustive Recall: GUID {guid} could not find the adaptation and has flushed an error message to the response stream.");

                throw new KeyNotFoundException();
            }

            _log.Info(
                $"Exhaustive Recall: GUID {guid} has flushed the adaptation response to the response stream.");
        }
        catch (Exception ex)
        {
            _log.Error(
                $"Exhaustive Recall: GUID {guid} has caused an error as {ex}");
        }
        finally
        {
            _log.Info($"Exhaustive Recall: GUID {guid} has completed.");
        }

        return valueRecall;
    }

    private static JToken GetValueFromJson(JObject jObject, string name)
    {
        foreach (var (key, value) in jObject)
            if (key == name)
                return value;

        return null;
    }
}