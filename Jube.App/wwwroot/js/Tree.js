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

const browserWindow = $(window);
let ChildDescription;
let ChildURLAdd;
let ChildURLUpdate;
let QueryString;
let serviceRoot = "../api/TreeChildren/";
let topLevel;
let topLevelId;
let HasThirdLevel = false;
let ThirdLevelDatasource;
let EntityAnalysisModelChildrenChildren;
const Page = getUrlVars()["Page"];

function GetColor(e) {
    const items = e.sender.items();
    for (let j = 0; j < items.length; j++) {
        const item = $(items[j]);
        const dataItem = e.sender.dataItem(item);
        const color = dataItem.get("color");

        item.css("color", color);
    }
}

$(document).ready(function () {
    switch (Page) {
        case 'AbstractionRule':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Abstraction Rule';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelAbstractionRule";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelAbstractionRule";
            QueryString = 0;
            break;
        case 'AbstractionCalculation':
            serviceRoot = serviceRoot + Page;
            ChildDescription = 'Abstraction Calculation';
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelAbstractionCalculation";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelAbstractionCalculation";
            QueryString = 0;
            break;
        case 'ActivationRule':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Activation Rule';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelActivationRule";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelActivationRule";
            QueryString = 0;
            break;
        case 'GatewayRule':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Gateway Rule';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelGatewayRule";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelGatewayRule";
            QueryString = 0;
            break;
        case 'TTLCounter':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'TTL Counters';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelTTLCounter";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelTTLCounter";
            QueryString = 0;
            break;
        case 'RequestXPath':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Request XPath';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelRequestXPath";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelRequestXPath";
            QueryString = 0;
            break;
        case 'List':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "guid";
            ChildDescription = 'Lists';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelList";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelList";
            QueryString = 0;
            break;
        case 'Dictionary':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "guid";
            ChildDescription = 'Dictionary';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelDictionary";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelDictionary";
            QueryString = 0;
            break;
        case 'InlineScript':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Inline Scripts';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelInlineScript";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelInlineScript";
            QueryString = 0;
            break;
        case 'InlineFunction':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Inline Function';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelInlineFunction";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelInlineFunction";
            QueryString = 0;
            break;
        case 'Sanctions':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Sanctions';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelSanction";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelSanction";
            QueryString = 0;
            break;
        case 'Exhaustive':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Exhaustive';
            ChildURLAdd = "/Model/Frame/Exhaustive";
            ChildURLUpdate = "/Model/Frame/Exhaustive";
            QueryString = 0;
            break;
        case 'Tag':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Tag';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelTag";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelTag";
            QueryString = 0;
            break;
        case 'Reprocessing':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Reprocessing';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelReprocessingRule";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelReprocessingRule";
            QueryString = 0;
            break;
        case 'Adaptation':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Adaptation';
            ChildURLAdd = "/Model/Frame/EntityAnalysisModelAdaptation";
            ChildURLUpdate = "/Model/Frame/EntityAnalysisModelAdaptation";
            QueryString = 0;
            break;
        case 'CaseWorkflow':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Cases Workflow';
            ChildURLAdd = "/Model/Frame/CaseWorkflow";
            ChildURLUpdate = "/Model/Frame/CaseWorkflow";
            QueryString = 0;
            break;
        case 'VisualisationRegistryParameter':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/VisualisationRegistry";
            topLevelId = "id";
            ChildDescription = 'Visualisation Registry Parameter';
            ChildURLAdd = "/Administration/Frame/VisualisationRegistryParameter";
            ChildURLUpdate = "/Administration/Frame/VisualisationRegistryParameter";
            QueryString = 0;
            break;
        case 'RoleRegistryPermission':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/RoleRegistry";
            topLevelId = "id";
            ChildDescription = 'Role Registry Permission';
            ChildURLAdd = "/Administration/Frame/RoleRegistryPermission";
            ChildURLUpdate = "/Administration/Frame/RoleRegistryPermission";
            QueryString = 0;
            break;
        case 'VisualisationRegistryDatasource':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/VisualisationRegistry";
            topLevelId = "id";
            ChildDescription = 'Visualisation Registry Parameter';
            ChildURLAdd = "/Administration/Frame/VisualisationRegistryDatasource";
            ChildURLUpdate = "/Administration/Frame/VisualisationRegistryDatasource";
            QueryString = 0;
            break;
        case 'UserRegistry':
            serviceRoot = serviceRoot + Page;
            topLevel = "/api/RoleRegistry";
            topLevelId = "guid";
            ChildDescription = 'User Registry';
            ChildURLAdd = "/Administration/Frame/UserRegistry";
            ChildURLUpdate = "/Administration/Frame/UserRegistry";
            QueryString = 0;
            break;
        case 'CaseWorkflowForm':
            HasThirdLevel = true;

            ThirdLevelDatasource = {
                transport: {
                    read: {
                        url: serviceRoot + "CaseWorkflowForm",
                        dataType: "json"
                    }
                },
                schema: {
                    model: {
                        id: "key",
                        hasChildren: false
                    }
                }
            };

            serviceRoot = serviceRoot + 'CaseWorkflow';
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Case Workflow Form';
            ChildURLAdd = "/Model/Frame/CaseWorkflowForm";
            ChildURLUpdate = "/Model/Frame/CaseWorkflowForm";
            QueryString = 0;
            break;
        case 'CaseWorkflowAction':
            HasThirdLevel = true;

            ThirdLevelDatasource = {
                transport: {
                    read: {
                        url: serviceRoot + "CaseWorkflowAction",
                        dataType: "json"
                    }
                },
                schema: {
                    model: {
                        id: "key",
                        hasChildren: false
                    }
                }
            };

            serviceRoot = serviceRoot + 'CaseWorkflow';
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Case Workflow Action';
            ChildURLAdd = "/Model/Frame/CaseWorkflowAction";
            ChildURLUpdate = "/Model/Frame/CaseWorkflowAction";
            QueryString = 0;
            break;
        case 'CaseWorkflowDisplay':
            HasThirdLevel = true;

            ThirdLevelDatasource = {
                transport: {
                    read: {
                        url: serviceRoot + "CaseWorkflowDisplay",
                        dataType: "json"
                    }
                },
                schema: {
                    model: {
                        id: "key",
                        hasChildren: false
                    }
                }
            };

            serviceRoot = serviceRoot + 'CaseWorkflow';
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Case Workflow Display';
            ChildURLAdd = "/Model/Frame/CaseWorkflowDisplay";
            ChildURLUpdate = "/Model/Frame/CaseWorkflowDisplay";
            QueryString = 0;
            break;
        case 'CaseWorkflowMacro':
            HasThirdLevel = true;

            ThirdLevelDatasource = {
                transport: {
                    read: {
                        url: serviceRoot + "CaseWorkflowMacro",
                        dataType: "json"
                    }
                },
                schema: {
                    model: {
                        id: "key",
                        hasChildren: false
                    }
                }
            };

            serviceRoot = serviceRoot + 'CaseWorkflow';
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Case Workflow Macro';
            ChildURLAdd = "/Model/Frame/CaseWorkflowMacro";
            ChildURLUpdate = "/Model/Frame/CaseWorkflowMacro";
            QueryString = 0;
            break;
        case 'CaseWorkflowFilter':
            HasThirdLevel = true;

            ThirdLevelDatasource = {
                transport: {
                    read: {
                        url: serviceRoot + "CaseWorkflowFilter",
                        dataType: "json"
                    }
                },
                schema: {
                    model: {
                        id: "key",
                        hasChildren: false
                    }
                }
            };

            serviceRoot = serviceRoot + 'CaseWorkflow';
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Case Workflow Filter';
            ChildURLAdd = "/Model/Frame/CaseWorkflowFilter";
            ChildURLUpdate = "/Model/Frame/CaseWorkflowFilter";
            QueryString = 0;
            break;
        case 'CaseWorkflowXPath':
            HasThirdLevel = true;

            ThirdLevelDatasource = {
                transport: {
                    read: {
                        url: serviceRoot + "CaseWorkflowXPath",
                        dataType: "json"
                    }
                },
                schema: {
                    model: {
                        id: "key",
                        hasChildren: false
                    }
                }
            };

            serviceRoot = serviceRoot + 'CaseWorkflow';
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Case Workflow XPath';
            ChildURLAdd = "/Model/Frame/CaseWorkflowXPath";
            ChildURLUpdate = "/Model/Frame/CaseWorkflowXPath";
            QueryString = 0;
            break;
        case 'CaseWorkflowStatus':
            HasThirdLevel = true;

            ThirdLevelDatasource = {
                transport: {
                    read: {
                        url: serviceRoot + "CaseWorkflowStatus",
                        dataType: "json"
                    }
                },
                schema: {
                    model: {
                        id: "key",
                        hasChildren: false
                    }
                }
            };

            serviceRoot = serviceRoot + 'CaseWorkflow';
            topLevel = "/api/EntityAnalysisModel";
            topLevelId = "id";
            ChildDescription = 'Case Workflow Status';
            ChildURLAdd = "/Model/Frame/CaseWorkflowStatus";
            ChildURLUpdate = "/Model/Frame/CaseWorkflowStatus";
            QueryString = 0;
            break;
    }

    const EntityAnalysisModelChildren = {
        transport: {
            read: {
                url: serviceRoot,
                dataType: "json"
            }
        },
        schema: {
            model: {
                id: "key",
                hasChildren: HasThirdLevel,
                children: ThirdLevelDatasource
            }
        }
    };

    const EntityAnalysisModel = new kendo.data.HierarchicalDataSource({
        transport: {
            read: {
                url: topLevel,
                dataType: "json"
            }
        },
        schema: {
            model: {
                id: topLevelId,
                hasChildren: true,
                children: EntityAnalysisModelChildren
            }
        }
    });

    var tree = $("#Tree").kendoTreeView({
        dataSource: EntityAnalysisModel,
        dataTextField: "name",
        select: onSelect,
        collapse: onCollapse,
        dataBound: GetColor
    });

    $("#TitleDiv").html(ChildDescription);

    function onCollapse(e) {
        const dataItemCollapsed = this.dataItem(e.node);
        dataItemCollapsed.loaded(false);
    }

    const splitter = $("#Splitter").kendoSplitter({
        panes: [
            {collapsible: true, size: 300},
            {}
        ]
    });

    function onSelect(e) {
        const kitems = $(e.node).add($(e.node).parentsUntil('.k-treeview', '.k-item'));

        const texts = $.map(kitems,
            function (kitem) {
                return $(kitem).find('>div span.k-in').text();
            });

        const parentAmount = texts.length - 1;
        const treeview = tree.getKendoTreeView();
        const item = treeview.dataItem(e.node);

        if (parentAmount > 1 && HasThirdLevel) {
            if (QueryString === 1) {
                splitter.data("kendoSplitter").ajaxRequest("#ContentPane", ChildURLUpdate + item.Key);
            } else {
                splitter.data("kendoSplitter").ajaxRequest("#ContentPane", ChildURLUpdate);
            }
        } else if (parentAmount > 0 && HasThirdLevel === false) {
            if (QueryString === 1) {
                splitter.data("kendoSplitter").ajaxRequest("#ContentPane", ChildURLUpdate + item.Key);
            } else {
                splitter.data("kendoSplitter").ajaxRequest("#ContentPane", ChildURLUpdate);
            }
        } else if (parentAmount === 1 && HasThirdLevel) {
            if (QueryString === 1) {
                splitter.data("kendoSplitter").ajaxRequest("#ContentPane", ChildURLAdd + item.Key);
            } else {
                splitter.data("kendoSplitter").ajaxRequest("#ContentPane", ChildURLAdd);
            }
        } else if (parentAmount === 0 && HasThirdLevel === false) {
            if (QueryString === 1) {
                splitter.data("kendoSplitter").ajaxRequest("#ContentPane", ChildURLAdd + item.Key);
            } else {
                splitter.data("kendoSplitter").ajaxRequest("#ContentPane", ChildURLAdd);
            }
        } else if (parentAmount === 0 && HasThirdLevel) {
            return false;
        }
    }

    function resizeSplitter() {
        const outerSplitter = splitter.data("kendoSplitter");
        let headerOffset;
        if ($('#TenantsWrapper').length) {
            headerOffset = 110;
        } else {
            headerOffset = 80;
        }

        outerSplitter.wrapper.height(browserWindow.height() - headerOffset);
        outerSplitter.resize();
    }

    resizeSplitter();
    browserWindow.resize(resizeSplitter);
});

