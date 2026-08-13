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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Data.Poco;
    using EntityAnalysisModelInvoke.Context;
    using EntityAnalysisModelInvoke.Models;
    using EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using Jube.Cache.Redis.Callback;
    using Jube.Engine.BackgroundTasks.TaskStarters.Models;

    public class ConcurrentQueues
    {
        public ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> Callbacks;
        public ConcurrentQueue<Context> PendingEntityInvoke = new ConcurrentQueue<Context>();
        public ConcurrentQueue<TagMessage> PendingTagging { get; set; } = new ConcurrentQueue<TagMessage>();
        public ConcurrentQueue<Notification> PendingNotifications { get; set; }
        public ConcurrentQueue<ActivationWatcher> PersistToActivationWatcherAsync { get; set; } = new ConcurrentQueue<ActivationWatcher>();
        public ConcurrentQueue<EntityAnalysisModelInstanceEntryPayload> PersistToDatabaseAsync { get; } = new ConcurrentQueue<EntityAnalysisModelInstanceEntryPayload>();
        public ConcurrentQueue<ResponseElevation> ResponseElevationEntries { get; } = new ConcurrentQueue<ResponseElevation>();
        // ReSharper disable once CollectionNeverUpdated.Global
        public ConcurrentQueue<DateTime> ActivationWatcherCountJournal { get; } = new ConcurrentQueue<DateTime>();
        public ConcurrentQueue<DateTime> BillingResponseElevationJournal { get; } = new ConcurrentQueue<DateTime>();
    }
}
