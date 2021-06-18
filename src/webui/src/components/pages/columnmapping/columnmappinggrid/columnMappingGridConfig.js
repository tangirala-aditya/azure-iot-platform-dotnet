// Copyright (c) Microsoft. All rights reserved.

import Config from "app.config";
import {
    TimeRenderer,
    SoftSelectLinkRenderer,
} from "components/shared/cellRenderers";
import { gridValueFormatters } from "components/shared/pcsGrid/pcsGridConfig";

const { checkForEmpty } = gridValueFormatters;

/** A collection of column definitions for the devices grid */
export const columnMappingGridColumnDefs = {
    id: {
        headerName: "Name",
        field: "id",
        cellRendererFramework: SoftSelectLinkRenderer,
        suppressSizeToFit: true,
    },
    createdBy: {
        headerName: "Created By",
        field: "createdBy",
        valueFormatter: ({ value }) => checkForEmpty(value),
    },
    createdDate: {
        headerName: "Created Date",
        field: "createdDate",
        cellRendererFramework: TimeRenderer,
    },
};

/** Default column definitions*/
export const defaultColDef = {
    sortable: true,
    lockPinned: true,
    resizable: true,
};

/** Given a device object, extract and return the device Id */
export const getSoftSelectId = ({ Id }) => Id;

/** Shared device grid AgGrid properties */
export const defaultColumnMappingGridProps = {
    enableColResize: true,
    multiSelect: true,
    pagination: true,
    paginationPageSize: Config.paginationPageSize,
    rowSelection: "multiple",
};
