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

namespace Jube.Engine.Exhaustive
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Accord.MachineLearning.VectorMachines;
    using Accord.Math;
    using Accord.Statistics;
    using Accord.Statistics.Distributions.Univariate;
    using Accord.Statistics.Kernels;
    using Accord.Statistics.Visualizations;
    using Algorithms;
    using Data;
    using Dictionary;
    using DynamicEnvironment;
    using Jube.Data.Context;
    using Jube.Data.Poco;
    using Jube.Data.Query;
    using Jube.Data.Repository;
    using Jube.Data.Repository.Interface;
    using log4net;
    using Model.Processing.Payload;
    using Newtonsoft.Json.Serialization;
    using Variables;
    using Performance=Algorithms.Performance;

    public class Training
    {
        private static DefaultContractResolver contractResolver;
        private readonly DynamicEnvironment environment;
        private readonly ILog log;
        private readonly Random seeded;
        public bool Stopped;

        public Training(ILog log, Random seeded, DynamicEnvironment environment,
            DefaultContractResolver contractResolver)
        {
            this.log = log;
            this.seeded = seeded;
            this.environment = environment;
            Training.contractResolver = contractResolver;
        }

        public async Task StartAsync()
        {
            while (!Stopped)
            {
                var dbContext =
                    DataConnectionDbContext.GetDbContextDataConnection(environment.AppSettings("ConnectionString"));

                if (log.IsInfoEnabled)
                {
                    log.Info("Exhaustive Training: Opening a database connection.");
                }

                try
                {
                    var queryNext = new GetNextExhaustiveSearchInstanceQuery(dbContext);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            "Exhaustive Training: Opened a database connection. " +
                            "Will proceed to lookup a request for an Exhaustive Instance,  " +
                            "or in the absence of an Exhaustive Instance to create one.");
                    }

                    var exhaustiveSearchInstance = queryNext.Execute();

                    if (exhaustiveSearchInstance != null)
                    {
                        if (!await Execute(exhaustiveSearchInstance, dbContext).ConfigureAwait(false))
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Exhaustive Training: ExhaustiveSearchInstanceId:{exhaustiveSearchInstance.Id} has exited in an uncompleted state.");
                            }
                            else
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Exhaustive Training: ExhaustiveSearchInstanceId:{exhaustiveSearchInstance.Id} has exited in a completed state.");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                "Exhaustive Training: Has not found anything that needs training.  Waiting.");
                        }

                        Thread.Sleep(60000);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(
                        $"Exhaustive Training: Has experienced an error as {ex}  waiting to poll again.");

                    Thread.Sleep(60000);
                }
                finally
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info("Exhaustive Training: Finished and closing database context.");
                    }

                    await dbContext.CloseAsync().ConfigureAwait(false);
                    await dbContext.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> Execute(GetNextExhaustiveSearchInstanceQuery.Dto exhaustiveSearchInstance,
            DbContext dbContext)
        {
            var repositoryExhaustiveSearchInstance = new ExhaustiveSearchInstanceRepository(dbContext);

            var repositoryExhaustiveSearchInstanceVariables =
                new ExhaustiveSearchInstanceVariableRepository(dbContext);

            var repositoryExhaustiveSearchInstanceVariableHistograms =
                new ExhaustiveSearchInstanceVariableHistogramRepository(dbContext);

            var repositoryExhaustiveSearchInstanceVariableMultiCollinearities =
                new ExhaustiveSearchInstanceVariableMultiColiniarityRepository(dbContext);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Found Exhaustive Search Instance ID {exhaustiveSearchInstance.Id}.  " +
                    $"Updating Status to 1 for pickup.");
            }

            var mockData = false;
            if (environment.AppSettings("UseMockDataExhaustive")
                .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                LoadMockData(dbContext, exhaustiveSearchInstance.EntityAnalysisModelId);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Found Exhaustive Search Instance ID {exhaustiveSearchInstance.Id}.  Is loading mock data.");
                }

                mockData = true;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Found Exhaustive Search Instance ID {exhaustiveSearchInstance.Id}.  Will use mock data for this training.");
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Set status to 1 for {exhaustiveSearchInstance.Id}. Starting");
            }

            if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
            {
                return false;
            }

            repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 1);

            double[][] data;
            Dictionary<int, Variable> variables;

            if (exhaustiveSearchInstance.Anomaly)
            {
                Extraction.GetSampleDataAsync(dbContext,
                    exhaustiveSearchInstance.TenantRegistryId,
                    exhaustiveSearchInstance.EntityAnalysisModelId,
                    mockData,
                    out variables,
                    out data);
            }
            else
            {
                var getSampleDataResponse = await Extraction.GetSampleDataAsync(dbContext,
                    exhaustiveSearchInstance.TenantRegistryId,
                    exhaustiveSearchInstance.EntityAnalysisModelId,
                    exhaustiveSearchInstance.FilterSql,
                    exhaustiveSearchInstance.FilterTokens,
                    mockData).ConfigureAwait(false);

                variables = getSampleDataResponse.Item1;
                data = getSampleDataResponse.Item2;
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Fetched data and there are {data.Length} records " +
                    $"for {exhaustiveSearchInstance.Id} updating status to 2 for calculating base statistics.");
            }

            if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
            {
                return false;
            }

            repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 2);

            CalculateStatistics(exhaustiveSearchInstance.Id, ref variables, data,
                repositoryExhaustiveSearchInstanceVariables,
                repositoryExhaustiveSearchInstanceVariableHistograms);

            double[] outputs = null;
            double[] cloneOfOutputsFromAnomaly = null;
            var classCount = 0;
            if (exhaustiveSearchInstance.Anomaly)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trained One Class Support Vector Machine " +
                        $"for {exhaustiveSearchInstance.Id} updating status to 3 for unsupervised training data normalisation.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 3);

                var copyOfDataForUnsupervised = NormaliseData(variables, data.Copy());

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trained One Class Support Vector Machine " +
                        $"for {exhaustiveSearchInstance.Id} updating status to 4 for unsupervised training.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 4);

                var anomaly = Unsupervised.Learn(copyOfDataForUnsupervised, log);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trained One Class Support Vector Machine " +
                        $"for {exhaustiveSearchInstance.Id} updating status to 5 for unsupervised training recall.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 5);

                outputs = GetClassVariableByAnomaly(anomaly, copyOfDataForUnsupervised,
                    exhaustiveSearchInstance.AnomalyProbability);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Recalled One Class Support Vector Machine.  Summing class count for {exhaustiveSearchInstance.Id}.");
                }

                classCount = (int)outputs.Sum();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: found {classCount} class for {exhaustiveSearchInstance.Id}.");
                }

                var onlyOutputWithClassificationForData =
                    OnlyOutputWithClassificationForData(classCount, outputs, data);

                var repositoryExhaustiveSearchInstanceVariablesAnomaly =
                    new ExhaustiveSearchInstanceVariableAnomalyRepository(dbContext);

                var repositoryExhaustiveSearchInstanceVariableHistogramsAnomaly =
                    new ExhaustiveSearchInstanceVariableHistogramAnomalyRepository(dbContext);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trained One Class Support Vector Machine " +
                        $"for {exhaustiveSearchInstance.Id} updating status to 6 for unsupervised " +
                        $"training recall statistics calculation.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 6);

                CalculateStatistics(exhaustiveSearchInstance.Id, ref variables,
                    onlyOutputWithClassificationForData,
                    repositoryExhaustiveSearchInstanceVariablesAnomaly,
                    repositoryExhaustiveSearchInstanceVariableHistogramsAnomaly);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Is about to clone the array of anomaly detection values.  This is useful for storing training data so it can be observed which values were anomaly and not overriden by filter.");
                }

                cloneOfOutputsFromAnomaly = (double[])outputs.Clone();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Has cloned the array of anomaly detection values.");
                }
            }

            if (exhaustiveSearchInstance.Filter)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Switched to create a classification " +
                        $"from data for {exhaustiveSearchInstance.Id} updating status to 7 for filtering.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 7);

                var (dataClassificationFilter, outputsClassificationFilter) =
                    await Extraction.GetClassDataAsync(dbContext,
                        exhaustiveSearchInstance.EntityAnalysisModelId,
                        exhaustiveSearchInstance.FilterSql,
                        exhaustiveSearchInstance.FilterTokens,
                        variables,
                        mockData).ConfigureAwait(false);

                var repositoryExhaustiveSearchInstanceVariablesClassification =
                    new ExhaustiveSearchInstanceVariableClassificationRepository(dbContext);

                var repositoryExhaustiveSearchInstanceVariableHistogramsClassification =
                    new ExhaustiveSearchInstanceVariableHistogramClassificationRepository(dbContext);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Switched to create a classification " +
                        $"from data for {exhaustiveSearchInstance.Id} updating status to 8 " +
                        $"for filtering statistics calculation.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 8);

                CalculateStatistics(exhaustiveSearchInstance.Id, ref variables, dataClassificationFilter,
                    repositoryExhaustiveSearchInstanceVariablesClassification,
                    repositoryExhaustiveSearchInstanceVariableHistogramsClassification);

                Append(data, dataClassificationFilter, out data, out outputs);

                classCount += (int)outputsClassificationFilter.Sum();
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Recalled Filter and found {classCount} further " +
                    $"class for {exhaustiveSearchInstance.Id} updating status to 9 for shuffling.");
            }

            if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
            {
                return false;
            }

            repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 9);

            if (classCount > 0)
            {
                Shuffle(data, outputs, out data, out outputs);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Finished shuffling {exhaustiveSearchInstance.Id} " +
                        $"updating status to 10 for normalising.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 10);

                data = NormaliseData(variables, data);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Updating status to 11 for {exhaustiveSearchInstance.Id} " +
                        $"for over sampling.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 11);

                DatasetSymmetry(data, outputs, out data, out outputs);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Has made the dataset symmetric for {exhaustiveSearchInstance.Id} " +
                        $"updating status to 12 for storing training data.  " +
                        $"Dataset has a length of {outputs.Length} with {outputs.Sum()} class value of 1. Is about to store final training data.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 12);

                StoreData(dbContext, outputs, exhaustiveSearchInstance, data, cloneOfOutputsFromAnomaly);

                if (log.IsInfoEnabled)
                {
                    log.Info("Has stored final training data.  Updating status to 13 for correlation analysis.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 13);

                CalculateCorrelations(variables, data, outputs,
                    repositoryExhaustiveSearchInstanceVariables);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Processed correlations for {exhaustiveSearchInstance.Id} " +
                        $"updating status to 14 for multi-co-linearity.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 14);

                variables = CalculateMulticollinearity(variables, data,
                    repositoryExhaustiveSearchInstanceVariableMultiCollinearities);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Inspected multi-co-linearity " +
                        $"for {exhaustiveSearchInstance.Id} updating status to 15 for supervised learning.");
                }

                if (IsStoppingOrStopped(repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id))
                {
                    return false;
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 15);

                var supervised = new Supervised(environment, repositoryExhaustiveSearchInstance,
                    exhaustiveSearchInstance.Id,
                    seeded,
                    variables, data, outputs, dbContext, log);

                if (await supervised.Train().ConfigureAwait(false))
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Finished training {exhaustiveSearchInstance.Id} " +
                            $"updating status to 16 for finished.");
                    }

                    repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 16);
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Stopped training {exhaustiveSearchInstance.Id} " +
                            $"updating status to 19 for stopped.");
                    }

                    repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 19);
                }
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Updated status to 17 for {exhaustiveSearchInstance.Id} " +
                        $"which denotes a zero class count.");
                }

                repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstance.Id, 17);
            }

            repositoryExhaustiveSearchInstance.UpdateCompleted(exhaustiveSearchInstance.Id);
            return true;
        }

        private bool IsStoppingOrStopped(ExhaustiveSearchInstanceRepository repositoryExhaustiveSearchInstance,
            int exhaustiveSearchInstanceId)
        {
            if (repositoryExhaustiveSearchInstance.IsStoppingOrStopped(exhaustiveSearchInstanceId))
            {
                return false;
            }

            repositoryExhaustiveSearchInstance.UpdateStatus(exhaustiveSearchInstanceId, 19);
            Stopped = true;
            return Stopped;
        }

        private static void StoreData(DbContext dbContext, double[] outputs,
            GetNextExhaustiveSearchInstanceQuery.Dto exhaustiveSearchInstance, double[][] data,
            double[] cloneOfOutputsFromAnomaly)
        {
            var exhaustiveSearchInstanceDataRepository = new ExhaustiveSearchInstanceDataRepository(dbContext);
            for (var i = 0; i < outputs.Length; i++)
            {
                var exhaustiveSearchInstanceData = new ExhaustiveSearchInstanceData
                {
                    Dependent = outputs[i],
                    ExhaustiveSearchInstanceId = exhaustiveSearchInstance.Id,
                    Independent = data[i],
                    Anomaly = cloneOfOutputsFromAnomaly != null ? cloneOfOutputsFromAnomaly[i] : 0
                };
                exhaustiveSearchInstanceDataRepository.Insert(exhaustiveSearchInstanceData);
            }
        }

        private void CalculateCorrelations(IReadOnlyDictionary<int, Variable> variables, double[][] data,
            double[] outputs, ExhaustiveSearchInstanceVariableRepository repository)
        {
            for (var i = 0; i < variables.Count - 1; i++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: About to calculate correlation for ExhaustiveSearch Instance Variable Id {variables[i].ExhaustiveSearchInstanceVariableId}.");
                }

                var independent = data.GetColumn(i);

                var performance = new Performance();
                variables[i].Correlation = performance.SpearmansCoeff(outputs, independent);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Calculated correlation as {variables[i].Correlation} for Exhaustive Search Instance Variable Id {variables[i].ExhaustiveSearchInstanceVariableId}.  Will proceed to rank them.");
                }
            }

            var sortedCorrelations = from c in variables
                orderby Math.Abs(Math.Round(c.Value.Correlation, 2)) descending
                select c;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Exhaustive Training: Ranked correlations.  Will store them alongside the existing variable record with ranking.");
            }

            var variableSequence = 0;
            foreach (var (_, value) in sortedCorrelations)
            {
                log.Info(
                    $"Exhaustive Training: Ranked correlations.  Will proceed to store the correlation for Exhaustive Search Instance Variable Id {value.ExhaustiveSearchInstanceVariableId}.");

                repository.UpdateCorrelation(value.ExhaustiveSearchInstanceVariableId,
                    value.Correlation, variableSequence);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Ranked correlations. Has stored correlation for {value.ExhaustiveSearchInstanceVariableId}.");
                }

                variableSequence += 1;
            }
        }

        private void DatasetSymmetry(IReadOnlyList<double[]> inData, double[] inOutputs, out double[][] outData,
            out double[] outOutputs)
        {
            var countPositive = (int)inOutputs.Sum();
            var countNegative = inOutputs.Length - countPositive;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: There are {countPositive} values in the affirmative and {countNegative} values in the negative.");
            }

            var remove = new List<int>();
            if (countNegative != countPositive)
            {
                int removeCount;
                int removeValue;
                if (countNegative > countPositive)
                {
                    removeCount = countNegative - countPositive;
                    removeValue = 0;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"The dataset is not symmetric and requires {removeCount} affirmative values to be removed.");
                    }
                }
                else
                {
                    removeCount = countPositive - countNegative;
                    removeValue = 1;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: The dataset is not symmetric and requires {removeCount} negative values to be removed.");
                    }
                }

                while (removeCount > 0)
                {
                    var randomRow = seeded.Next(0, inOutputs.Length - 1);

                    if (!(Math.Abs(inOutputs[randomRow] - removeValue) < 0.0001))
                    {
                        continue;
                    }

                    remove.Add(randomRow);

                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Exhaustive Training: Removed Record {randomRow} from the dataset.");
                    }

                    removeCount -= 1;
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info($"Exhaustive Training: Is about to rebuild the array given {remove.Count} records to remove.");
            }

            var symmetricData = new List<double[]>();
            var symmetricOutputs = new List<double>();
            for (var i = 0; i < inOutputs.Length - 1; i++)
            {
                if (remove.Contains(i))
                {
                    continue;
                }

                symmetricData.Add(inData[i]);
                symmetricOutputs.Add(inOutputs[i]);
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Has rebuilt the array and it is now {symmetricOutputs.Count} records long. Setting output variables.");
            }

            outData = symmetricData.ToArray();
            outOutputs = symmetricOutputs.ToArray();

            if (log.IsInfoEnabled)
            {
                log.Info("Exhaustive Training: Set output variables. Symmetry concluded.");
            }
        }

        private double[] GetClassVariableByAnomaly(SupportVectorMachine<Gaussian> model,
            double[][] data,
            double probability)
        {
            var outputs = new double[data.Length];

            for (var i = 0; i < data.Length - 1; i++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Processing row {i}.");
                }

                var p = model.Probability(data[i]);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Processed row {i} and returned probability {p}.");
                }

                outputs[i] = p < probability ? 1 : 0;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Processed row {i} and created class {outputs[i]}.");
                }
            }

            return outputs;
        }

        private double[][] NormaliseData(IReadOnlyDictionary<int, Variable> variables, double[][] data)
        {
            for (var i = 0; i < data.Length - 1; i++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Starting to normalise row {i}.");
                }

                for (var j = 0; j < variables.Count; j++)
                {
                    if (variables[j].NormalisationType == 2)
                    {
                        data[i][j] = (data[i][j] - variables[j].Mean) / variables[j].Sd;
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Finished normalising row {i}.");
                }
            }

            return data;
        }

        private static double[][] OnlyOutputWithClassificationForData(int classCount, double[] outputs, double[][] data)
        {
            var filteredData = new double[classCount][];
            var j = 0;
            for (var i = 0; i < outputs.Length - 1; i++)
            {
                if (!(Math.Abs(outputs[i] - 1) < 0.0001))
                {
                    continue;
                }

                filteredData[j] = data[i];
                j += 1;
            }

            return filteredData;
        }

        private static double SwapNanInfinityToZero(double value)
        {
            if (Double.IsNaN(value))
            {
                return 0;
            }

            if (Double.IsInfinity(value))
            {
                return 0;
            }

            return value;
        }

        private void CalculateStatistics(int exhaustiveSearchInstanceId, ref Dictionary<int, Variable> variables,
            double[][] data,
            IGenericRepository repositoryVariables,
            IGenericRepository repositoryHistogram
        )
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Is starting calculation of statistics for Exhaustive Search Instance {exhaustiveSearchInstanceId}.  There are {variables.Count} variables.");
            }

            for (var i = 0; i < variables.Count - 1; i++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Processing variable {i} for Exhaustive Search Instance {exhaustiveSearchInstanceId}.  There are {variables.Count} variables.");
                }

                var variableData = data.GetColumn(i);

                var modelExhaustiveSearchInstanceVariableClassification =
                    new ExhaustiveSearchInstanceVariableClassification();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                modelExhaustiveSearchInstanceVariableClassification.Mean = SwapNanInfinityToZero(variableData.Mean());

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Variable Mean: {modelExhaustiveSearchInstanceVariableClassification.Mean} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                modelExhaustiveSearchInstanceVariableClassification.Mode = SwapNanInfinityToZero(variableData.Mode());

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Variable Mode: {modelExhaustiveSearchInstanceVariableClassification.Mean} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                modelExhaustiveSearchInstanceVariableClassification.Maximum = SwapNanInfinityToZero(variableData.Max());

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Variable Maximum: {modelExhaustiveSearchInstanceVariableClassification.Maximum} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                modelExhaustiveSearchInstanceVariableClassification.Minimum = SwapNanInfinityToZero(variableData.Min());

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Variable Minimum: {modelExhaustiveSearchInstanceVariableClassification.Minimum} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                modelExhaustiveSearchInstanceVariableClassification.Kurtosis =
                    SwapNanInfinityToZero(variableData.Kurtosis());

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Variable Kurtosis: {modelExhaustiveSearchInstanceVariableClassification.Kurtosis} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                modelExhaustiveSearchInstanceVariableClassification.Skewness =
                    SwapNanInfinityToZero(variableData.Skewness());

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Variable Skewness: {modelExhaustiveSearchInstanceVariableClassification.Skewness} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                variableData.Quartiles(out var q1, out var q3);
                modelExhaustiveSearchInstanceVariableClassification.Iqr = SwapNanInfinityToZero(q3 - q1);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Variable Iqr: {modelExhaustiveSearchInstanceVariableClassification.Iqr} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                modelExhaustiveSearchInstanceVariableClassification.StandardDeviation =
                    SwapNanInfinityToZero(variableData.StandardDeviation());

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Variable Standard Deviation: {modelExhaustiveSearchInstanceVariableClassification.StandardDeviation} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                var distinctDoubles = new List<double>();
                foreach (var value in variableData)
                {
                    if (!distinctDoubles.Contains(value))
                    {
                        distinctDoubles.Add(value);
                    }
                }

                modelExhaustiveSearchInstanceVariableClassification.DistinctValues = distinctDoubles.Count;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Variable Distinct Values: {modelExhaustiveSearchInstanceVariableClassification.DistinctValues} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                const int maxBins = 20;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Maximum number of bins is {maxBins} and will proceed to calculate the optimal number of bins for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                var numberOfBins = 0.0d;
                try
                {
                    if (Math.Abs(modelExhaustiveSearchInstanceVariableClassification.Maximum.Value - 1) < 0.0001 &
                        modelExhaustiveSearchInstanceVariableClassification.Minimum.Value == 0 &
                        modelExhaustiveSearchInstanceVariableClassification.DistinctValues.Value == 2)
                    {
                        numberOfBins = 2;

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Variable is inferred to be binary with 2 bins for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                        }
                    }
                    else if (numberOfBins > modelExhaustiveSearchInstanceVariableClassification.DistinctValues.Value)
                    {
                        numberOfBins = modelExhaustiveSearchInstanceVariableClassification.DistinctValues.Value;

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Variable is set to distinct values of {modelExhaustiveSearchInstanceVariableClassification.DistinctValues.Value} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                        }
                    }
                    else if (modelExhaustiveSearchInstanceVariableClassification.Iqr.Value == 0)
                    {
                        numberOfBins = maxBins;

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Variable is set to distinct values of {modelExhaustiveSearchInstanceVariableClassification.DistinctValues.Value} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                        }
                    }
                    else
                    {
                        numberOfBins = Math.Round(1d + 3.3d * Math.Log(data.Length));

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Has used Sturges Rule to create {numberOfBins} bins for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                        }
                    }

                    if (numberOfBins > maxBins)
                    {
                        numberOfBins = maxBins;

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Has clipped to {numberOfBins} bins for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    numberOfBins = maxBins;

                    log.Error(
                        $"Exhaustive Training: Has {numberOfBins} bins for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} after exception of {ex}.");
                }

                modelExhaustiveSearchInstanceVariableClassification.Bins = (int)numberOfBins;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Number of Bins Values: {modelExhaustiveSearchInstanceVariableClassification.Bins} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id}.");
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} inserted and is about to compute histogram with {modelExhaustiveSearchInstanceVariableClassification.Bins} bins.");
                }

                var histogram = new Histogram();
                histogram.Compute(variableData);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} has computed histogram with {modelExhaustiveSearchInstanceVariableClassification.Bins} bins.  Will proceed to process bins.");
                }

                var binSequence = 0;
                if (repositoryVariables.GetType() == typeof(ExhaustiveSearchInstanceVariableRepository))
                {
                    variables[i].Mean =
                        modelExhaustiveSearchInstanceVariableClassification.Mean
                            .Value;
                    variables[i].Mode = modelExhaustiveSearchInstanceVariableClassification.Mode.Value;
                    variables[i].Max = modelExhaustiveSearchInstanceVariableClassification.Maximum.Value;
                    variables[i].Min = modelExhaustiveSearchInstanceVariableClassification.Minimum.Value;
                    variables[i].Kurtosis = modelExhaustiveSearchInstanceVariableClassification.Kurtosis.Value;
                    variables[i].Skewness = modelExhaustiveSearchInstanceVariableClassification.Skewness.Value;
                    variables[i].Iqr = modelExhaustiveSearchInstanceVariableClassification.Iqr.Value;
                    variables[i].Sd = modelExhaustiveSearchInstanceVariableClassification.StandardDeviation.Value;
                    variables[i].DistinctCount =
                        modelExhaustiveSearchInstanceVariableClassification.DistinctValues.Value;
                    variables[i].Bins = modelExhaustiveSearchInstanceVariableClassification.Bins.Value;

                    var modelExhaustiveSearchInstanceVariable = new ExhaustiveSearchInstanceVariable
                    {
                        ExhaustiveSearchInstanceId = exhaustiveSearchInstanceId,
                        VariableSequence = i,
                        Name = variables[i].Name,
                        ProcessingTypeId = variables[i].ProcessingTypeId,
                        Mode = modelExhaustiveSearchInstanceVariableClassification.Mode,
                        Mean = modelExhaustiveSearchInstanceVariableClassification.Mean,
                        StandardDeviation = modelExhaustiveSearchInstanceVariableClassification.StandardDeviation,
                        Kurtosis = modelExhaustiveSearchInstanceVariableClassification.Kurtosis,
                        Skewness = modelExhaustiveSearchInstanceVariableClassification.Skewness,
                        Maximum = modelExhaustiveSearchInstanceVariableClassification.Maximum,
                        Minimum = modelExhaustiveSearchInstanceVariableClassification.Minimum,
                        Iqr = modelExhaustiveSearchInstanceVariableClassification.Iqr,
                        DistinctValues = modelExhaustiveSearchInstanceVariableClassification.DistinctValues,
                        Bins = modelExhaustiveSearchInstanceVariableClassification.Bins
                    };

                    if (variables[i].Max is 0)
                    {
                        variables[i].NormalisationType = 0;//Empty
                    }
                    else if (variables[i].Min == 0 && Math.Abs(variables[i].Max - 1) < 0.0001)
                    {
                        variables[i].NormalisationType = 1;//Binary
                    }
                    else
                    {
                        variables[i].NormalisationType = 2;//Z Score
                    }

                    modelExhaustiveSearchInstanceVariable.NormalisationTypeId = variables[i].NormalisationType;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Variable Normalisation Type: {modelExhaustiveSearchInstanceVariable.NormalisationTypeId} for Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariable.Id}.");
                    }

                    var minNormalised = ZScore(variables[i].Min, variables[i].Mean, variables[i].Sd,
                        variables[i].NormalisationType);

                    var maxNormalised = ZScore(variables[i].Max, variables[i].Mean, variables[i].Sd,
                        variables[i].NormalisationType);

                    if (variables[i].Mode == 0)
                    {
                        var meanNormalised = ZScore(variables[i].Mean, variables[i].Mean, variables[i].Sd,
                            variables[i].NormalisationType);

                        if (variables[i].Mean == 0)
                        {
                            if (variables[i].Min < variables[i].Max)
                            {
                                variables[i].TriangularDistribution = new TriangularDistribution(minNormalised,
                                    maxNormalised, meanNormalised);
                            }
                        }
                        else
                        {
                            variables[i].TriangularDistribution = new TriangularDistribution(minNormalised,
                                maxNormalised, 0.01d);
                        }
                    }
                    else
                    {
                        var modeNormalised = ZScore(variables[i].Mode, variables[i].Mean, variables[i].Sd,
                            variables[i].NormalisationType);

                        if (variables[i].Min < variables[i].Max)
                        {
                            variables[i].TriangularDistribution = new TriangularDistribution(minNormalised,
                                maxNormalised, modeNormalised);
                        }
                    }

                    variables[i].ExhaustiveSearchInstanceVariableId =
                        repositoryVariables.Insert(modelExhaustiveSearchInstanceVariable);

                    foreach (var histogramBin in histogram.Bins)
                    {
                        var modelExhaustiveSearchInstanceVariableHistogram =
                            new ExhaustiveSearchInstanceVariableHistogram
                            {
                                BinSequence = binSequence,
                                BinRangeStart = histogramBin.Range.Min,
                                BinRangeEnd = histogramBin.Range.Max,
                                ExhaustiveSearchInstanceVariableId = variables[i].ExhaustiveSearchInstanceVariableId,
                                Frequency = histogramBin.Value
                            };

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} bin {binSequence} has start: {modelExhaustiveSearchInstanceVariableHistogram.BinRangeStart}, end {modelExhaustiveSearchInstanceVariableHistogram.BinRangeEnd} and frequency of {modelExhaustiveSearchInstanceVariableHistogram.Frequency}.");
                        }

                        repositoryHistogram.Insert(modelExhaustiveSearchInstanceVariableHistogram);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} bin {binSequence} inserted will move next bin.");
                        }
                        binSequence += 1;
                    }
                }
                else if (repositoryVariables.GetType() ==
                         typeof(ExhaustiveSearchInstanceVariableClassificationRepository))
                {
                    modelExhaustiveSearchInstanceVariableClassification.ExhaustiveSearchInstanceVariableId
                        = variables[i].ExhaustiveSearchInstanceVariableId;

                    var exhaustiveSearchInstanceVariableClassificationId =
                        repositoryVariables.Insert(modelExhaustiveSearchInstanceVariableClassification);

                    foreach (var histogramBin in histogram.Bins)
                    {
                        var modelExhaustiveSearchInstanceVariableHistogram =
                            new ExhaustiveSearchInstanceVariableHistogramClassification
                            {
                                BinSequence = binSequence,
                                BinRangeStart = histogramBin.Range.Min,
                                BinRangeEnd = histogramBin.Range.Max,
                                ExhaustiveSearchInstanceVariableClassificationId =
                                    exhaustiveSearchInstanceVariableClassificationId,
                                Frequency = histogramBin.Value
                            };

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} bin {binSequence} has start: {modelExhaustiveSearchInstanceVariableHistogram.BinRangeStart}, end {modelExhaustiveSearchInstanceVariableHistogram.BinRangeEnd} and frequency of {modelExhaustiveSearchInstanceVariableHistogram.Frequency}.");
                        }

                        repositoryHistogram.Insert(modelExhaustiveSearchInstanceVariableHistogram);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} bin {binSequence} inserted will move next bin.");
                        }

                        binSequence += 1;
                    }
                }
                else
                {
                    var modelExhaustiveSearchInstanceVariableAnomaly = new ExhaustiveSearchInstanceVariableAnomaly
                    {
                        Mode = modelExhaustiveSearchInstanceVariableClassification.Mode,
                        Mean = modelExhaustiveSearchInstanceVariableClassification.Mean,
                        StandardDeviation = modelExhaustiveSearchInstanceVariableClassification.StandardDeviation,
                        Kurtosis = modelExhaustiveSearchInstanceVariableClassification.Kurtosis,
                        Skewness = modelExhaustiveSearchInstanceVariableClassification.Skewness,
                        Maximum = modelExhaustiveSearchInstanceVariableClassification.Maximum,
                        Minimum = modelExhaustiveSearchInstanceVariableClassification.Minimum,
                        Iqr = modelExhaustiveSearchInstanceVariableClassification.Iqr,
                        DistinctValues = modelExhaustiveSearchInstanceVariableClassification.DistinctValues,
                        Bins = modelExhaustiveSearchInstanceVariableClassification.Bins,
                        ExhaustiveSearchInstanceVariableId = variables[i].ExhaustiveSearchInstanceVariableId
                    };

                    var exhaustiveSearchInstanceVariableAnomalyId =
                        repositoryVariables.Insert(modelExhaustiveSearchInstanceVariableAnomaly);

                    foreach (var histogramBin in histogram.Bins)
                    {
                        var modelExhaustiveSearchInstanceVariableHistogram =
                            new ExhaustiveSearchInstanceVariableHistogramAnomaly
                            {
                                BinSequence = binSequence,
                                BinRangeStart = histogramBin.Range.Min,
                                BinRangeEnd = histogramBin.Range.Max,
                                ExhaustiveSearchInstanceVariableAnomalyId = exhaustiveSearchInstanceVariableAnomalyId,
                                Frequency = histogramBin.Value
                            };

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} bin {binSequence} has start: {modelExhaustiveSearchInstanceVariableHistogram.BinRangeStart}, end {modelExhaustiveSearchInstanceVariableHistogram.BinRangeEnd} and frequency of {modelExhaustiveSearchInstanceVariableHistogram.Frequency}.");
                        }

                        repositoryHistogram.Insert(modelExhaustiveSearchInstanceVariableHistogram);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {modelExhaustiveSearchInstanceVariableClassification.Id} bin {binSequence} inserted will move next bin.");
                        }

                        binSequence += 1;
                    }
                }
            }
        }

        private Dictionary<int, Variable> CalculateMulticollinearity(Dictionary<int, Variable> variables, double[][] data,
            ExhaustiveSearchInstanceVariableMultiColiniarityRepository repository)
        {
            for (var i = 0; i < variables.Count - 1; i++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} will evaluate for strong correlations to this variable.");
                }

                var crossCorrelations = new Dictionary<int, double>();
                for (var j = 0; j < variables.Count - 1; j++)
                {
                    if (i != j)
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} will evaluate correlation against {variables[j].ExhaustiveSearchInstanceVariableId}.");
                        }

                        var performance = new Performance();
                        var correlation = performance.SpearmansCoeff(data.GetColumn(i), data.GetColumn(j));
                        crossCorrelations.Add(j, correlation);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} has evaluated correlation against {variables[j].ExhaustiveSearchInstanceVariableId} and returned {correlation}.");
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} will not evaluate to itself.");
                        }
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} will sort correlations for ranking.");
                }

                var sortedCrossCorrelations = from pair in crossCorrelations
                    orderby Math.Abs(Math.Round(pair.Value, 2)) descending
                    select pair;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} has sorted correlations for ranking. Will insert these correlations into the database.");
                }

                var seqCrossCorrelation = 1;
                foreach (var (key, value) in sortedCrossCorrelations)
                {
                    var model = new ExhaustiveSearchInstanceVariableMultiCollinearity
                    {
                        ExhaustiveSearchInstanceVariableId = variables[i].ExhaustiveSearchInstanceVariableId,
                        TestExhaustiveSearchInstanceVariableId = variables[key].ExhaustiveSearchInstanceVariableId,
                        Correlation = value,
                        CorrelationAbsRank = seqCrossCorrelation
                    };

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} will insert correlation for test key {variables[key].ExhaustiveSearchInstanceVariableId}.");
                    }

                    repository.Insert(model);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Exhaustive Search Instance Variable ID {variables[i].ExhaustiveSearchInstanceVariableId} has inserted correlation for test key {variables[key].ExhaustiveSearchInstanceVariableId} and will proceed to next test correlation.");
                    }

                    seqCrossCorrelation += 1;
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Exhaustive Training: Has concluded calculation of correlation for each correlation,  have updated variables and will return.");
            }

            return variables;
        }

        private void LoadMockData(DbContext dbContext, int entityAnalysisModelId)
        {
            var repository = new MockArchiveRepository(dbContext);

            if (log.IsInfoEnabled)
            {
                log.Info("Exhaustive Training: Will delete the MockArchive table for mock data.");
            }

            repository.Delete();

            if (log.IsInfoEnabled)
            {
                log.Info("Exhaustive Training: Deleted MockArchive.");
            }

            var variables = new List<string>();

            var iRow = 1;

            var lines = Mock.Get().Split("\n");
            foreach (var line in lines)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info($"Exhaustive Training:Found line data {line}.");
                }

                var splitsString = line.Split(",");
                if (iRow > 1)
                {
                    try
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Exhaustive Training:Found line at line {iRow} {splitsString.Length} splits.");
                        }

                        var splitsDouble = new double[splitsString.Length];

                        var row = new EntityAnalysisModelInstanceEntryPayload
                        {
                            Tag = new PooledDictionary<string, double>(splitsString.Length),
                            Abstraction = new PooledDictionary<string, double>(splitsString.Length)
                        };
                        
                        for (var i = 0; i < splitsString.Length - 1; i++)
                        {
                            splitsDouble[i] = Double.Parse(splitsString[i]);

                            if (i == 0)
                            {
                                row.Tag.Add("Fraud", splitsDouble[i] > 0 ? 1 : 0);
                            }
                            else
                            {
                                row.Abstraction.Add(variables[i], splitsDouble[i]);
                            }
                        }

                        var json = new EntityAnalysisModelInstanceEntryPayloadJson();

                        var model = new MockArchive
                        {
                            EntityAnalysisModelId = entityAnalysisModelId,
                            EntryKeyValue = iRow.ToString(),
                            ResponseElevation = 0,
                            EntityAnalysisModelActivationRuleId = 0,
                            ActivationRuleCount = 0,
                            CreatedDate = DateTime.Now,
                            ReferenceDate = DateTime.Now,
                            EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid()
                        };

                        var sr = new StreamReader(json.BuildJson(row, contractResolver));
                        model.Json = sr.ReadToEnd();

                        repository.Insert(model);

                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Exhaustive Training:Found line {line} has been added to the array.");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Exhaustive Training: Line {iRow} has caused an exception {ex}.");
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Exhaustive Training:Found header data {line}.");
                    }

                    foreach (var split in line.Split(","))
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info("Exhaustive Training:Adding variable.");
                        }

                        variables.Add(split);
                    }
                }

                iRow += 1;

                if (log.IsInfoEnabled)
                {
                    log.Info($"Exhaustive Training:Concluded line {line}.");
                }
            }

            switch (log.IsInfoEnabled)
            {
                case false:
                    return;
                case true:
                    log.Info(
                        $"Exhaustive Training:Concluded and created array of length {iRow} will cast and return array.");
                    break;
            }

        }

        private static double ZScore(double value, double mean, double sd, int normalisationTypeId)
        {
            try
            {
                if (normalisationTypeId == 2)
                {
                    return (value - mean) / sd;
                }

                return value;
            }
            catch
            {
                return 0;
            }
        }


        private static void Append(double[][] existingData,
            double[][] existingDataClassification,
            out double[][] data,
            out double[] outputs)
        {
            var appendedData = new double[existingData.Length + existingDataClassification.Length][];
            existingData.CopyTo(appendedData, 0);
            existingDataClassification.CopyTo(appendedData, existingData.Length);

            var existingOutputs = Enumerable.Repeat(0, existingData.Length).ToArray();
            var existingOutputsClassification = Enumerable.Repeat(1, existingDataClassification.Length).ToArray();

            var appendedOutputs = new double[existingData.Length + existingOutputsClassification.Length];
            existingOutputs.CopyTo(appendedOutputs, 0);
            existingOutputsClassification.CopyTo(appendedOutputs, existingOutputs.Length);

            data = appendedData;
            outputs = appendedOutputs;
        }

        private static void Shuffle(double[][] existingData,
            double[] existingOutputs,
            out double[][] newData,
            out double[] outputs)
        {
            var shuffleArray = new int[existingOutputs.Length];
            for (var i = 0; i < shuffleArray.Length; i++)
            {
                shuffleArray[i] = i;
            }

            var r = new Random();
            shuffleArray = shuffleArray.OrderBy(_ => r.Next()).ToArray();

            var newDataAfterShuffle = new double[shuffleArray.Length][];
            var outputsAfterShuffle = new double[shuffleArray.Length];

            for (var i = 0; i < shuffleArray.Length; i++)
            {
                newDataAfterShuffle[i] = existingData[shuffleArray[i]];
                outputsAfterShuffle[i] = existingOutputs[shuffleArray[i]];
            }

            newData = newDataAfterShuffle;
            outputs = outputsAfterShuffle;
        }
    }
}
