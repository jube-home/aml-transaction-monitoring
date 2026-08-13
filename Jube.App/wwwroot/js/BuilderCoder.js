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

let langTools;
let ruleTextBuilder = "";
let ruleTextCoder = "";
let builderInvalid = false;
let CompileInProgress = false;
let FirstCompile = true;
let completions = [];
let builder;
let PendingCoderChangedCompileTimer;
let coder;
let showBuilder = false;
let ruleType;
let valid;
let builderValidationInterval;
let tabStrip;
let entityAnalysisModelId;
let ruleParseType;
let builderReady = false;

function loadScript(url) {
    return new Promise(function (resolve, reject) {
        $.getScript(url).done(resolve).fail(reject);
    });
}

function loadCompletions() {
    return new Promise(function (resolve, reject) {
        $.getJSON("../api/Completions/ByEntityAnalysisModelIdParseTypeId",
            {
                entityAnalysisModelId: entityAnalysisModelId,
                parseTypeId: ruleParseType
            })
            .done(resolve)
            .fail(reject);
    });
}

function getBuilderCoder() {
    let value;
    if (showBuilder) {
        createBuilderRuleText();

        value = {
            ruleTextBuilder: ruleTextBuilder,
            ruleJsonBuilder: JSON.stringify(builder.queryBuilder('getRules'), null, 2),
            ruleTextCoder: ruleTextCoder,
            ruleType: ruleType
        };
    } else {
        value = {
            ruleTextCoder: ruleTextCoder,
            ruleType: ruleType
        };
    }

    return value;
}

function validateBuilderCoder() {
    return true;
}

function checkDivergence() {
    if (showBuilder) {
        if (ruleType === 2) {
            if (ruleTextBuilder !== ruleTextCoder) {
                tabStrip.disable(tabStrip.tabGroup.children().eq(0));
            } else {
                tabStrip.enable(tabStrip.tabGroup.children().eq(0));
            }
        }
    }
}

function CoderChanged() {
    ruleTextCoder = coder.getValue().trim();
    checkDivergence();
    if (typeof PendingCoderChangedCompileTimer !== "undefined") {
        clearTimeout(PendingCoderChangedCompileTimer);
    }
    if (FirstCompile) {
        FirstCompile = false;
        CoderChangedCompile();
    } else {
        PendingCoderChangedCompileTimer = setTimeout('CoderChangedCompile();', 1300);
    }
}

function converge() {
    coder.setValue(createBuilderRuleText());
    $("#BuilderRuleTypeWrapper").show();
    $("#ResetBuilderRuleTypeWrapper").hide();
}

function CoderChangedCompile() {
    if (!CompileInProgress) {
        CompileInProgress = true;
        let coderText = coder.getValue();

        let compileStatus = $('#CompileStatus');
        if (coderText.trim() === '') {
            coder.getSession().clearAnnotations();
            compileStatus.html("Return False");
            compileStatus.css("color", "blue");
        } else {
            $.ajax({
                url: "../api/Parser",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: JSON.stringify({
                    RuleParseType: ruleParseType,
                    RuleText: coderText,
                    EntityAnalysisModelId: entityAnalysisModelId
                }),
                error: function () {
                    coder.getSession().clearAnnotations();
                    compileStatus.html("Disconnected");
                    compileStatus.css("color", "red");
                },
                success: function (data) {
                    const annotations = [];

                    if (data.errorSpans != null) {
                        data.errorSpans.forEach(function (errorSpan) {
                                annotations.push({
                                    row: errorSpan.line,
                                    column: 0,
                                    text: errorSpan.message,
                                    type: "error"
                                })
                            }
                        );
                        coder.getSession().setAnnotations(annotations);

                        compileStatus.html("Errors");
                        compileStatus.css("color", "red");
                    } else {
                        coder.getSession().clearAnnotations();
                        compileStatus.html("Compiled");
                        compileStatus.css("color", "green");
                    }
                    CompileInProgress = false;
                }
            });
        }
    }
}

function validateBuilder() {
    if (!builderReady || !builder) {
        return false;
    }
    const instance = builder.data('queryBuilder');
    if (!instance || !instance.filters || instance.filters.length === 0) {
        return false;
    }
    return builder.queryBuilder('validate');
}

function setRuleType(denormIndex) {
    ruleType = denormIndex;
    const $builderWarnings = $('#BuilderWarnings');
    if (ruleType === 2) {
        coder.session.setValue(createBuilderRuleText());
        $builderWarnings.hide();
    } else {
        $builderWarnings.show();
    }
}

