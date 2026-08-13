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

let where;
let select;
let initialFilterBuilder;

function getCasesFilter() {
    let value;
    let result = where.queryBuilder('getRules', 'numbered(@)');

    value = {
        selectJson: JSON.stringify(select.queryBuilder('getRules'), null, 2),
        filterJson: JSON.stringify(where.queryBuilder('getRules'), null, 2),
        filterTokens: JSON.stringify(result.params, null, 2)
    }

    return value;
}

function validateBuilder() {
    try {
        return where.queryBuilder('validate');
    } catch (error) {
        return false;
    }
}

function SetInitialFilterBuilder() {
    let builderResult = getCasesFilter();

    initialFilterBuilder = {
        filterJson: builderResult.filterJson,
        selectJson: builderResult.selectJson,
        filterTokens: builderResult.filterTokens
    };
}

function FilterExists(name, filters) {
    for (let i = 0; i < filters.length; i++) {
        if (filters[i].id === name) {
            return true;
        }
    }
    return false;
}

function initCaseFilterBuilder(isGuid, id, data) {
    let GetCaseWorkflow;
    let GetCaseWorkflowStatus;

    if (isGuid) {
        GetCaseWorkflow = "../api/Completions/ByCaseWorkflowGuidIncludingDeleted?CaseWorkflowGuid=" + id;
        GetCaseWorkflowStatus = "../api/CaseWorkflowStatus/ByCaseWorkflowGuid/" + id;
    } else {
        GetCaseWorkflow = "../api/Completions/ByCaseWorkflowIdIncludingDeleted?CaseWorkflowId=" + id;
        GetCaseWorkflowStatus = "../api/CaseWorkflowStatus/ByCaseWorkflowId/" + id;
    }

    const $builder = $("#Builder");

    if ($('#Select').length === 0) {
        $builder.append("<div id='Select'/>");
    }

    if (typeof select !== "undefined") {
        select.queryBuilder('destroy');
    }

    if ($("#Where").length === 0) {
        $builder.append("<div id='Where'/>");
    }

    if (typeof where !== "undefined") {
        where.queryBuilder('destroy');
    }

    $.getJSON(GetCaseWorkflow, function (completionsData) {
        completions = completionsData;

        $.getScript('../js/builder/query-builder.standalone.min.js', function () {
            $('<link/>', {
                rel: 'stylesheet',
                type: 'text/css',
                href: '/styles/query-builder.default.min.css'
            }).appendTo('head');

            $.getJSON(GetCaseWorkflowStatus, function (statusData) {
                let statusValues = {};

                if (typeof statusData !== "undefined") {
                    for (let i = 0; i < statusData.length; i++) {
                        statusValues[statusData[i].guid] = statusData[i].name;
                    }
                }

                let rulesFilter;
                if (data) {
                    rulesFilter = data.filterJson;
                }

                let rulesSelect;
                if (data) {
                    rulesSelect = data.selectJson;
                }

                let filters = [];
                let selects = [];

                filters.push({
                    optgroup: "Case",
                    id: "CaseKey",
                    name: "CaseKey",
                    field: "\"Case\".\"CaseKey\"",
                    label: 'CaseKey',
                    type: 'string'
                });

                selects.push({
                    optgroup: "Case",
                    id: "CaseKey",
                    field: "\"CaseKey\"",
                    label: 'CaseKey',
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "CaseKeyValue",
                    field: "\"CaseKeyValue\"",
                    label: "CaseKeyValue",
                    type: 'string'
                });

                selects.push({
                    optgroup: "Case",
                    id: "CaseKeyValue",
                    field: "\"CaseKeyValue\"",
                    label: "CaseKeyValue",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "Id",
                    field: "\"Case\".\"Id\"",
                    label: "Id",
                    type: 'integer'
                });

                selects.push({
                    optgroup: "Case",
                    id: "Id",
                    field: "\"Case\".\"Id\"",
                    label: "Id",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "Locked",
                    field: "\"Case\".\"Locked\"",
                    label: "Locked",
                    type: 'integer',
                    input: "radio",
                    default_value: 0,
                    values: {
                        1: 'Yes',
                        0: 'No'
                    },
                    operators: ['equal']
                });

                selects.push({
                    optgroup: "Case",
                    id: "Locked",
                    field: "\"Case\".\"Locked\"",
                    label: "Locked",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "LockedUser",
                    field: "\"Case\".\"LockedUser\"",
                    label: "LockedUser",
                    type: 'string',
                });

                selects.push({
                    optgroup: "Case",
                    id: "LockedUser",
                    field: "\"Case\".\"LockedUser\"",
                    label: "LockedUser",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "LockedDate",
                    field: "\"Case\".\"LockedDate\"",
                    label: "LockedDate",
                    type: 'datetime',
                });

                selects.push({
                    optgroup: "Case",
                    id: "LockedDate",
                    field: "\"Case\".\"LockedDate\"",
                    label: "LockedDate",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "ClosedStatusId",
                    field: "\"Case\".\"ClosedStatusId\"",
                    label: "ClosedStatus",
                    type: 'integer',
                    input: 'select',
                    values: {
                        0: 'Open',
                        1: 'Suspend Open',
                        2: 'Suspend Closed',
                        3: 'Closed',
                        4: 'Suspend Bypass'
                    }
                });

                selects.push({
                    optgroup: "Case",
                    id: "ClosedStatusId",
                    field: "\"Case\".\"ClosedStatusId\"",
                    label: "ClosedStatus",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "ClosedDate",
                    field: "\"Case\".\"ClosedDate\"",
                    label: "ClosedDate",
                    type: 'datetime'
                });

                selects.push({
                    optgroup: "Case",
                    id: "ClosedDate",
                    field: "\"Case\".\"ClosedDate\"",
                    label: "ClosedDate",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "ClosedUser",
                    field: "\"Case\".\"ClosedUser\"",
                    label: "ClosedUser",
                    type: 'string'
                });

                selects.push({
                    optgroup: "Case",
                    id: "ClosedUser",
                    field: "\"Case\".\"ClosedUser\"",
                    label: "ClosedUser",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "CreatedDate",
                    field: "\"Case\".\"CreatedDate\"",
                    label: "Created Date",
                    type: 'datetime'
                });

                selects.push({
                    optgroup: "Case",
                    id: "CreatedDate",
                    field: "\"Case\".\"CreatedDate\"",
                    label: "CreatedDate",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "Diary",
                    field: "\"Case\".\"Diary\"",
                    label: "Diary",
                    type: 'integer',
                    input: 'radio',
                    default_value: "True",
                    values: {
                        1: 'Yes',
                        0: 'No'
                    },
                    operators: ['equal']
                });

                selects.push({
                    optgroup: "Case",
                    id: "Diary",
                    field: "\"Case\".\"Diary\"",
                    label: "Diary",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "DiaryDate",
                    field: "\"Case\".\"DiaryDate\"",
                    label: "DiaryDate",
                    type: 'datetime'
                });

                selects.push({
                    optgroup: "Case",
                    id: "DiaryDate",
                    field: "\"Case\".\"DiaryDate\"",
                    label: "DiaryDate",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "CaseWorkflowStatusGuid",
                    field: "\"CaseWorkflowStatus\".\"Guid\"",
                    label: "CaseWorkflowStatus",
                    type: 'string',
                    input: 'select',
                    values: statusValues
                });

                selects.push({
                    optgroup: "Case",
                    id: "CaseWorkflowStatus",
                    field: "\"CaseWorkflowStatus\".\"Name\"",
                    label: "CaseWorkflowStatus",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "Priority",
                    field: "\"CaseWorkflowStatus\".\"Priority\"",
                    label: "Priority",
                    type: 'integer',
                    input: 'radio',
                    values: {
                        1: 'Ultra High',
                        2: 'High',
                        3: 'Normal',
                        4: 'Low',
                        5: 'Ultra Low'
                    }
                });

                selects.push({
                    optgroup: "Case",
                    id: "Priority",
                    field: "\"CaseWorkflowStatus\".\"Priority\"",
                    label: "Priority",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                filters.push({
                    optgroup: "Case",
                    id: "Rating",
                    field: "\"Case\".\"Rating\"",
                    label: "Rating",
                    name: "Rating",
                    type: 'integer'
                });

                selects.push({
                    optgroup: "Case",
                    id: "Rating",
                    field: "\"Case\".\"Rating\"",
                    label: "Rating",
                    type: 'string',
                    input: 'radio',
                    default_value: "ASC",
                    values: {
                        'ASC': 'Ascending',
                        'DESC': 'Descending'
                    },
                    operators: ['order']
                });

                for (const completion of completions) {
                    if (!FilterExists(completion.name, filters)) {

                        selects.push({
                            optgroup: completion.group,
                            id: completion.name,
                            field: completion.field,
                            label: completion.name,
                            type: 'string',
                            input: 'radio',
                            default_value: "ASC",
                            values: {
                                'ASC': 'Ascending',
                                'DESC': 'Descending'
                            },
                            operators: ['order']
                        });

                        if (completion.dataType === "string") {
                            let filter = {
                                optgroup: completion.group,
                                id: completion.name,
                                field: completion.field,
                                label: completion.name,
                                type: completion.dataType
                            };
                            filters.push(filter);
                        } else if (completion.dataType === "integer") {
                            let filter = {
                                optgroup: completion.group,
                                id: completion.name,
                                field: completion.field,
                                label: completion.name,
                                type: 'integer'
                            };
                            filters.push(filter);
                        } else if (completion.dataType === "double") {
                            let filter = {
                                optgroup: completion.group,
                                id: completion.name,
                                field: completion.field,
                                label: completion.name,
                                type: 'integer'
                            };
                            filters.push(filter);
                        } else if (completion.dataType === "boolean") {
                            let filter = {
                                optgroup: completion.group,
                                id: completion.name,
                                field: "\"Json\" ->> '" + completion.name + "' as \"" + completion.name + "\"",
                                label: completion.name,
                                type: 'string',
                                input: "radio",
                                default_value: "True",
                                values: {
                                    'True': 'Yes',
                                    'False': 'No'
                                },
                                operators: ['equal']
                            };
                            filters.push(filter);
                        }
                    }
                }

                select = $('#Select').queryBuilder({
                    filters: selects,
                    operators: [
                        {type: 'order', optgroup: 'basic', nb_inputs: 1, apply_to: ['string']}
                    ],
                    rules: rulesSelect
                });
                select.find("[data-add='group']").hide();
                select.find("[data-add='rule']").html("Add column");
                select.find(".group-conditions").children('label').each(
                    function () {
                        this.style.visibility = "hidden";
                    }
                );
                select.find(".rules-group-header").prepend('Select')

                where = $('#Where').queryBuilder({
                    filters: filters,
                    operators: [
                        {type: 'equal', optgroup: 'basic'},
                        {type: 'not_equal', optgroup: 'basic'},
                        {type: 'less', optgroup: 'basic'},
                        {type: 'less_or_equal', optgroup: 'basic'},
                        {type: 'greater', optgroup: 'basic'},
                        {type: 'greater_or_equal', optgroup: 'basic'},
                        {type: 'like', optgroup: 'basic', nb_inputs: 1, apply_to: ['string']},
                        {type: 'not_like', optgroup: 'basic', nb_inputs: 1, apply_to: ['string']}
                    ],
                    rules: rulesFilter
                });

                SetInitialFilterBuilder();
                validateBuilder()
                builderValidationInterval = setInterval(validateBuilder, 1300);
            });
        });
    });
}

//# sourceURL=CaseFilterBuilder.js