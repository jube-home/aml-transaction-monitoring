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

namespace Jube.HttpAdaptationProtocol
{
    public static class ProtocolConstants
    {
        public const string CurrentProtocolVersion = "1.1";

        public static class Family
        {
            public const string Glm = "GLM";
            public const string RandomForest = "RandomForest";
            public const string C5 = "C5";
            public const string XgBoost = "XGBoost";
            public const string Svm = "SVM";
            public const string BayesianNetwork = "BayesianNetwork";
            public const string NeuralNetwork = "NeuralNetwork";
            public const string ExpertRule = "ExpertRule";
        }

        public static class ContributionSpace
        {
            public const string Relative = "Relative";
        }

        public static class ContributionMethod
        {
            public const string Coefficient = "Coefficient";
            public const string BootstrapStrength = "BootstrapStrength";
            public const string ArcStrength = "ArcStrength";
            public const string ConnectionWeight = "ConnectionWeight";
        }

        public static class ValueSpace
        {
            public const string Probability = "Probability";
            public const string LogOdds = "LogOdds";
            public const string DecisionFunction = "DecisionFunction";
            public const string VoteFraction = "VoteFraction";
            public const string Score = "Score";
        }

        public static class CalibrationMethod
        {
            public const string None = "None";
            public const string Native = "Native";
            public const string Platt = "Platt";
            public const string Isotonic = "Isotonic";
            public const string Beta = "Beta";
        }

        public static class StructureLearning
        {
            public const string HillClimbing = "HillClimbing";
            public const string Mmhc = "MMHC";
            public const string TabuSearch = "TabuSearch";
            public const string Expert = "Expert";
            public const string Constrained = "Constrained";
            public const string None = "None";
        }

        public static class Source
        {
            public const string Payload = "Payload";
            public const string Abstraction = "Abstraction";
            public const string AbstractionCalculation = "AbstractionCalculation";
            public const string TtlCounter = "TtlCounter";
            public const string Dictionary = "Dictionary";
            public const string Sanction = "Sanction";
        }
    }
}
