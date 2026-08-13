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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Accord.Math;
    using Accord.Neuro;
    using Accord.Neuro.ActivationFunctions;
    using Accord.Neuro.Learning;
    using Accord.Statistics;
    using Accord.Statistics.Visualizations;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using DynamicEnvironment;
    using Helpers;
    using log4net;
    using Models;
    using Newtonsoft.Json;

    public class Supervised(
        DynamicEnvironment environment,
        ExhaustiveSearchInstanceRepository repositoryExhaustiveSearchInstance,
        int exhaustiveSearchInstanceId,
        JsonSerializationHelper jsonSerializationHelper,
        Random seeded,
        Dictionary<int, Variable> variables,
        double[][] data,
        double[] output,
        DbContext dbContext,
        ILog log)
    {
        private readonly double[][] output = output.ToJagged();
        private readonly Performance performance = new Performance();

        public async Task<bool> TrainAsync(CancellationToken token = default)
        {
            var trialsLimit = Int32.Parse(environment.AppSettings("ExhaustiveTrialsLimit"));
            var minVariableCount = Int32.Parse(environment.AppSettings("ExhaustiveMinVariableCount"));
            var maxVariableCount = Int32.Parse(environment.AppSettings("ExhaustiveMaxVariableCount"));
            var trainingDataSamplePercentage =
                Double.Parse(environment.AppSettings("ExhaustiveTrainingDataSamplePercentage"));
            var crossValidationDataSamplePercentage =
                Double.Parse(environment.AppSettings("ExhaustiveCrossValidationDataSamplePercentage"));
            var testingDataSamplePercentage =
                Double.Parse(environment.AppSettings("ExhaustiveTestingDataSamplePercentage"));
            var validationTestingActivationThreshold =
                Double.Parse(environment.AppSettings("ExhaustiveValidationTestingActivationThreshold"));
            var topologySinceImprovementLimit =
                Int32.Parse(environment.AppSettings("ExhaustiveTopologySinceImprovementLimit"));
            var layerDepthLimit = Int32.Parse(environment.AppSettings("ExhaustiveLayerDepthLimit"));
            var layerWidthLimitInputLayerFactor =
                Int32.Parse(environment.AppSettings("ExhaustiveLayerWidthLimitInputLayerFactor"));
            var topologyComplexityLimit = Int32.Parse(environment.AppSettings("ExhaustiveTopologyComplexityLimit"));
            var activationFunctionExplorationEpochs =
                Int32.Parse(environment.AppSettings("ExhaustiveActivationFunctionExplorationEpochs"));
            var topologyExplorationEpochs = Int32.Parse(environment.AppSettings("ExhaustiveTopologyExplorationEpochs"));
            var topologyFinalisationEpochs = Int32.Parse(environment.AppSettings("ExhaustiveTopologyFinalisationEpochs"));
            var simulationsCount = Int32.Parse(environment.AppSettings("ExhaustiveSimulationsCount"));

            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Exhaustive Training: Is going to start supervised learning with the parameters " +
                    $"Trials Limit:{trialsLimit}," +
                    $"Min Variable Count: {minVariableCount}," +
                    $"MaxVariableCount: {maxVariableCount}," +
                    $"Training Data Sample Percentage: {trainingDataSamplePercentage}, " +
                    $"Cross Validation Data Sample Percentage: {crossValidationDataSamplePercentage}, " +
                    $"Testing Data Sample Percentage: {testingDataSamplePercentage}, " +
                    $"Validation Testing Activation Threshold:{validationTestingActivationThreshold}," +
                    $"Topology Since Improvement Limit: {topologySinceImprovementLimit}, " +
                    $"Layer Depth Limit: {layerDepthLimit}, " +
                    $"Layer Width Limit Input Layer Factor: {layerWidthLimitInputLayerFactor}, " +
                    $"Topology Complexity Limit: {topologyComplexityLimit}, " +
                    $"ActivationFunctionExplorationEpochs: {activationFunctionExplorationEpochs}," +
                    $"Topology Exploration Epochs: {topologyExplorationEpochs}," +
                    $"Topology Finalisation Epochs: {topologyFinalisationEpochs}, " +
                    $"Simulations Count: {simulationsCount}.");
            }

            var bestScore = 0d;
            var models = 0;
            var modelsSinceBest = 0;

            var exhaustiveSearchInstanceTrialInstanceRepository = new
                ExhaustiveSearchInstanceTrialInstanceRepository(dbContext);

            var exhaustiveSearchInstancePromotedTrialInstanceRepository = new
                ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext);

            var exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository = new
                ExhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository(dbContext);

            var exhaustiveSearchInstancePromotedTrialInstanceRocRepository =
                new ExhaustiveSearchInstancePromotedTrialInstanceRocRepository(dbContext);

            var exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository =
                new ExhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository(dbContext);

            var exhaustiveSearchInstancePromotedTrialInstancePrescriptionRepository =
                new ExhaustiveSearchInstancePromotedTrialInstanceVariableRepository(dbContext);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    "Exhaustive Training: Is going to start supervised learning with the parameters has created " +
                    "global variables and repositories for the training,  will now proceed to loop until trial limit of" +
                    $"{trialsLimit} has been reached.  Will check if stopped before next trial");
            }

            for (var i = 1; i < trialsLimit; i++)
            {
                try
                {
                    if (await IsStoppingOrStoppedAsync(token).ConfigureAwait(false))
                    {
                        return false;
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Is starting trial instance count {i}.  Recording the trial instance in the database.");
                    }

                    var exhaustiveSearchInstanceTrialInstance =
                        await InsertExhaustiveSearchInstanceTrialInstanceAsync(exhaustiveSearchInstanceTrialInstanceRepository, token).ConfigureAwait(false);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                            $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}.  " +
                            " Will create a random variable count for this trial and optimise the data based on sensitivity of" +
                            "those variables.");
                    }

                    var (activationFunction,
                        trialVariables,
                        dataTraining,
                        dataCrossValidation,
                        dataTesting,
                        outputsTraining,
                        outputsCrossValidation,
                        outputsTesting) = await OptimiseDataAsync(activationFunctionExplorationEpochs,
                        minVariableCount,
                        maxVariableCount,
                        exhaustiveSearchInstanceTrialInstance,
                        trainingDataSamplePercentage,
                        crossValidationDataSamplePercentage,
                        testingDataSamplePercentage,
                        validationTestingActivationThreshold,
                        token
                    ).ConfigureAwait(false);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                            $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                            "starting the evolution of width and depth.");
                    }

                    var (topologyNetwork,
                        performanceAchievedInThisTrial,
                        topologyComplexity) = await EvolveTopologyAsync(
                        topologySinceImprovementLimit,
                        topologyExplorationEpochs,
                        topologyFinalisationEpochs,
                        i,
                        exhaustiveSearchInstanceTrialInstance,
                        activationFunction,
                        trialVariables,
                        dataTraining,
                        outputsTraining,
                        dataCrossValidation,
                        outputsCrossValidation,
                        dataTesting,
                        outputsTesting,
                        validationTestingActivationThreshold,
                        layerWidthLimitInputLayerFactor, layerDepthLimit,
                        topologyComplexityLimit, token).ConfigureAwait(false);

                    LogTopology(exhaustiveSearchInstanceTrialInstance, topologyNetwork);

                    models = await IncrementModelsAsync(models, token).ConfigureAwait(false);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                            $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                            $"The total number of models executed so far is {models} and the best score is {bestScore}.  " +
                            $"This score is {performanceAchievedInThisTrial} and evaluates {performance.Score > bestScore} for " +
                            "promotion.");
                    }

                    if (performance.Score > bestScore && performance.Score > 0)
                    {
                        bestScore = performance.Score;

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                                $"Has promoted best score as {bestScore}.  There have been {modelsSinceBest},  which will be reset. " +
                                "Will promote model in the database.");
                        }

                        await repositoryExhaustiveSearchInstance.UpdateBestScoreAsync(exhaustiveSearchInstanceId, bestScore,
                            topologyComplexity, token).ConfigureAwait(false);

                        modelsSinceBest = 0;

                        await PromoteTopologyNetworkAsync(topologyNetwork, topologyComplexity,
                            exhaustiveSearchInstanceTrialInstance,
                            exhaustiveSearchInstancePromotedTrialInstanceRepository, token).ConfigureAwait(false);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                                "Has promoted the model in the database,  will now store the ROC data.");
                        }

                        await StoreRocAsync(outputsTesting,
                            exhaustiveSearchInstanceTrialInstance,
                            exhaustiveSearchInstancePromotedTrialInstanceRocRepository, token).ConfigureAwait(false);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                                "Has stored ROC data and will now store the predicted vs actual data (this will be sampled when" +
                                "presenting it in the user interface).");
                        }

                        await StorePredictedVsActualAsync(exhaustiveSearchInstanceTrialInstance, outputsTesting,
                            exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository, token).ConfigureAwait(false);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                                "Has stored the predicted vs. the actual in the database,  will now proceed to perform " +
                                "sensitivity analysis on the model and variables.");
                        }

                        await PerformSensitivityAnalysisAndStoreForPromotedAsync(
                            exhaustiveSearchInstanceTrialInstance.Id,
                            topologyNetwork, trialVariables,
                            dataCrossValidation, exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository, token).ConfigureAwait(false);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                                "Has performed sensitivity analysis on the model and variables and will now use Monte Carlo " +
                                "simulation to explain the model topology in practice.");
                        }

                        await PerformMonteCarloSimulationToUnderstandTopologyModelAsync(simulationsCount,
                            trialVariables,
                            topologyNetwork,
                            exhaustiveSearchInstancePromotedTrialInstancePrescriptionRepository, token).ConfigureAwait(false);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                                "Has performed Monte Carlo simulation on the model and variables and the promotion is concluded.");
                        }

                        if (!(Math.Abs(bestScore - 1) < 0.0001))
                        {
                            continue;
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                                "Is breaking training for reasons of total fit (and probably over fit).");
                        }

                        break;
                    }

                    modelsSinceBest = await IncrementModelsSinceBestAsync(modelsSinceBest, token).ConfigureAwait(false);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                            $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                            $"Has not beaten the best model,  has incremented models since best to {modelsSinceBest}.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Error(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                        $"Trial {trialsLimit} has caused an error in training as {ex}. ");
                }
            }

            return true;
        }

        private void LogTopology(ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            Network topologyNetwork)
        {
            var layersString =
                "Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                $" Has concluded the topology evolution.  The topology discovered is {topologyNetwork.Layers.Length} deep.  The " +
                " processing elements for each layer are as follows:";

            var first = true;
            var complexity = 0;
            foreach (var layer in topologyNetwork.Layers)
            {
                if (!first)
                {
                    layersString += ",";
                }
                else
                {
                    first = false;
                }

                layersString += layer.Neurons.Length;

                complexity += layer.Neurons.Length;
            }

            layersString += $". The topology complexity is {complexity}.";

            if (log.IsInfoEnabled)
            {
                log.Info(layersString);
            }
        }

        private static async Task PerformMonteCarloSimulationToUnderstandTopologyModelAsync(int simulationsCount,
            Dictionary<int, TrialVariable> trialVariables, ActivationNetwork topologyNetwork,
            ExhaustiveSearchInstancePromotedTrialInstanceVariableRepository
                exhaustiveSearchInstancePromotedTrialInstanceVariableRepository, CancellationToken token = default)
        {
            var activations = new List<double[]>();
            for (var j = 0; j < simulationsCount; j++)
            {
                var simulationZScore = new double[trialVariables.Count];

                for (var k = 0; k < trialVariables.Count; k++)
                {
                    try
                    {
                        simulationZScore[k] = trialVariables.ElementAt(k).Value.TriangularDistribution.Generate();
                        if (trialVariables.ElementAt(k).Value.NormalisationTypeId == 1)
                        {
                            simulationZScore[k] = simulationZScore[k] > 0.5 ? 1 : 0;
                        }
                    }
                    catch
                    {
                        simulationZScore[k] = 0;
                    }
                }

                if (topologyNetwork.Compute(simulationZScore)[0] > 0.5)
                {
                    activations.Add(simulationZScore);
                }
            }

            var activationsArray = activations.ToArray();

            if (activations.Count <= 0)
            {
                return;
            }

            {
                for (var j = 0; j < trialVariables.Count; j++)
                {
                    var exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription =
                        new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                        {
                            ExhaustiveSearchInstanceTrialInstanceVariableId =
                                trialVariables.ElementAt(j).Value.ExhaustiveSearchInstanceTrialInstanceVariableId
                        };

                    var outputs = activationsArray.GetColumn(j);
                    for (var i = 0; i < outputs.Length; i++)
                    {
                        outputs[i] = trialVariables.ElementAt(j).Value.ReverseZScore(outputs[i]);
                    }

                    exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Mean = outputs.Mean();
                    exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Maximum = outputs.Max();
                    exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Minimum = outputs.Min();
                    exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.StandardDeviation =
                        outputs.StandardDeviation();

                    outputs.Quartiles(out var q1, out var q3);
                    var iqr = q1 - q3;

                    exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Iqr =
                        trialVariables.ElementAt(j).Value.ReverseZScore(iqr);

                    exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription =
                        await exhaustiveSearchInstancePromotedTrialInstanceVariableRepository
                            .InsertAsync(exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription, token).ConfigureAwait(false);

                    if (exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Maximum -
                        exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Minimum == 0)
                    {
                        continue;
                    }

                    var histogram = new Histogram();
                    histogram.Compute(outputs);

                    var binSequence = 0;
                    foreach (var histogramBin in histogram.Bins)
                    {
                        var exhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram =
                            new ExhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram
                            {
                                ExhaustiveSearchInstancePromotedTrialInstanceVariableId
                                    = exhaustiveSearchInstancePromotedTrialInstanceVariablePrescription
                                        .Id,
                                Frequency = histogramBin.Value,
                                BinIndex = binSequence,
                                BinRangeEnd = histogramBin.Range.Min,
                                BinRangeStart = histogramBin.Range.Max
                            };

                        exhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram.BinRangeEnd =
                            histogramBin.Range.Max;

                        binSequence += 1;
                    }
                }
            }
        }

        private async Task PerformSensitivityAnalysisAndStoreForPromotedAsync(
            int exhaustiveSearchInstanceTrialInstanceId,
            ActivationNetwork topologyNetwork,
            Dictionary<int, TrialVariable> trialVariables, double[][] dataTesting,
            ExhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository
                exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository, CancellationToken token = default)
        {
            var sensitivityAnalysis = PerformSensitivityAnalysis(exhaustiveSearchInstanceTrialInstanceId,
                topologyNetwork, trialVariables, dataTesting, performance.Scores);

            foreach (var (key, value) in sensitivityAnalysis)
            {
                var exhaustiveSearchInstancePromotedTrialInstanceSensitivity =
                    new ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
                    {
                        Sensitivity = value,
                        SensitivityRank = 1,
                        ExhaustiveSearchInstanceTrialInstanceVariableId =
                            trialVariables[key].ExhaustiveSearchInstanceTrialInstanceVariableId
                    };

                await exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository.InsertAsync(
                    exhaustiveSearchInstancePromotedTrialInstanceSensitivity, token).ConfigureAwait(false);
            }
        }

        private async Task StoreRocAsync(double[][] outputsTesting,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            ExhaustiveSearchInstancePromotedTrialInstanceRocRepository
                exhaustiveSearchInstancePromotedTrialInstanceRocRepository, CancellationToken token = default)
        {
            const double minPrediction = 0d;
            const double maxPrediction = 1d;
            const double rocStep = (maxPrediction - minPrediction) / 20d;
            var rocStepThreshold = 0.05d;
            for (var j = 0; j < 20; j++)
            {
                performance.CalculatePerformance(performance.Scores, outputsTesting, rocStepThreshold);

                var exhaustiveSearchInstancePromotedTrialInstanceRoc =
                    new ExhaustiveSearchInstancePromotedTrialInstanceRoc
                    {
                        ExhaustiveSearchInstanceTrialInstanceId =
                            exhaustiveSearchInstanceTrialInstance.Id,
                        Score = performance.Score,
                        FalsePositive = performance.Fp,
                        FalseNegative = performance.Fn,
                        TruePositive = performance.Tp,
                        TrueNegative = performance.Tn,
                        Threshold = rocStepThreshold
                    };

                await exhaustiveSearchInstancePromotedTrialInstanceRocRepository.InsertAsync(
                    exhaustiveSearchInstancePromotedTrialInstanceRoc, token).ConfigureAwait(false);

                rocStepThreshold += rocStep;
            }
        }

        private async Task StorePredictedVsActualAsync(ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            double[][] outputsTesting,
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository
                exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository, CancellationToken token = default)
        {
            for (var j = 0; j < performance.Scores.Length; j++)
            {
                var exhaustiveSearchInstancePromotedTrialInstancePredictedActual
                    = new ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
                    {
                        ExhaustiveSearchInstanceTrialInstanceId =
                            exhaustiveSearchInstanceTrialInstance.Id,
                        Predicted = performance.Scores[j],
                        Actual = outputsTesting[j][0]
                    };

                await exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository.InsertAsync(
                    exhaustiveSearchInstancePromotedTrialInstancePredictedActual, token).ConfigureAwait(false);
            }
        }

        private Task<ExhaustiveSearchInstancePromotedTrialInstance> PromoteTopologyNetworkAsync(Network topologyNetwork, int topologyComplexity,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            ExhaustiveSearchInstancePromotedTrialInstanceRepository exhaustiveSearchInstancePromotedTrialsRepository, CancellationToken token = default)
        {
            var json = JsonConvert.SerializeObject(topologyNetwork, jsonSerializationHelper.TopologyNetworkJsonSerializerSettings);

            var exhaustiveSearchInstancePromotedTrial = new ExhaustiveSearchInstancePromotedTrialInstance
            {
                Active = 1,
                Score = performance.Score,
                TopologyComplexity = topologyComplexity,
                TrueNegative = performance.Tn,
                TruePositive = performance.Tp,
                FalseNegative = performance.Fn,
                FalsePositive = performance.Fp,
                Json = json,
                ExhaustiveSearchInstanceTrialInstanceId = exhaustiveSearchInstanceTrialInstance
                    .Id
            };

            return exhaustiveSearchInstancePromotedTrialsRepository.InsertAsync(exhaustiveSearchInstancePromotedTrial, token);
        }

        private async Task<int> IncrementModelsAsync(int models, CancellationToken token = default)
        {
            models += 1;
            await repositoryExhaustiveSearchInstance.UpdateModelsAsync(exhaustiveSearchInstanceId, models, token).ConfigureAwait(false);
            return models;
        }

        private async Task<int> IncrementModelsSinceBestAsync(int modelsSinceBest, CancellationToken token = default)
        {
            modelsSinceBest += 1;
            await repositoryExhaustiveSearchInstance.UpdateModelsSinceBestAsync(exhaustiveSearchInstanceId, modelsSinceBest, token).ConfigureAwait(false);
            return modelsSinceBest;
        }

        private async Task<(ActivationNetwork ActivationNetwork, Performance Performance, int BestTopologyComplexity)> EvolveTopologyAsync(
            int topologySinceImprovementLimit,
            int topologyExplorationEpochs,
            int topologyFinalEpochs,
            int i,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            IActivationFunction activationFunction,
            Dictionary<int, TrialVariable> trialVariables, double[][] dataTraining, double[][] outputsTraining,
            double[][] dataCrossValidation,
            double[][] outputsCrossValidation,
            double[][] dataTesting,
            double[][] outputsTesting,
            double validationTestingActivationThreshold,
            int layerWidthLimitInputLayerFactor,
            int layerDepthLimit, int topologyComplexityLimit, CancellationToken token = default)
        {
            ActivationNetwork bestTopologyNetwork = null;
            var bestTopologyComplexity = 0;

            var repositoryExhaustiveSearchInstanceTrialInstanceTopologyTrial
                = new ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository(dbContext);

            var bestScore = 0d;
            var topologiesSinceNoImproveCounter = 0;
            const int topologyTrials = 1;
            var layers = new List<int>
            {
                1
            };

            while (topologiesSinceNoImproveCounter < topologySinceImprovementLimit)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. {topologiesSinceNoImproveCounter} topologies processed since last improvement.");
                }

                var exhaustiveSearchInstanceTrialInstanceTopologyTrial =
                    new ExhaustiveSearchInstanceTrialInstanceTopologyTrial
                    {
                        ExhaustiveSearchInstanceTrialInstanceId = exhaustiveSearchInstanceTrialInstance
                            .Id,
                        TrialsSinceImprovement = topologiesSinceNoImproveCounter,
                        TopologyTrials = topologyTrials,
                        Layer = layers.Count,
                        Neurons = layers[^1],
                        Score = bestScore,
                        Finalisation = 0
                    };

                exhaustiveSearchInstanceTrialInstanceTopologyTrial =
                    await repositoryExhaustiveSearchInstanceTrialInstanceTopologyTrial.InsertAsync(
                        exhaustiveSearchInstanceTrialInstanceTopologyTrial, token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        $" created entry in the database for {exhaustiveSearchInstanceTrialInstanceTopologyTrial.ExhaustiveSearchInstanceTrialInstanceId}.  Will construct topology network and trainer with {layers.Count} layers" +
                        $" and activation function {activationFunction}.  Weights are randomised on the construction of the trainer.");
                }

                var topologyNetwork = new ActivationNetwork(activationFunction, trialVariables.Count,
                    MapTrialVariableToActivationNetworkAnnotations(trialVariables), LayersParamsArray(layers));

                var trainerExploration =
                    new LevenbergMarquardtLearning(TopologyRandomise(topologyNetwork));

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        $" created entry in the database for {exhaustiveSearchInstanceTrialInstanceTopologyTrial.ExhaustiveSearchInstanceTrialInstanceId}. Trainer is ready.");
                }

                for (var k = 0; k < topologyExplorationEpochs; k++)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} starting the training of EPOCH " +
                            k + ".");
                    }

                    var sw = new Stopwatch();
                    sw.Start();

                    trainerExploration.RunEpoch(dataTraining, outputsTraining);

                    sw.Stop();

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. Trial instance count {i} the EPOCH {k} in {sw.ElapsedMilliseconds} ms.");
                    }

                    sw.Reset();
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        " has finished topology training and will calculate the performance of the model.");
                }

                var thisScore = performance.CalculatePerformance(
                    performance.CalculateScores(topologyNetwork, dataCrossValidation),
                    outputsCrossValidation,
                    validationTestingActivationThreshold);

                var thisTopologyComplexity = topologyNetwork.Layers.Sum(layer => layer.Neurons.Length);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                        $" has a score of {thisScore}.  The best score is {bestScore} and when tested against {thisScore} evaluates to {thisScore > bestScore}.  The topology complexity is {bestTopologyComplexity}.");
                }

                if (thisScore > bestScore)
                {
                    bestScore = thisScore;
                    bestTopologyComplexity = thisTopologyComplexity;

                    bestTopologyNetwork = (ActivationNetwork)topologyNetwork.DeepMemberwiseClone();
                    topologiesSinceNoImproveCounter = 0;

                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                                 " has taken a deep copy of the best model and has reset the topologiesSinceNoImproveCounter to 0.");
                    }
                }

                if (Math.Abs(bestScore - 1) < 0.0001)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. Trial instance count {i}. Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} the topology has arrived at a perfect fit (too good in fact).  The training will terminate for this iteration.");
                    }

                    break;
                }

                if (topologyNetwork.Layers[layers.Count - 1].Neurons.Length >
                    trialVariables.Count * layerWidthLimitInputLayerFactor)
                {
                    if (layers.Count < layerDepthLimit)
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive " +
                                     "Search Instance Trial Instance ID of " +
                                     $"{exhaustiveSearchInstanceTrialInstance.Id}. " +
                                     $"Has reached a layer depth limit of {layerDepthLimit}.");
                        }

                        break;
                    }

                    layers.Add(1);
                    layers[^1] = 1;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive " +
                            $"Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                            $"Trial instance count {i}. Has increased layers to " +
                            $" {layers.Count} to width of {layers[^1]}.");
                    }
                }
                else
                {
                    layers[^1] += 1;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive " +
                            $"Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                            $"Trial instance count {i}. Has kept layers at " +
                            $" {layers.Count} and increased width to {layers[^1]}.");
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. Has created " +
                        "an Exhaustive Search Instance Trial Instance ID of " +
                        $"{exhaustiveSearchInstanceTrialInstance.Id} " +
                        $"topology complexity or weights count is {thisTopologyComplexity}.");
                }

                if (thisTopologyComplexity > topologyComplexityLimit)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search " +
                                 "Instance Trial Instance ID of " +
                                 $"{exhaustiveSearchInstanceTrialInstance.Id} " +
                                 $"topology complexity or weights count of {thisTopologyComplexity} has exceeded limits.");
                    }

                    break;
                }

                topologiesSinceNoImproveCounter += 1;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance count {i}. " +
                        "Has created an Exhaustive Search Instance Trial Instance ID of " +
                        $"{exhaustiveSearchInstanceTrialInstance.Id} " +
                        $"{topologiesSinceNoImproveCounter} trials since improvement in the evolution of the topology.");
                }
            }

            var trainerFinalise = new LevenbergMarquardtLearning(bestTopologyNetwork);
            for (var j = 0; j < topologyFinalEpochs; j++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Has created an Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} starting the training of finalise EPOCH " +
                        j + ".");
                }

                var sw = new Stopwatch();
                sw.Start();

                trainerFinalise.RunEpoch(dataTraining, outputsTraining);

                sw.Stop();

                if (log.IsInfoEnabled)
                {
                    log.Info($"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                             $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                             $"Trial instance count {i} the finalise EPOCH {j} in {sw.ElapsedMilliseconds} ms.");
                }

                sw.Reset();
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    "Finalisation done  Will calculate the final score from this topology evolution.");
            }

            performance.CalculatePerformance(performance.CalculateScores(bestTopologyNetwork, dataTesting),
                outputsTesting,
                validationTestingActivationThreshold);

            var bestPerformance = performance;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    $"Finalisation has a score of{bestPerformance.Score}.  Will store the finalisation trial in the database.");
            }

            var modelExhaustiveSearchInstanceTrialInstanceTopologyTrialFinalisation =
                new ExhaustiveSearchInstanceTrialInstanceTopologyTrial
                {
                    ExhaustiveSearchInstanceTrialInstanceId = exhaustiveSearchInstanceTrialInstance
                        .Id,
                    TrialsSinceImprovement = topologiesSinceNoImproveCounter,
                    TopologyTrials = topologyTrials,
                    Layer = layers.Count,
                    Neurons = layers[^1],
                    Score = bestScore,
                    Finalisation = 1
                };

            await repositoryExhaustiveSearchInstanceTrialInstanceTopologyTrial.InsertAsync(
                modelExhaustiveSearchInstanceTrialInstanceTopologyTrialFinalisation, token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    "Has finished evolving topology.  Returning best topology complexity {topologyComplexity}.");
            }

            return (bestTopologyNetwork, bestPerformance, bestTopologyComplexity);
        }

        private static int[] LayersParamsArray(List<int> layers)
        {
            var layersArray = new int[layers.Count + 1];
            for (var j = 0; j < layers.Count; j++)
            {
                layersArray[j] = layers[j];
            }

            layersArray[^1] = 1;
            return layersArray;
        }

        private async Task<(IActivationFunction bestActivationFunctionObject, Dictionary<int, TrialVariable> bestTrialVariables,
            double[][] bestDataTraining,
            double[][] bestDataCrossValidation,
            double[][] bestDataTesting,
            double[][] bestOutputsTraining,
            double[][] bestOutputsCrossValidation,
            double[][] bestOutputsTesting)> OptimiseDataAsync(
            int activationFunctionExplorationEpochs,
            int minVariableCount,
            int maxVariableCount,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            double trainingDataSamplePercentage,
            double crossValidationDataSamplePercentage,
            double testingDataSamplePercentage,
            double validationTestingActivationThreshold,
            CancellationToken token
        )
        {
            var bestSensitivityAnalysisScore = 0d;
            var trialVariables = new Dictionary<int, TrialVariable>();
            var removedVariablesCount = 0;
            Dictionary<int, TrialVariable> trialVariablesSnapshot = null;
            Dictionary<int, TrialVariable> originalVariables = null;
            IActivationFunction activationFunctionSnapshot = null;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                    " will proceed to optimise the dataset in a perpetual loop.");
            }

            while (true)
            {
                if (trialVariables.Count < minVariableCount)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                            " does not have enough variables so is establishing a random set of trial variables.");
                    }

                    var randomTrialVariableCount =
                        GetRandomVariableCount(variables.Count, minVariableCount, maxVariableCount);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                            $" will create with {randomTrialVariableCount} random variables.  Will proceed to select them in random order.");
                    }

                    trialVariables = await SelectVariablesAsync(exhaustiveSearchInstanceTrialInstance.Id,
                        randomTrialVariableCount, token).ConfigureAwait(false);

                    originalVariables = (Dictionary<int, TrialVariable>)trialVariables.DeepMemberwiseClone();

                    removedVariablesCount = 0;

                    trialVariablesSnapshot = new Dictionary<int, TrialVariable>();

                    activationFunctionSnapshot = null;

                    bestSensitivityAnalysisScore = 0;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} will create a model with {randomTrialVariableCount} variables. Will now proceed to split dataset.");
                    }
                }

                var reducedDataToTestActivationFunction = ReduceDataForOnlyTrialVariables(trialVariables);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                        $"reduced array to height {reducedDataToTestActivationFunction.Length} by width {reducedDataToTestActivationFunction[0].Length}.  " +
                        "Will now split for training, cross validation and testing data");
                }

                SplitData(trainingDataSamplePercentage,
                    crossValidationDataSamplePercentage,
                    testingDataSamplePercentage,
                    exhaustiveSearchInstanceTrialInstance,
                    reducedDataToTestActivationFunction,
                    output,
                    out var dataTraining,
                    out var outputsTraining,
                    out var dataCrossValidation,
                    out var outputsCrossValidation,
                    out var dataTesting,
                    out _);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} has split datasets.  " +
                        $"Counts are Testing:{dataTraining.Length}, Cross Validation: {dataCrossValidation.Length} and Testing {dataTesting.Length}. " +
                        "Will now select activation functions.");
                }

                var (bestTopologyNetwork,
                    bestActivationFunction,
                    bestActivationFunctionScore) = await SearchForBestActivationFunctionAsync(trialVariables,
                    activationFunctionExplorationEpochs,
                    exhaustiveSearchInstanceTrialInstance,
                    dataTraining, outputsTraining, dataCrossValidation,
                    outputsCrossValidation, validationTestingActivationThreshold, token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                        $" has found an activation function {bestActivationFunction}. Will proceed to perform sensitivity analysis.");
                }

                var sensitivityAnalysis = await PerformSensitivityAnalysisAndStoreForVariableSelectionAsync(
                    exhaustiveSearchInstanceTrialInstance.Id,
                    bestTopologyNetwork, trialVariables, dataCrossValidation, token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                        $" The best activation score is {bestActivationFunctionScore} and the best Sensitivity score is {bestSensitivityAnalysisScore}" +
                        $" which will evaluate to {bestActivationFunctionScore > bestSensitivityAnalysisScore} to proceed to remove the least sensitive variable from the trial.");
                }

                if (bestActivationFunctionScore > bestSensitivityAnalysisScore)
                {
                    bestSensitivityAnalysisScore = bestActivationFunctionScore;

                    activationFunctionSnapshot = (IActivationFunction)bestActivationFunction.DeepMemberwiseClone();
                    trialVariablesSnapshot = (Dictionary<int, TrialVariable>)trialVariables.DeepMemberwiseClone();

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                            $" The best activation score is {bestActivationFunctionScore} is about to remove key {sensitivityAnalysis.ElementAt(removedVariablesCount).Key}.  Deep copies of the best " +
                            " so far has been taken to allow revert as there is no certainty that this next trial will be better.");
                    }

                    trialVariables.Remove(sensitivityAnalysis.ElementAt(removedVariablesCount).Key);
                    removedVariablesCount += 1;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                            $" the removed variable account for sensitivity is {removedVariablesCount}.");
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                            " no longer gets improvement from removing variables that are not sensitive.  " +
                            "Will revert to deep copy before last removed variable and store the final removed variables removed in the database.");
                    }

                    var bestTrialVariables = trialVariablesSnapshot;
                    var bestActivationFunctionObject = activationFunctionSnapshot;

                    var repository = new ExhaustiveSearchInstanceTrialInstanceVariableRepository(dbContext);

                    if (originalVariables != null)
                    {
                        for (var j = 0; j < originalVariables.Count - 1; j++)
                        {
                            if (!bestTrialVariables.ContainsKey(originalVariables.ElementAt(j).Key))
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                                        $" variable {originalVariables.ElementAt(j).Key} has been removed,  will mark as removed in the database.");
                                }

                                await repository.UpdateAsRemovedByExhaustiveSearchInstanceVariableIdAsync(
                                    variables[originalVariables.ElementAt(j).Key].ExhaustiveSearchInstanceVariableId,
                                    exhaustiveSearchInstanceTrialInstance.Id, token).ConfigureAwait(false);

                                if (log.IsInfoEnabled)
                                {
                                    log.Info($"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                                             $" variable {originalVariables.ElementAt(j).Key}has been removed in the database for this trial. Will reduce data for the trial variables only.");
                                }
                            }
                            else
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                                        $" variable {originalVariables.ElementAt(j).Key} has not been removed.  Will reduce data for the trial variables only.");
                                }
                            }
                        }
                    }

                    var reducedDataAfterOptimisation = ReduceDataForOnlyTrialVariables(bestTrialVariables);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} " +
                            $" the array size is height {reducedDataAfterOptimisation.Length} by width {reducedDataAfterOptimisation[0].Length}.");
                    }

                    SplitData(trainingDataSamplePercentage,
                        crossValidationDataSamplePercentage,
                        testingDataSamplePercentage,
                        exhaustiveSearchInstanceTrialInstance,
                        reducedDataAfterOptimisation,
                        output,
                        out var bestDataTraining,
                        out var bestOutputsTraining,
                        out var bestDataCrossValidation,
                        out var bestOutputsCrossValidation,
                        out var bestDataTesting,
                        out var bestOutputsTesting);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} has split datasets.  " +
                            $"Counts are Testing:{dataTraining.Length}, Cross Validation: {dataCrossValidation.Length} and Testing {dataTesting.Length}. " +
                            "breaking search and returning method.");
                    }

                    return (bestActivationFunctionObject,
                        bestTrialVariables,
                        bestDataTraining,
                        bestDataCrossValidation,
                        bestDataTesting,
                        bestOutputsTraining,
                        bestOutputsCrossValidation,
                        bestOutputsTesting);
                }
            }
        }

        private async Task<Dictionary<int, double>> PerformSensitivityAnalysisAndStoreForVariableSelectionAsync(
            int exhaustiveSearchInstanceTrialInstanceId,
            ActivationNetwork bestTopologyNetwork,
            Dictionary<int, TrialVariable> trialVariables,
            double[][] dataCrossValidation, CancellationToken token = default)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId} " +
                    " is starting sensitivity analysis.");
            }

            var sensitivityAnalysis = PerformSensitivityAnalysis(exhaustiveSearchInstanceTrialInstanceId,
                bestTopologyNetwork,
                trialVariables, dataCrossValidation,
                performance.CalculateScores(bestTopologyNetwork, dataCrossValidation));

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId} " +
                    " has finished the sensitivity analysis.  Will store in the database.");
            }

            await StoreSensitivityForVariableSelectionAsync(sensitivityAnalysis, trialVariables, token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId} " +
                    " has stored sensitivity.");
            }

            return sensitivityAnalysis;
        }

        private async Task StoreSensitivityForVariableSelectionAsync(Dictionary<int, double> sensitivityAnalysis,
            Dictionary<int, TrialVariable> trialVariables, CancellationToken token = default)
        {
            var repository = new ExhaustiveSearchInstanceTrialInstanceSensitivityRepository(dbContext);

            foreach (var (key, value) in sensitivityAnalysis)
            {
                var model =
                    new ExhaustiveSearchInstanceTrialInstanceSensitivity
                    {
                        Sensitivity = value,
                        ExhaustiveSearchInstanceVariableId = trialVariables[key].ExhaustiveSearchInstanceVariableId
                    };

                await repository.InsertAsync(model, token).ConfigureAwait(false);
            }
        }

        private Dictionary<int, double> PerformSensitivityAnalysis(
            int exhaustiveSearchInstanceTrialInstanceId,
            ActivationNetwork bestTrialTopologyNetworkObject,
            Dictionary<int, TrialVariable> trialVariables, double[][] dataCrossValidationOrTesting,
            double[] baseline)
        {
            var sensitivityTrialVariables = new Dictionary<int, double>();

            for (var j = 0; j < trialVariables.Count; j++)
            {
                var outputSensitivityValues =
                    Performance.Sensitivity(seeded, bestTrialTopologyNetworkObject, dataCrossValidationOrTesting, j,
                        baseline);

                var sensitivity = outputSensitivityValues.Mean();

                sensitivityTrialVariables.Add(trialVariables.ElementAt(j).Key, sensitivity);
            }

            var sorted = from pair in sensitivityTrialVariables
                orderby Math.Abs(pair.Value)
                select pair;

            var sortedDictionary = sorted.ToDictionary(p => p.Key, p => p.Value);

            WriteSensitivityLogString(sortedDictionary, exhaustiveSearchInstanceTrialInstanceId);

            return sortedDictionary;
        }

        private void WriteSensitivityLogString(Dictionary<int, double> sortedDictionary,
            int exhaustiveSearchInstanceTrialInstanceId)
        {
            var sensitivityLogString =
                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstanceId}. " +
                " has created sensitivity ranking as ";

            var first = true;
            foreach (var (key, value) in sortedDictionary)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sensitivityLogString += ",";
                }

                sensitivityLogString += $" key {key} sensitivity {value}";
            }

            sensitivityLogString += ".";

            if (log.IsInfoEnabled)
            {
                log.Info(sensitivityLogString);
            }
        }

        private async Task<(ActivationNetwork bestTrialTopologyNetwork, IActivationFunction bestActivationFunction, double bestScore)> SearchForBestActivationFunctionAsync(Dictionary<int, TrialVariable> trialVariables,
            int activationFunctionExplorationEpochs,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            double[][] dataTraining, double[][] outputsTraining,
            double[][] dataCrossValidation, double[][] outputsCrossValidation,
            double validationTestingActivationThreshold, CancellationToken token = default)
        {
            var bestScore = 0d;
            ActivationNetwork bestTrialTopologyNetwork = null;
            IActivationFunction bestActivationFunction = null;

            var repositoryExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =
                new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository(dbContext);

            IActivationFunction trialTopologyFunction = new GaussianFunction();

            var trialTopologyNetwork = DefaultFirstActivationFunction(trialVariables,
                exhaustiveSearchInstanceTrialInstance,
                trialTopologyFunction,
                out var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial);

            for (var j = 1; j < 11; j++)
            {
                switch (j)
                {
                    case 1:
                    {
                        trialTopologyFunction = new BipolarSigmoidFunction(1);

                        trialTopologyNetwork =
                            new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                                MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Bipolar Sigmoid Function 1.");
                        }

                        break;
                    }

                    case 2:
                    {
                        trialTopologyFunction = new BipolarSigmoidFunction(2);

                        trialTopologyNetwork =
                            new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                                MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Bipolar Sigmoid Function 2.");
                        }

                        break;
                    }

                    case 3:
                    {
                        trialTopologyFunction = new ThresholdFunction();

                        trialTopologyNetwork =
                            new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                                MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Threshold Function.");
                        }

                        break;
                    }

                    case 4:
                    {
                        trialTopologyFunction = new SigmoidFunction(1);

                        trialTopologyNetwork =
                            new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                                MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Sigmoid Function 1.");
                        }

                        break;
                    }

                    case 5:
                    {
                        trialTopologyFunction = new SigmoidFunction(2);

                        trialTopologyNetwork =
                            new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                                MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Sigmoid Function 2.");
                        }

                        break;
                    }

                    case 6:
                    {
                        trialTopologyFunction = new LinearFunction(1);

                        trialTopologyNetwork =
                            new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                                MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Linear Function 1.");
                        }

                        break;
                    }

                    case 7:
                    {
                        trialTopologyFunction = new LinearFunction(2);

                        trialTopologyNetwork =
                            new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                                MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Linear Function 2.");
                        }

                        break;
                    }

                    case 8:
                    {
                        trialTopologyFunction = new RectifiedLinearFunction();

                        trialTopologyNetwork =
                            new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                                MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Rectified Linear Function.");
                        }

                        break;
                    }

                    case 9:
                    {
                        trialTopologyFunction = new IdentityFunction();

                        trialTopologyNetwork =
                            new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                                MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Identity Linear Function.");
                        }

                        break;
                    }

                    case 10:
                    {
                        trialTopologyFunction = new BernoulliFunction();

                        trialTopologyNetwork =
                            new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                                MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Bernoulli Linear Function.");
                        }

                        break;
                    }
                }

                exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial.ActivationFunctionId = j;
                exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =
                    await repositoryExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
                        .InsertAsync(exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial, token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}has randomised topology weights for testing the Activation Function.");
                }

                var trainer =
                    new LevenbergMarquardtLearning(TopologyRandomise(trialTopologyNetwork));

                for (var k = 1; k < activationFunctionExplorationEpochs; k++)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is performing EPOCH {k}");
                    }

                    var sw = new Stopwatch();
                    sw.Start();

                    trainer.RunEpoch(dataTraining, outputsTraining);

                    sw.Stop();
                    sw.Reset();

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} has finished EPOCH {k} in {sw.ElapsedMilliseconds} ms.");
                    }

                    var outputs = performance.CalculateScores(trialTopologyNetwork, dataCrossValidation);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is calculating performance for EPOCH {k} as a classification model.");
                    }

                    var thisScore = performance.CalculatePerformance(outputs, outputsCrossValidation,
                        validationTestingActivationThreshold);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} and the score is {thisScore},  this will be tested against the best score so far which is {bestScore}.");
                    }

                    if (thisScore > bestScore)
                    {
                        bestTrialTopologyNetwork = (ActivationNetwork)trialTopologyNetwork.DeepMemberwiseClone();
                        bestActivationFunction = (IActivationFunction)trialTopologyFunction.DeepMemberwiseClone();
                        bestScore = thisScore;

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} calculated performance for EPOCH {k} and the score is {bestScore}. This is the best score so far in sensitivity analysis.  The model has been saved as best.");
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} calculated performance for EPOCH {k} and the score is {bestScore}.  This is not the best score so far.");
                        }
                    }
                }
            }

            return (bestTrialTopologyNetwork, bestActivationFunction, bestScore);
        }

        private ActivationNetwork DefaultFirstActivationFunction(Dictionary<int, TrialVariable> trialVariables,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            IActivationFunction trialTopologyFunction,
            out ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
                exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial)
        {
            var trialTopologyNetwork = new ActivationNetwork(trialTopologyFunction, trialVariables.Count,
                MapTrialVariableToActivationNetworkAnnotations(trialVariables), 1);

            exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =
                new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
                {
                    ExhaustiveSearchInstanceTrialInstanceId =
                        exhaustiveSearchInstanceTrialInstance.Id
                };

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id} is testing Gaussian Function.");
            }

            return trialTopologyNetwork;
        }

        private double[][] ReduceDataForOnlyTrialVariables(Dictionary<int, TrialVariable> trialVariables)
        {
            var reducedData = new double[data.Length][];
            for (var j = 0; j < reducedData.Length; j++)
            {
                reducedData[j] = new double[trialVariables.Count];

                for (var k = 0; k < trialVariables.Count - 1; k++)
                {
                    reducedData[j][k] = data[j][trialVariables.ElementAt(k).Key];
                    k += 1;
                }
            }

            return reducedData;
        }

        private static ActivationNetwork TopologyRandomise(ActivationNetwork topologyNetwork)
        {
            var topologyRandomise = new NguyenWidrow(topologyNetwork);
            topologyRandomise.Randomize();
            return topologyNetwork;
        }

        private void SplitData(
            double trainingDataSamplePercentage,
            double crossValidationDataSamplePercentage,
            double testingDataSamplePercentage,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            double[][] dataToSplit,
            double[][] outputToSplit,
            out double[][] dataTraining,
            out double[][] outputsTraining,
            out double[][] dataCrossValidation,
            out double[][] outputsCrossValidation,
            out double[][] dataTesting,
            out double[][] outputsTesting
        )
        {
            var trainingLength = (int)(outputToSplit.Length * trainingDataSamplePercentage);
            var crossValidationLength = (int)(outputToSplit.Length * crossValidationDataSamplePercentage);
            var testingLength = (int)(outputToSplit.Length * testingDataSamplePercentage);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID " +
                    $"of {exhaustiveSearchInstanceTrialInstance.Id} is separating the dataset. " +
                    $"There are {trainingLength} training records, {crossValidationLength} cross validation records " +
                    $"and {testingLength} testing records.  Will split the dataset.");
            }

            dataTraining = new double[trainingLength - 1][];
            dataCrossValidation = new double[crossValidationLength - 1][];
            dataTesting = new double[testingLength + crossValidationLength][];
            outputsTraining = new double[trainingLength - 1][];

            outputsCrossValidation = new double[crossValidationLength - 1][];
            outputsTesting = new double[testingLength + crossValidationLength - 1][];

            dataTraining = SplitArray(dataToSplit, 0, trainingLength);
            outputsTraining = SplitArray(outputToSplit, 0, trainingLength);

            dataCrossValidation = SplitArray(dataToSplit, trainingLength, crossValidationLength);
            outputsCrossValidation = SplitArray(outputToSplit, trainingLength, testingLength + crossValidationLength);

            dataTesting = SplitArray(dataToSplit, crossValidationLength, testingLength);
            outputsTesting = SplitArray(outputToSplit, crossValidationLength, testingLength + crossValidationLength);
        }

        private async Task<Dictionary<int, TrialVariable>> SelectVariablesAsync(
            int exhaustiveSearchInstanceTrialInstanceId, int randomTrialVariableCount, CancellationToken token = default)
        {
            var repository = new ExhaustiveSearchInstanceTrialInstanceVariableRepository(dbContext);
            await repository.DeleteAllByExhaustiveSearchInstanceTrialInstanceIdAsync(exhaustiveSearchInstanceTrialInstanceId, token).ConfigureAwait(false);

            var trialVariables = new Dictionary<int, TrialVariable>();
            for (var j = 0; j < randomTrialVariableCount; j++)
            {
                var randomVariable = seeded.Next(0, randomTrialVariableCount - 1);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                        $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  Has selected {randomVariable} as" +
                        " a random index.  Will check if it has already been added as a trial variable.");
                }

                if (!trialVariables.ContainsKey(randomVariable))
                {
                    var trialVariable = new TrialVariable
                    {
                        Name = variables[randomVariable].Name,
                        Mean = variables[randomVariable].Mean,
                        Sd = variables[randomVariable].Sd,
                        Mode = variables[randomVariable].Mode,
                        Min = variables[randomVariable].Min,
                        Max = variables[randomVariable].Max,
                        ExhaustiveSearchInstanceVariableId =
                            variables[randomVariable].ExhaustiveSearchInstanceVariableId,
                        TriangularDistribution = variables[randomVariable].TriangularDistribution,
                        NormalisationTypeId = variables[randomVariable].NormalisationType,
                        ProcessingTypeId = variables[randomVariable].ProcessingTypeId
                    };

                    trialVariables.Add(randomVariable, trialVariable);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                            $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                            $"Has added Exhaustive Search Instance Variable Id {variables[randomVariable].ExhaustiveSearchInstanceVariableId})." +
                            "Will also store a record in the database.");
                    }

                    var exhaustiveSearchInstanceTrialInstanceVariable =
                        new ExhaustiveSearchInstanceTrialInstanceVariable
                        {
                            VariableSequence = j,
                            ExhaustiveSearchInstanceVariableId =
                                variables[randomVariable].ExhaustiveSearchInstanceVariableId,
                            ExhaustiveSearchInstanceTrialInstanceId = exhaustiveSearchInstanceTrialInstanceId
                        };

                    trialVariables[randomVariable].ExhaustiveSearchInstanceTrialInstanceVariableId
                        = (await repository.InsertAsync(exhaustiveSearchInstanceTrialInstanceVariable, token).ConfigureAwait(false)).Id;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                            $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                            $"Has added Exhaustive Search Instance Variable Id {variables[randomVariable].ExhaustiveSearchInstanceVariableId})." +
                            " has created Exhaustive Search Instance Trial Instance Variable Id " +
                            $"{trialVariables[randomVariable].ExhaustiveSearchInstanceTrialInstanceVariableId}.");
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                            $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                            $"Has not added Exhaustive Search Instance Variable Id {variables[randomVariable].ExhaustiveSearchInstanceVariableId})." +
                            " as it already exist.");
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID of " +
                        $"{exhaustiveSearchInstanceTrialInstanceId} is separating the dataset.  " +
                        $" Has finished adding trial variables and there are now {trialVariables.Count} available.  Returning method.");
                }
            }

            return trialVariables;
        }

        private static double[][] SplitArray(IReadOnlyList<double[]> inputs, int start, int finish)
        {
            var newArray = new double[finish][];
            var j = 0;
            var loopTo = start + finish - 1;
            for (var i = start; i <= loopTo; i++)
            {
                newArray[j] = inputs[i];
                j += 1;
            }

            return newArray;
        }

        private int GetRandomVariableCount(
            int availableVariableCount, int minVariableCount, int maxVariableCount)
        {
            if (availableVariableCount < minVariableCount)
            {
                return availableVariableCount;
            }

            var value = seeded.Next(minVariableCount, availableVariableCount);

            if (value < minVariableCount)
            {
                return minVariableCount;
            }

            return value > maxVariableCount ? maxVariableCount : value;
        }

        private Task<ExhaustiveSearchInstanceTrialInstance> InsertExhaustiveSearchInstanceTrialInstanceAsync(
            ExhaustiveSearchInstanceTrialInstanceRepository repository, CancellationToken token = default)
        {
            var exhaustiveSearchInstanceTrialInstance = new ExhaustiveSearchInstanceTrialInstance
            {
                ExhaustiveSearchInstanceId = exhaustiveSearchInstanceId,
                CreatedDate = DateTime.UtcNow
            };

            return repository.InsertAsync(exhaustiveSearchInstanceTrialInstance, token);
        }

        private static List<ActivationNetworkAnnotation> MapTrialVariableToActivationNetworkAnnotations(
            Dictionary<int, TrialVariable> trialVariables)
        {
            return trialVariables.Select(trialVariable => new ActivationNetworkAnnotation
                {
                    Name = trialVariable.Value.Name,
                    ExhaustiveSearchInstanceVariableId = trialVariable.Value.ExhaustiveSearchInstanceVariableId,
                    Mean = trialVariable.Value.Mean,
                    Sd = trialVariable.Value.Sd,
                    Max = trialVariable.Value.Max,
                    Min = trialVariable.Value.Min,
                    Mode = trialVariable.Value.Mode,
                    ExhaustiveSearchInstanceTrialInstanceVariableId =
                        trialVariable.Value.ExhaustiveSearchInstanceTrialInstanceVariableId,
                    NormalisationTypeId = trialVariable.Value.NormalisationTypeId,
                    ProcessingTypeId = trialVariable.Value.ProcessingTypeId
                })
                .ToList();
        }

        private async Task<bool> IsStoppingOrStoppedAsync(CancellationToken token = default)
        {
            return !await repositoryExhaustiveSearchInstance.IsStoppingOrStoppedAsync(exhaustiveSearchInstanceId, token).ConfigureAwait(false);
        }
    }
}
