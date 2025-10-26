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

namespace Jube.Engine.Exhaustive.Algorithms
{
    using Accord.MachineLearning.VectorMachines;
    using Accord.MachineLearning.VectorMachines.Learning;
    using Accord.Statistics.Kernels;
    using log4net;

    public static class Unsupervised
    {
        public static SupportVectorMachine<Gaussian> Learn(double[][] data, ILog log)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Exhaustive Training: Is about to start looking for a Gaussian estimate for the Kernel Trick using the dataset.");
            }

            var estimate = Gaussian.Estimate(data, data.Length);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Has estimated as Gamma {estimate.Gamma} and Sigma {estimate.Sigma}.  Will now proceed to train the One Class Support Vector Machine with Gaussian Kernel Trick.");
            }

            var svm = new OneclassSupportVectorLearning<Gaussian>
            {
                Kernel = estimate
            };

            var model = svm.Learn(data);

            if (log.IsInfoEnabled)
            {
                log.Info("Exhaustive Training: Has finished training and will return model.");
            }

            return model;
        }
    }
}