function getUrlVars() {
    const vars = [];
    let hash;
    const hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
    for (let i = 0; i < hashes.length; i++) {
        hash = hashes[i].split('=');
        vars.push(hash[0]);
        vars[hash[0]] = hash[1];
    }
    return vars;
}

function DeleteNode(child, terminator) {
    const treeview = $("#Tree").getKendoTreeView();
    const $contentPane = $("#ContentPane");
    let parentNode;
    let childNode;
    const nodes = treeview.dataSource.view();
    for (let i = 0; i < nodes.length; i++) {
        parentNode = nodes[i];
        let j;
        let item;
        if (HasThirdLevel) {
            if (parentNode.expanded) {
                const childParentNodes = parentNode.children.data();
                for (j = 0; j < childParentNodes.length; j++) {
                    if (childParentNodes[j].expanded) {
                        const childChildParentNodes = childParentNodes[j].children.data();
                        for (let k = 0; k < childChildParentNodes.length; k++) {
                            if (childChildParentNodes[k].id === child) {
                                item = treeview.findByUid(childChildParentNodes[k].uid);
                                treeview.remove(item);
                                if (terminator === 1) {
                                    $contentPane.html('Entry ' + child + ' has been deleted.');
                                }
                            }
                        }
                    } else {
                        if (terminator === 1) {
                            $contentPane.html('Entry ' + child + ' has been deleted.');
                        }
                    }
                }
            }
        } else {
            if (parentNode.hasChildren) {
                if (parentNode.expanded) {
                    const childNodes = parentNode.children.data();
                    for (j = 0; j < childNodes.length; j++) {
                        childNode = childNodes[j];
                        if (childNode.key === child) {
                            item = treeview.findByUid(childNode.uid);
                            treeview.remove(item);
                            if (terminator === 1) {
                                $contentPane.html('Entry ' + child + ' has been deleted.');
                            }
                        }
                    }
                } else {
                    if (terminator === 1) {
                        $contentPane.html('Entry ' + child + ' has been deleted.');
                    }
                }
            }
        }
    }
}

