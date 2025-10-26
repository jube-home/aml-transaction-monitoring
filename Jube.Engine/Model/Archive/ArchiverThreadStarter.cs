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

namespace Jube.Engine.Model.Archive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using log4net;

    public class ArchiverThreadStarter
    {
        public Dictionary<int, EntityAnalysisModel> ActiveModels;
        public ILog Log;
        private bool stopping;
        public int ThreadSequence { get; init; }

        public void StopMe()
        {
            stopping = true;
        }

        public void Start()
        {
            var found = false;
            while (!stopping)
            {
                try
                {
                    if (ActiveModels == null)
                    {
                        continue;
                    }

                    foreach (var activeModelKvp in ActiveModels)
                    {
                        found = activeModelKvp.Value.TryProcessSingleDequeueForCaseCreationAndArchiver(
                            ThreadSequence);
                    }

                    if (found)
                    {
                        found = false;
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Thread.Sleep(100);
                    Log.Error($"All Entity Models Database Storage: Error{ex}");
                }
            }
        }
    }
}
