// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import Config from "app.config";

import { compareByProperty } from "utilities";
import {
    SeverityRenderer,
    RuleStatusRenderer,
    LastTriggerRenderer,
    LinkRenderer,
    SoftSelectLinkRenderer,
} from "components/shared/cellRenderers";
export const LAST_TRIGGER_DEFAULT_WIDTH = 310;

export const rulesColumnDefs = {
    ruleName: {
        headerName: "rules.grid.ruleName",
        field: "name",
        sort: "asc",
        cellRendererFramework: SoftSelectLinkRenderer,
    },
    description: {
        headerName: "rules.grid.description",
        field: "description",
    },
    severity: {
        headerName: "rules.grid.severity",
        field: "severity",
        cellRendererFramework: SeverityRenderer,
    },
    severityIconOnly: {
        headerName: "rules.grid.severity",
        field: "severity",
        cellRendererFramework: (props) => (
            <SeverityRenderer {...props} iconOnly={true} />
        ),
    },
    filter: {
        headerName: "rules.grid.deviceGroup",
        field: "groupId",
        valueFormatter: ({ value, context: { deviceGroups } }) => {
            if (!deviceGroups) {
                return value;
            }

            const deviceGroup = deviceGroups.find(
                (group) => group.id === value
            );
            return (deviceGroup || {}).displayName || value;
        },
    },
    trigger: {
        headerName: "rules.grid.trigger",
        field: "sortableConditions",
        cellClass: "capitalize-cell",
    },
    notificationType: {
        headerName: "rules.grid.notificationType",
        field: "type",
        valueFormatter: ({ value, context: { t } }) =>
            value || t("rules.grid.maintenanceLog"),
    },
    status: {
        headerName: "rules.grid.status",
        field: "status",
        cellRendererFramework: RuleStatusRenderer,
    },
    alertStatus: {
        headerName: "rules.grid.status",
        field: "status",
        cellClass: "capitalize-cell",
    },
    lastTrigger: {
        headerName: "rules.grid.lastTrigger",
        field: "lastTrigger",
        cellRendererFramework: LastTriggerRenderer,
        comparator: compareByProperty("response", true),
        width: LAST_TRIGGER_DEFAULT_WIDTH,
    },
    explore: {
        headerName: "rules.grid.explore",
        field: "ruleId",
        cellRendererFramework: (props) => (
            <LinkRenderer {...props} to={`/maintenance/rule/${props.value}`} />
        ),
    },
};

/** Default column definitions*/
export const defaultColDef = {
    sortable: true,
    lockPinned: true,
    resizable: true,
};

export const defaultRulesGridProps = {
    enableColResize: true,
    multiSelect: true,
    pagination: true,
    paginationPageSize: Config.paginationPageSize,
    rowSelection: "multiple",
};