function GetSelectedParentID() {
    const treeview = $("#Tree").getKendoTreeView();
    const selectedNode = treeview.select();
    const item = treeview.dataItem(selectedNode);
    const kitems = $(selectedNode).add($(selectedNode).parentsUntil('.k-treeview', '.k-item'));

    const texts = $.map(kitems,
        function (kitem) {
            return $(kitem).find('>div span.k-in').text();
        });

    const parentAmount = texts.length - 1;
    if (parentAmount > 0) {
        if (parentAmount > 1 && HasThirdLevel) {
            return item.parentNode().key;
        } else if (parentAmount > 0 && HasThirdLevel === false) {
            return item.parentNode()[topLevelId];
        } else if (parentAmount === 1 && HasThirdLevel) {
            return item.key;
        } else if (parentAmount === 1 && HasThirdLevel === false) {
            return item[topLevelId];
        }
    } else {
        return item[topLevelId];
    }
}

function GetSelectedChildID() {
    const treeview = $("#Tree").getKendoTreeView();
    const selectedNode = treeview.select();
    const item = treeview.dataItem(selectedNode);
    const kitems = $(selectedNode).add($(selectedNode).parentsUntil('.k-treeview', '.k-item'));

    const texts = $.map(kitems,
        function (kitem) {
            return $(kitem).find('>div span.k-in').text();
        });

    const parentAmount = texts.length - 1;

    if (parentAmount > 1 && HasThirdLevel) {
        return item.key;
    } else if (parentAmount > 0 && HasThirdLevel === false) {
        return item.key;
    }
}

