// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { PcsGrid } from "components/shared";
import { translateColumnDefs } from "utilities";
import { TimeRenderer } from "components/shared/cellRenderers";

const columnDefs = [
    {
        headerName: "maintenance.deviceJobGrid.jobName",
        field: "jobId",
    },
    {
        headerName: "maintenance.deviceJobGrid.status",
        field: "jobStatus",
    },
    {
        headerName: "maintenance.deviceJobGrid.category",
        field: "category",
    },
    {
        headerName: "maintenance.deviceJobGrid.deviceGroup",
        field: "deviceGroupId",
    },
    {
        headerName: "maintenance.deviceJobGrid.parentDevice",
        field: "parentDeviceId",
    },
    {
        headerName: "maintenance.deviceJobGrid.createdBy",
        field: "createdBy",
    },
    {
        headerName: "maintenance.deviceJobGrid.createdDate",
        field: "createdDate",
        cellRendererFramework: TimeRenderer,
    },
];

export const defaultColDef = {
    sortable: true,
    lockPinned: true,
    resizable: true,
};

export const DeviceJobGrid = ({ t, ...props }) => {
    const gridProps = {
        columnDefs: translateColumnDefs(t, columnDefs),
        defaultColDef: defaultColDef,
        context: { t },
        sizeColumnsToFit: true,
        ...props,
    };
    return <PcsGrid {...gridProps} />;
};
