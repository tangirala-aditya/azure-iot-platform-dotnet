// Copyright (c) Microsoft. All rights reserved.

import Config from "app.config";
import { SoftSelectLinkRenderer } from "components/shared/cellRenderers";
import { checkboxColumn } from "components/shared/pcsGrid/pcsGridConfig";

export const defaultDeviceColumns = [
    checkboxColumn,
    {
        headerName: "devices.grid.deviceName",
        field: "id",
        sort: "asc",
        cellRendererFramework: SoftSelectLinkRenderer,
        suppressSizeToFit: true,
    }
];

/** Default column definitions*/
export const defaultColDef = {
    sortable: true,
    lockPinned: true,
    resizable: true,
};

/** Given a device object, extract and return the device Id */
export const getSoftSelectId = ({ Id }) => Id;

/** Shared device grid AgGrid properties */
export const defaultDeviceGridProps = {
    enableColResize: true,
    multiSelect: true,
    pagination: true,
    paginationPageSize: Config.paginationPageSize,
    rowSelection: "multiple",
};