function AddNode(parent, child, name) {
    const treeview = $("#Tree").getKendoTreeView();
    let parentNode;
    const parentNodes = treeview.dataSource.view();
    for (let i = 0; i < parentNodes.length; i++) {
        parentNode = parentNodes[i];
        let j;
        let item;
        if (HasThirdLevel) {
            if (parentNode.expanded) {
                const childParentNodes = parentNode.children.data();
                for (j = 0; j < childParentNodes.length; j++) {
                    if (childParentNodes[j].expanded) {
                        if (childParentNodes[j].key === parent) {
                            childParentNodes[j].append({name: name, key: child});

                            const childChildParentNodes = childParentNodes[j].children.data();
                            for (let k = 0; k < childChildParentNodes.length; k++) {
                                if (childChildParentNodes[k].id === child) {
                                    item = treeview.findByUid(childChildParentNodes[k].uid);
                                    treeview.select(item);
                                }
                            }
                        }
                    }
                }
            }
        } else {
            if (parentNode[topLevelId] === parent) {
                if (parentNode.expanded) {
                    parentNode.append({name: name, key: child});
                    const childNodes = parentNode.children.data();
                    for (j = 0; j < childNodes.length; j++) {
                        if (childNodes[j].id === child) {
                            item = treeview.findByUid(childNodes[j].uid);
                            treeview.select(item);
                        }
                    }
                }
            }
        }
    }
}

function SearchTreeNodes(query) {
    const tree = $("#Tree").data("kendoTreeView");
    if (!tree) return;

    const ds = tree.dataSource;

    query = (query || "").toString().trim().toLowerCase();

    if (!query.length) {
        ds.filter([]);
        tree.dataSource.read();
    } else {
        ds.filter({
            logic: "or",
            filters: [
                {
                    field: "name",
                    operator: function (value) {
                        return (value || "").toString().toLowerCase().indexOf(query) >= 0;
                    }
                }
            ]
        });

        sortTreeNodes(ds.view(), "name");
        tree.expand(".k-item");
    }
}

function sortTreeNodes(data, field) {
    data.sort((a, b) => {
        const nameA = (a[field] || "").toLowerCase();
        const nameB = (b[field] || "").toLowerCase();
        if (nameA < nameB) return -1;
        if (nameA > nameB) return 1;
        return 0;
    });

    data.forEach(item => {
        if (item.hasChildren && item.children) {
            sortTreeNodes(item.children.data(), field);
        }
    });
}

function attachOriginalIndex(nodes) {
    nodes.forEach((node, index) => {
        node.originalIndex = index;
        if (node.hasChildren && node.children) {
            attachOriginalIndex(node.children.data());
        }
    });
}

//# sourceURL=Tree.js