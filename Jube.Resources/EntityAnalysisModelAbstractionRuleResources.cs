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

namespace Jube.Resources
{
    public sealed class EntityAnalysisModelAbstractionRuleResources
    {
        public const string PermissionDenied = nameof(PermissionDenied);
        public const string NotAuthenticated = nameof(NotAuthenticated);
        public const string EntityAnalysisModelIdInvalid = nameof(EntityAnalysisModelIdInvalid);
        public const string NameRequired = nameof(NameRequired);
        public const string NameMaxLength = nameof(NameMaxLength);
        public const string NameAlreadyExists = nameof(NameAlreadyExists);
        public const string RuleScriptTypeIdInvalid = nameof(RuleScriptTypeIdInvalid);
        public const string BuilderRuleScriptRequired = nameof(BuilderRuleScriptRequired);
        public const string BuilderRuleScriptMaxLength = nameof(BuilderRuleScriptMaxLength);
        public const string JsonRequired = nameof(JsonRequired);
        public const string CoderRuleScriptRequired = nameof(CoderRuleScriptRequired);
        public const string CoderRuleScriptMaxLength = nameof(CoderRuleScriptMaxLength);
        public const string SearchKeyRequired = nameof(SearchKeyRequired);
        public const string SearchValueRange = nameof(SearchValueRange);
        public const string SearchIntervalInvalid = nameof(SearchIntervalInvalid);
        public const string SearchFunctionTypeIdInvalid = nameof(SearchFunctionTypeIdInvalid);
        public const string SearchFunctionKeyRequired = nameof(SearchFunctionKeyRequired);
        public const string OffsetTypeIdInvalid = nameof(OffsetTypeIdInvalid);
        public const string OffsetValueRange = nameof(OffsetValueRange);
    }
}