// Copyright (c) Microsoft. All rights reserved.

import Config from "app.config";
import {
    IsSimulatedRenderer,
    ConnectionStatusRenderer,
    TimeRenderer,
    SoftSelectLinkRenderer,
    IsActivePackageRenderer,
} from "components/shared/cellRenderers";
import {
    EMPTY_FIELD_VAL,
    gridValueFormatters,
} from "components/shared/pcsGrid/pcsGridConfig";

const { checkForEmpty } = gridValueFormatters;

/** A collection of column definitions for the modules grid */
export const modulesColumnDefs = {
    id: {
        headerName: "devices.grid.deviceName",
        field: "moduleId",
        sort: "asc",
        cellRendererFramework: SoftSelectLinkRenderer,
    },
    isSimulated: {
        headerName: "devices.grid.simulated",
        field: "isSimulated",
        cellRendererFramework: IsSimulatedRenderer,
    },
    deviceType: {
        headerName: "devices.grid.deviceType",
        field: "type",
        valueFormatter: ({ value }) => checkForEmpty(value),
    },
    firmware: {
        headerName: "devices.grid.firmware",
        field: "firmware",
        valueFormatter: ({ value }) => checkForEmpty(value),
    },
    telemetry: {
        headerName: "devices.grid.telemetry",
        field: "telemetry",
        valueFormatter: ({ value }) =>
            Object.keys(value || {}).join("; ") || EMPTY_FIELD_VAL,
    },
    isEdge: {
        headerName: "isEdge",
        field: "isEdgeDevice",
        cellRendererFramework: IsActivePackageRenderer,
    },
    status: {
        headerName: "devices.grid.status",
        field: "connected",
        cellRendererFramework: ConnectionStatusRenderer,
    },
    lastConnection: {
        headerName: "devices.grid.lastConnection",
        field: "lastActivity",
        cellRendererFramework: TimeRenderer,
    },
};

/** Given a device object, extract and return the device Id */
export const getSoftSelectId = ({ Id }) => Id;

/** Shared device grid AgGrid properties */
export const defaultModulesGridProps = {
    enableColResize: true,
    multiSelect: true,
    pagination: true,
    paginationPageSize: Config.paginationPageSize,
    rowSelection: "multiple",
};