function createBuilderRuleText() {
    if (validateBuilder()) {
        let sql = builder.queryBuilder('getSQL').sql;
        sql = sql.split("['").join("");
        sql = sql.split("']").join("");
        sql = sql.split("= 'True'").join("= True");
        sql = sql.split("= 'False'").join("= False");

        let result = "If (" + sql + ") Then\n  Return True\nEnd If"
        result = result.replace(/'/g, '"');
        result = result.split(' .').join('.');
        result = result.trim();
        ruleTextBuilder = result;
    } else {
        ruleTextBuilder = "Return False";
        builderInvalid = true;
    }
    return ruleTextBuilder;
}

function FilterExists(name, filters) {
    for (let i = 0; i < filters.length; i++) {
        if (filters[i].id === name) {
            return true;
        }
    }
    return false;
}

function initBuilder(data) {
    if (!completions || completions.length === 0) {
        console.warn('initBuilder called with no completions — aborting.');
        return;
    }

    let rules;
    if (data) {
        rules = data.ruleJsonBuilder;
    }

    let filters = [];
    let listSelects = {};

    for (const completion of completions) {
        if (!FilterExists(completion.name, filters)) {
            if (completion.dataType === "string") {
                let filter = {
                    optgroup: completion.group,
                    id: completion.name,
                    name: completion.name,
                    type: completion.dataType
                };
                filters.push(filter);
                listSelects[completion.name] = completion.name;
            } else if (completion.dataType === "integer") {
                let filter = {
                    optgroup: completion.group,
                    id: completion.name,
                    name: completion.name,
                    type: 'integer'
                };
                filters.push(filter);
            } else if (completion.dataType === "double") {
                let filter = {
                    optgroup: completion.group,
                    id: completion.name,
                    name: completion.name,
                    type: 'double'
                };
                filters.push(filter);
            } else if (completion.dataType === "boolean") {
                let filter = {
                    optgroup: completion.group,
                    id: completion.name,
                    name: completion.name,
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
            } else if (completion.dataType === "list") {
                let filter = {
                    optgroup: completion.group,
                    id: completion.name,
                    name: completion.name,
                    type: 'string',
                    input: "select",
                    default_value: "True",
                    values: listSelects,
                    operators: ['has']
                };
                filters.push(filter);
            }
        }
    }

    builder = $('#Builder').queryBuilder({
        plugins: [
            'not-group'
        ],
        filters: filters,
        operators: [
            {type: 'equal', optgroup: 'basic'},
            {type: 'less', optgroup: 'basic'},
            {type: 'less_or_equal', optgroup: 'basic'},
            {type: 'greater', optgroup: 'basic'},
            {type: 'begins_with', optgroup: 'basic'},
            {type: 'contains', optgroup: 'basic'},
            {type: 'ends_with', optgroup: 'basic'},
            {type: 'has', optgroup: 'custom', nb_inputs: 1, multiple: false, apply_to: ['lists']}
        ],
        sqlOperators: {
            equal: {op: '= ?'},
            less: {op: '< ?'},
            less_or_equal: {op: '<= ?'},
            greater: {op: '> ?'},
            greater_or_equal: {op: '>= ?'},
            begins_with: {op: '.StartsWith(?)'},
            contains: {op: '.Contains(?)'},
            ends_with: {op: '.EndsWith(?)'},
            has: {op: '.contains([?])'}
        }
    });

    if (rules) {
        const dropped = [];

        try {
            const parsedRules = typeof rules === 'string' ? JSON.parse(rules) : rules;

            function stripUnknownFilters(group) {
                if (!group || !Array.isArray(group.rules)) return group;
                group.rules = group.rules.filter(function (rule) {
                    if (rule.rules) {
                        stripUnknownFilters(rule);
                        return true;
                    }
                    if (!FilterExists(rule.id, filters)) {
                        dropped.push(rule.id);
                        return false;
                    }
                    return true;
                });
                return group;
            }

            const cleanedRules = stripUnknownFilters(parsedRules);

            if (cleanedRules && cleanedRules.rules && cleanedRules.rules.length > 0) {
                builder.queryBuilder('setRules', cleanedRules);
            } else if (dropped.length === 0) {
                console.warn('Could not restore builder rules: empty or malformed rule object');
            }
        } catch (e) {
            console.warn('Could not restore builder rules:', e.message);
        }

        if (dropped.length > 0) {
            const msg = '⚠ The following fields are no longer available and have been removed from the rule: '
                + dropped.join(', ')
                + '. This rule should be reviewed.';
            console.warn(msg);
            $('#BuilderWarnings').text(msg);
        }
    }

    builderReady = true;

    if (builderValidationInterval) {
        clearInterval(builderValidationInterval);
    }
    builderValidationInterval = setInterval(validateBuilder, 1300);
}

function initCoder(data) {
    langTools = ace.require('ace/ext/language_tools');

    const completer = {
        getCompletions: (editor, session, pos, prefix, callback) => {
            if (prefix.length === 0) {
                callback(null, []);
                return;
            }

            callback(null, completions.map(function (ea) {
                return {
                    name: ea.name,
                    value: ea.value,
                    score: ea.score,
                    meta: ea.meta
                };
            }));
        }
    };

    coder = ace.edit('Coder');
    coder.setTheme('ace/theme/sqlserver');
    coder.getSession().setMode('ace/mode/vbscript');
    coder.getSession().on('change', CoderChanged);
    coder.setReadOnly(false);
    langTools.setCompleters([]);
    coder.setOptions({
        enableBasicAutocompletion: true,
        enableSnippets: true,
        enableLiveAutocompletion: true
    });

    if (data) {
        coder.setValue(data.ruleTextCoder);
    } else {
        coder.setValue(ruleTextCoder);
    }
    langTools.addCompleter(completer);
}

function initBuilderCoder(parseType, modelId, data) {
    ruleParseType = parseType;
    switch (ruleParseType) {
        case 1:
            showBuilder = false;
            break;
        case 2:
            showBuilder = true;
            break;
        case 3:
            showBuilder = true;
            break;
        case 4:
            showBuilder = false;
            break;
        case 5:
            showBuilder = true;
            break;
        default:
            showBuilder = false;
            break;
    }

    entityAnalysisModelId = modelId;

    let ruleDiv = $("#rule");

    if (showBuilder) {
        ruleDiv.append("<div id='RuleType'></div>");
        let ruleTypeDiv = $("#RuleType");
        ruleTypeDiv.append("<ul id='RuleTypeList'></ul>");
        let ruleTypeList = $("#RuleTypeList");
        ruleTypeList.append("<li>Builder</li>");
        ruleTypeDiv.append("<div id='Builder'></div>");
        ruleTypeList.append("<li>Coder</li>");
        ruleTypeDiv.append("<div id='CoderWrapper' style='padding-top: .3em'></div>");
        let coderWrapper = $("#CoderWrapper");
        coderWrapper.append("<div class='validationLink'><a href='#' onclick='converge()'>Reset</a></div>");
        coderWrapper.append("<div id='Coder' class='coder'></div>");
        coderWrapper.append("<div id='CompileStatus' class='compileErrors'></div>");
        $('#CompileStatus').html("Return False").css("color", "blue");
        tabStrip = ruleTypeDiv.kendoTabStrip({animation: false}).data("kendoTabStrip");
        ruleDiv.append("<div id='BuilderWarnings' style='color: darkorange; font-size: 0.85em; padding: 0.3em 0;'></div>");
    } else {
        ruleDiv.append("<div id='CoderWrapper' style='padding-top: .3em'></div>");
        let coderWrapper = $("#CoderWrapper");
        coderWrapper.append("<div id='Coder' class='coder'></div>");
        coderWrapper.append("<div id='CompileStatus' class='compileErrors'></div>");
        $('#CompileStatus').html("Return False").css("color", "blue");
    }

    const scriptPromises = [
        loadScript('/js/ace/ace.js').then(function () {
            ace.config.set('basePath', '/js/ace/');
            return loadScript('/js/ace/ext-language_tools.js');
        })
    ];

    if (showBuilder) {
        scriptPromises.push(
            loadScript('/js/builder/query-builder.standalone.min.js').then(function () {
                $('<link/>', {
                    rel: 'stylesheet',
                    type: 'text/css',
                    href: '/styles/query-builder.default.min.css'
                }).appendTo('head');
            })
        );
    }

    Promise.all([loadCompletions(), ...scriptPromises])
        .then(function (results) {
            completions = results[0];

            initCoder(data);

            if (showBuilder) {
                initBuilder(data);

                if (data) {
                    ruleType = data.ruleType;
                    tabStrip.select(ruleType - 1);
                    checkDivergence();
                } else {
                    tabStrip.select(0);
                    ruleType = 1;
                }

                const $builderWarnings = $("#BuilderWarnings");
                if (ruleType === 2) {
                    $builderWarnings.hide();
                } else {
                    $builderWarnings.show();
                }

                tabStrip.bind("select", function (e) {
                    setRuleType($(e.item).index() + 1);
                });
            }
        })
        .catch(function (err) {
            console.error('Failed to initialise builder/coder:', err);
            $('#BuilderWarnings').text('⚠ Failed to load rule editor. Please refresh the page.');
        });
}

//# sourceURL=BuilderCoder.js