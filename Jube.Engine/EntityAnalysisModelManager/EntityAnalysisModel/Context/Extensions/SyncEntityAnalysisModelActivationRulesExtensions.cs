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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Data.Repository;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Jube.Engine.EntityAnalysisModelManager.Helpers;
    using Jube.Engine.Models;
    using Parser;
    using Parser.Compiler;

    public static class SyncEntityAnalysisModelActivationRulesExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelActivationRulesAsync(this Context context)
        {
            try
            {
                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Looping through active models {key} is started for the purpose adding the Activation Rules.");
                    }

                    var repository = new EntityAnalysisModelActivationRuleRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelActivationRuleRepository.GetByEntityAnalysisModelIdInPriorityOrder for entity model key of {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelIdInPriorityOrderAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowEntityModelActivationRule = new List<EntityAnalysisModelActivationRule>();
                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Activation Rule ID {record.Id} returned for model {key}.");
                            }

                            bool active;
                            if (record.Active == 1)
                            {
                                active = true;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Active value set as {active}.");
                                }
                            }
                            else
                            {
                                active = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and DEFAULT Activation Rule {record.Id} set Active value set as {active}.");
                                }
                            }

                            bool approval;
                            if (record.ReviewStatusId.HasValue)
                            {
                                switch (record.ReviewStatusId.Value)
                                {
                                    case 0:
                                        approval = false;

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 0 set as {approval}.");
                                        }

                                        break;
                                    case 1:
                                        approval = false;

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 1 set as {approval}.");
                                        }

                                        break;
                                    case 2:
                                        approval = false;

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 2 set as {approval}.");
                                        }

                                        break;
                                    case 3:
                                        approval = false;

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 3 set as {approval}.");
                                        }

                                        break;
                                    case 4:
                                        approval = true;

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 4 set as {approval}.");
                                        }

                                        break;
                                    default:
                                        approval = false;

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 0 set as {approval}.");
                                        }

                                        break;
                                }
                            }
                            else
                            {
                                approval = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set DEFAULT Approval value set as {approval}.");
                                }
                            }

                            if (!active || !approval)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {record.Id} is Active and Approved. Proceeding to build Activation Rule.");
                            }

                            var modelActivationRule = new EntityAnalysisModelActivationRule
                            {
                                Id = record.Id,
                                Guid = record.Guid
                            };

                            if (record.ReportTable.HasValue)
                            {
                                modelActivationRule.ReportTable =
                                    record.ReportTable.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Promote Report Table value set as {modelActivationRule.ReportTable}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.ReportTable = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Promote Report Table value set as {modelActivationRule.ReportTable}.");
                                }
                            }

                            if (record.Name != null)
                            {
                                modelActivationRule.Name = record.Name.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Name value set as {modelActivationRule.Name}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.Name =
                                    $"Activation_Rule_{modelActivationRule.Id}";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Name value set as {modelActivationRule.Name}.");
                                }
                            }

                            if (record.ResponsePayload.HasValue)
                            {
                                modelActivationRule.ResponsePayload = record.ResponsePayload == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Payload value set as {modelActivationRule.ResponsePayload}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.ResponsePayload = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Payload value set as {modelActivationRule.ResponsePayload}.");
                                }
                            }

                            if (record.Visible.HasValue)
                            {
                                modelActivationRule.Visible = record.Visible.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Visible value set as {modelActivationRule.Visible}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.Visible = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Visible value set as {modelActivationRule.Visible}.");
                                }
                            }

                            if (record.EnableReprocessing.HasValue)
                            {
                                modelActivationRule.EnableReprocessing = record.EnableReprocessing == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Reprocessing value set as {modelActivationRule.EnableReprocessing}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.EnableReprocessing = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Reprocessing value set as {modelActivationRule.EnableReprocessing}.");
                                }
                            }

                            if (record.ResponseElevationForeColor != null)
                            {
                                modelActivationRule.ResponseElevationForeColor = record.ResponseElevationForeColor;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation Fore Color value set as {modelActivationRule.ResponseElevationForeColor}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.ResponseElevationForeColor = "#000000";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation Fore Color value set as {modelActivationRule.ResponseElevationForeColor}.");
                                }
                            }

                            if (record.BypassSuspendInterval != null)
                            {
                                modelActivationRule.BypassSuspendInterval = record.BypassSuspendInterval.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Bypass Suspend Interval set as {modelActivationRule.BypassSuspendInterval}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.BypassSuspendInterval = 'd';

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Bypass Suspend Interval set as {modelActivationRule.BypassSuspendInterval}.");
                                }
                            }

                            if (record.BypassSuspendValue.HasValue)
                            {
                                modelActivationRule.BypassSuspendValue = record.BypassSuspendValue.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Bypass Suspend Value set as {modelActivationRule.BypassSuspendValue}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.BypassSuspendValue = 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Bypass Suspend Value set as {modelActivationRule.BypassSuspendValue}.");
                                }
                            }

                            if (record.BypassSuspendSample.HasValue)
                            {
                                modelActivationRule.BypassSuspendSample = record.BypassSuspendSample.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Bypass Suspend Sample set as {modelActivationRule.BypassSuspendSample}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.BypassSuspendSample = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Bypass Suspend Sample set as {modelActivationRule.BypassSuspendSample}.");
                                }
                            }

                            if (record.ResponseElevationBackColor != null)
                            {
                                modelActivationRule.ResponseElevationBackColor = record.ResponseElevationBackColor;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation Back Color value set as {modelActivationRule.ResponseElevationBackColor}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.ResponseElevationBackColor = "#ffffff";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation Back Color value set as {modelActivationRule.ResponseElevationBackColor}.");
                                }
                            }

                            if (record.EnableCaseWorkflow.HasValue)
                            {
                                modelActivationRule.EnableCaseWorkflow = record.EnableCaseWorkflow.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable Cases Workflow value set as {modelActivationRule.EnableCaseWorkflow}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.EnableCaseWorkflow = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable Cases Workflow value set as {modelActivationRule.EnableCaseWorkflow}.");
                                }
                            }

                            if (record.EnableNotification.HasValue)
                            {
                                modelActivationRule.EnableNotification = record.EnableNotification == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable Notification value set as {modelActivationRule.EnableNotification}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.EnableNotification = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable Notification value set as {modelActivationRule.EnableNotification}.");
                                }
                            }

                            if (record.NotificationTypeId.HasValue)
                            {
                                modelActivationRule.NotificationTypeId = record.NotificationTypeId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Type value set as {modelActivationRule.NotificationTypeId}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.NotificationTypeId = 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Type value set as {modelActivationRule.NotificationTypeId}.");
                                }
                            }

                            if (record.CaseKey != null)
                            {
                                modelActivationRule.CaseKey = record.CaseKey;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Case Key value set as {modelActivationRule.CaseKey}.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Case Key value set as {modelActivationRule.CaseKey}.");
                                }
                            }

                            if (record.ResponseElevationKey != null)
                            {
                                modelActivationRule.ResponseElevationKey = record.ResponseElevationKey;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation Key set as {modelActivationRule.ResponseElevationKey}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.ResponseElevationKey = value.References.EntryName;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation Key set as {modelActivationRule.ResponseElevationKey}.");
                                }
                            }

                            if (record.NotificationDestination != null)
                            {
                                modelActivationRule.NotificationDestination = record.NotificationDestination;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Destination value set as {modelActivationRule.NotificationDestination}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.NotificationDestination = "";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Destination value set as {modelActivationRule.NotificationDestination}.");
                                }
                            }

                            if (record.NotificationSubject != null)
                            {
                                modelActivationRule.NotificationSubject = record.NotificationSubject;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Subject value set as {modelActivationRule.NotificationSubject}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.NotificationSubject = "";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Subject value set as {modelActivationRule.NotificationSubject}.");
                                }
                            }

                            if (record.NotificationBody != null)
                            {
                                modelActivationRule.NotificationBody =
                                    HttpUtility.HtmlDecode(record.NotificationBody);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Body value set as {modelActivationRule.NotificationBody}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.NotificationBody = "";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Body value set as {modelActivationRule.NotificationBody}.");
                                }
                            }

                            if (record.SendToActivationWatcher.HasValue)
                            {
                                modelActivationRule.SendToActivationWatcher = record.SendToActivationWatcher.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Send To Activation Watcher value set as {modelActivationRule.SendToActivationWatcher}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.SendToActivationWatcher = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Send To Activation Watcher value set as {modelActivationRule.SendToActivationWatcher}.");
                                }
                            }

                            if (record.ResponseElevationContent != null)
                            {
                                modelActivationRule.ResponseElevationContent = record.ResponseElevationContent;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Content value set as {modelActivationRule.ResponseElevationContent}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.ResponseElevationContent = "";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT, not supplied, Response Content value set as {modelActivationRule.ResponseElevationContent}.");
                                }
                            }

                            if (record.ResponseElevationRedirect != null)
                            {
                                try
                                {
                                    var uri = new Uri(record.ResponseElevationRedirect);

                                    modelActivationRule.ResponseElevationRedirect = record.ResponseElevationRedirect;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set ResponseRedirect value set as {uri.AbsoluteUri} from original {modelActivationRule.ResponseElevationRedirect}.");
                                    }
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    modelActivationRule.ResponseElevationRedirect =
                                        value.Flags.FallbackResponseElevationRedirect;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set FAILED PARSE fallback, ResponseRedirect value set as {modelActivationRule.ResponseElevationRedirect} with exception message of {ex.Message}.");
                                    }
                                }

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Redirect value set as {modelActivationRule.ResponseElevationRedirect}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.ResponseElevationRedirect = value.Flags.FallbackResponseElevationRedirect;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT, not supplied, ResponseRedirect value set as {modelActivationRule.ResponseElevationRedirect}.");
                                }
                            }

                            if (record.ActivationSample.HasValue)
                            {
                                modelActivationRule.ActivationSample = record.ActivationSample.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Activation Sample value set as {modelActivationRule.ActivationSample}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.ActivationSample = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Activation Sample value set as {modelActivationRule.ActivationSample}.");
                                }
                            }

                            if (record.Priority.HasValue)
                            {
                                modelActivationRule.Priority = record.Priority.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Priority value set as {modelActivationRule.Priority}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.Priority = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Priority value set as {modelActivationRule.Priority}.");
                                }
                            }

                            if (record.EnableTtlCounter.HasValue)
                            {
                                modelActivationRule.EnableTtlCounter = record.EnableTtlCounter == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable TTL Counter value set as {modelActivationRule.EnableTtlCounter}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.EnableTtlCounter = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable TTL Counter value set as {modelActivationRule.EnableTtlCounter}.");
                                }
                            }

                            if (record.EnableResponseElevation.HasValue)
                            {
                                modelActivationRule.EnableResponseElevation = record.EnableResponseElevation == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable Response Elevation value set as {modelActivationRule.EnableResponseElevation}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.EnableResponseElevation = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable Response Elevation value set as {modelActivationRule.EnableResponseElevation}.");
                                }
                            }

                            if (record.EntityAnalysisModelTtlCounterGuid != Guid.Empty)
                            {
                                modelActivationRule.EntityAnalysisModelTtlCounterGuid =
                                    record.EntityAnalysisModelTtlCounterGuid;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set TTL Counter Name value set as {modelActivationRule.EntityAnalysisModelTtlCounterGuid}.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT TTL Counter Name, not supplied, value set as {modelActivationRule.EntityAnalysisModelTtlCounterGuid}.");
                                }
                            }

                            if (record.EntityAnalysisModelGuidTtlCounter != Guid.Empty)
                            {
                                modelActivationRule.EntityAnalysisModelGuidTtlCounter =
                                    record.EntityAnalysisModelGuidTtlCounter;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Entity Analysis Model Id Ttl Counter value set as {modelActivationRule.EntityAnalysisModelGuidTtlCounter}.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT TTL Counter Name, not supplied, value set as {modelActivationRule.EntityAnalysisModelTtlCounterGuid}.");
                                }
                            }

                            if (record.ResponseElevation.HasValue)
                            {
                                modelActivationRule.ResponseElevation = record.ResponseElevation.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation value set as {modelActivationRule.ResponseElevation}.");
                                }
                            }
                            else
                            {
                                modelActivationRule.ResponseElevation = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation value set as {modelActivationRule.ResponseElevation}.");
                                }
                            }

                            if (record.CaseWorkflowGuid != Guid.Empty)
                            {
                                modelActivationRule.CaseWorkflowGuid = record.CaseWorkflowGuid;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Cases Workflow ID value set as {modelActivationRule.CaseWorkflowGuid}.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Cases Workflow ID, not supplied, value set as {modelActivationRule.CaseWorkflowGuid}.");
                                }
                            }

                            if (record.CaseWorkflowStatusGuid != Guid.Empty)
                            {
                                modelActivationRule.CaseWorkflowStatusGuid = record.CaseWorkflowStatusGuid;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Cases Workflow Status Name value set as {modelActivationRule.CaseWorkflowStatusGuid}.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Cases Workflow Status Name, not supplied, value set as {modelActivationRule.CaseWorkflowStatusGuid}.");
                                }
                            }

                            if (record.RuleScriptTypeId.HasValue)
                            {
                                modelActivationRule.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Rule Script Type ID value set as {modelActivationRule.RuleScriptTypeId}.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Rule Script Type ID, not supplied, value set as {modelActivationRule.RuleScriptTypeId}.");
                                }
                            }

                            context.Services.CancellationToken.ThrowIfCancellationRequested();

                            var hasRuleScript = false;
                            if (record.BuilderRuleScript != null && modelActivationRule.RuleScriptTypeId == 1)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.BuilderRuleScript,
                                    ErrorSpans = []
                                };
                                parsedRule = context.Services.Parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = context.Services.Parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelActivationRule.ActivationRuleScript = parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Activation Rule Model {modelActivationRule.Id} set builder script as {modelActivationRule.ActivationRuleScript}.");
                                    }
                                }
                            }
                            else if (record.CoderRuleScript != null && modelActivationRule.RuleScriptTypeId == 2)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.CoderRuleScript,
                                    ErrorSpans = []
                                };
                                parsedRule = context.Services.Parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = context.Services.Parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelActivationRule.ActivationRuleScript = parsedRule.ParsedRuleText;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Activation Rule Model {modelActivationRule.Id} set coder script as {modelActivationRule.ActivationRuleScript}.");
                                    }

                                    hasRuleScript = true;
                                }
                            }

                            if (!hasRuleScript)
                            {
                                continue;
                            }

                            var activationRuleScript = new StringBuilder();
                            activationRuleScript.Append("Imports System.IO\r\n");
                            activationRuleScript.Append("Imports log4net\r\n");
                            activationRuleScript.Append("Imports System.Net\r\n");
                            activationRuleScript.Append("Imports System.Collections.Generic\r\n");
                            activationRuleScript.Append("Imports Jube.Dictionary\r\n");
                            activationRuleScript.Append("Imports Jube.Dictionary.Extensions\r\n");
                            activationRuleScript.Append("Imports System\r\n");
                            activationRuleScript.Append("Imports Jube.HttpAdaptationProtocol\r\n");
                            activationRuleScript.Append("Public Class ActivationRule\r\n");
                            activationRuleScript.Append(
                                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),TTLCounter As PooledDictionary(Of String, Double),Abstraction As PooledDictionary(Of string,double),HttpAdaptation As PooledDictionary(Of String, Adaptation),ExhaustiveAdaptation As PooledDictionary(Of String, Double),List as Dictionary(Of String,List(Of String)),Calculation As PooledDictionary(Of String, Double),Sanctions As PooledDictionary(Of String, Double),KVP As PooledDictionary(Of String, Double),Activation as ICollection(Of String),Log as ILog) As Boolean\r\n");
                            activationRuleScript.Append("Dim Matched as Boolean\r\n");
                            activationRuleScript.Append("Try\r\n");
                            activationRuleScript.Append(modelActivationRule.ActivationRuleScript + "\r\n");
                            activationRuleScript.Append("Catch ex As Exception\r\n");
                            activationRuleScript.Append("Log.Info(ex.ToString)\r\n");
                            activationRuleScript.Append("End Try\r\n");
                            activationRuleScript.Append("Return Matched\r\n");
                            activationRuleScript.Append("\r\n");
                            activationRuleScript.Append("End Function\r\n");
                            activationRuleScript.Append("End Class\r\n");

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} class wrapped as {activationRuleScript}.");
                            }

                            var activationRuleScriptHash = HashHelper.GetHash(activationRuleScript.ToString());

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash}, will now check if it is in the hash cache.");
                            }

                            if (context.Caching.HashCacheAssembly.TryGetValue(activationRuleScriptHash, out var valueHash))
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and been located in the hash cache to be assigned to a delegate.");
                                }

                                modelActivationRule.ActivationRuleCompile =
                                    valueHash;

                                var classType = modelActivationRule.ActivationRuleCompile.GetType("ActivationRule");
                                var methodInfo = classType.GetMethod("Match");
                                modelActivationRule.ActivationRuleCompileDelegate =
                                    (EntityAnalysisModelActivationRule.Match)Delegate.CreateDelegate(
                                        typeof(EntityAnalysisModelActivationRule.Match), methodInfo);

                                shadowEntityModelActivationRule.Add(modelActivationRule);

                                await repository.UpdateCompileStatusAsync(modelActivationRule.Id, true, null,
                                    context.Services.CancellationToken).ConfigureAwait(false);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash}, assigned to a delegate from the hash cache and added to a shadow list of Activation Rules.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and has not been located in the hash cache, hence it will be compiled.");
                                }

                                var compile = new Compile();
                                compile.CompileCode(activationRuleScript.ToString(), context.Services.Log,
                                [
                                    Path.Combine(context.Paths.BinaryPath ?? throw new InvalidOperationException(), "log4net.dll"),
                                    Path.Combine(context.Paths.BinaryPath, "Jube.Dictionary.dll"),
                                    Path.Combine(context.Paths.BinaryPath, "Jube.HttpAdaptationProtocol.dll")
                                ], Compile.Language.Vb);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and compiled with {compile.Errors}.");
                                }

                                if (compile.Errors == null)
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  it will now be allocated to a delegate.");
                                    }

                                    modelActivationRule.ActivationRuleCompile = compile.CompiledAssembly;

                                    var classType =
                                        modelActivationRule.ActivationRuleCompile.GetType("ActivationRule");
                                    var methodInfo = classType.GetMethod("Match");
                                    modelActivationRule.ActivationRuleCompileDelegate =
                                        (EntityAnalysisModelActivationRule.Match)Delegate.CreateDelegate(
                                            typeof(EntityAnalysisModelActivationRule.Match), methodInfo);
                                    shadowEntityModelActivationRule.Add(modelActivationRule);
                                    context.Caching.HashCacheAssembly.TryAdd(activationRuleScriptHash, compile.CompiledAssembly);
                                    context.Caching.HashCacheAssemblyMetadata.TryAdd(activationRuleScriptHash,
                                        new HashCacheAssemblyPayload(compile.CompiledAssemblyBytes, compile.CompiledAssemblyBinary, activationRuleScript.ToString()));

                                    await repository.UpdateCompileStatusAsync(modelActivationRule.Id, true, null,
                                        context.Services.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  allocated to a delegate and added to a shadow list of Activation Rules.");
                                    }
                                }
                                else
                                {
                                    await repository.UpdateCompileStatusAsync(modelActivationRule.Id, false, compile.ErrorsSummary,
                                        context.Services.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: {key} and Activation Rule Model {record.Id} has been hashed to {activationRuleScriptHash} but has failed to load.");
                                    }
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Activation Rule ID {record.Id} returned for model {key} has created an error as {ex}.");

                            await repository.UpdateCompileStatusAsync(record.Id, false, ex.Message,
                                context.Services.CancellationToken).ConfigureAwait(false);
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug($"Entity Start: {key} replaced Activation Rule List with shadow activation rules.");
                    }

                    var previousActivationRulesById = value.Collections.ModelActivationRules.ToDictionary(r => r.Id);
                    foreach (var newActivationRule in shadowEntityModelActivationRule)
                    {
                        if (!previousActivationRulesById.TryGetValue(newActivationRule.Id, out var previousActivationRule))
                        {
                            continue;
                        }

                        newActivationRule.EvaluationCounter =
                            Interlocked.Exchange(ref previousActivationRule.EvaluationCounter, 0);
                        newActivationRule.ActivationCounter =
                            Interlocked.Exchange(ref previousActivationRule.ActivationCounter, 0);
                        newActivationRule.ActivationCounterDate = previousActivationRule.ActivationCounterDate;
                    }

                    value.Collections.ModelActivationRules = shadowEntityModelActivationRule;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug($"Entity Start: {key} finished updating the counters for the Activation Rules.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: finished loading the Activation Rules.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelActivationRulesAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.ActivationRules, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
