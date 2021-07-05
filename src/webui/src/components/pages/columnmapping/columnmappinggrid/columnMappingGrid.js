// Copyright (c) Microsoft. All rights reserved.

import React, { Component, Fragment } from "react";
import { toDiagnosticsModel } from "services/models";
import { PcsGrid, Btn } from "components/shared";
import {
    defaultColumnMappingGridProps,
    columnMappingGridColumnDefs,
    defaultColDef,
} from "./columnMappingGridConfig";
import { isFunc, translateColumnDefs, svgs } from "utilities";
const classnames = require("classnames/bind");
const css = classnames.bind(require("../columnMapping.module.scss"));

/**
 * A grid for displaying users
 *
 * Encapsulates the PcsGrid props
 */
export class ColumnMappingsGrid extends Component {
    constructor(props) {
        super(props);

        // Default user grid columns
        this.columnDefs = [
            columnMappingGridColumnDefs.name,
            columnMappingGridColumnDefs.createdBy,
            columnMappingGridColumnDefs.createdDate,
        ];
    }

    /**
     * Get the grid api options
     *
     * @param {Object} gridReadyEvent An object containing access to the grid APIs
     */
    onGridReady = (gridReadyEvent) => {
        this.userGridApi = gridReadyEvent.api;
        // Call the onReady props if it exists
        if (isFunc(this.props.onGridReady)) {
            this.props.onGridReady(gridReadyEvent);
        }
    };

    /**
     * Handles soft select props method
     *
     * @param userId The ID of the currently soft selected user
     */
    onSoftSelectChange = (mappingId) => {
        const { onSoftSelectChange } = this.props;
        if (mappingId) {
            this.props.history.push(`/columnMapping/edit/${mappingId}`);
        }
        if (isFunc(onSoftSelectChange)) {
            onSoftSelectChange(mappingId);
        }
    };

    /**
     * Handles context filter changes and calls any hard select props method
     *
     * @param {Array} selectedUsers A list of currently selected users
     */
    onHardSelectChange = (selectedUsers) => {
        const { onContextMenuChange, onHardSelectChange } = this.props;
        if (isFunc(onContextMenuChange)) {
            onContextMenuChange(
                selectedUsers.length > 0 ? this.contextBtns : null
            );
        }
        if (isFunc(onHardSelectChange)) {
            onHardSelectChange(selectedUsers);
        }
    };

    onColumnMoved = () => {
        this.props.logEvent(
            toDiagnosticsModel("ColumnMappings_ColumnArranged", {})
        );
    };

    onSortChanged = () => {
        this.props.logEvent(
            toDiagnosticsModel("ColumnMappings_Sort_Click", {})
        );
    };

    getSoftSelectId = ({ id } = "") => id;

    addNewColumnMapping = () => {
        this.props.history.push(`/columnMapping/add`);
    };

    render() {
        const gridProps = {
            /* Grid Properties */
            ...defaultColumnMappingGridProps,
            columnDefs: translateColumnDefs(this.props.t, this.columnDefs),
            defaultColDef: defaultColDef,
            sizeColumnsToFit: true,
            getSoftSelectId: this.getSoftSelectId,
            ...this.props, // Allow default property overrides
            immutableData: true,
            getRowNodeId: ({ id }) => id,
            context: {
                t: this.props.t,
            },
            rowData: this.props.columnMappings || [],
            /* Grid Events */
            onRowClicked: ({ node }) => node.setSelected(!node.isSelected()),
            onGridReady: this.onGridReady,
            onSoftSelectChange: this.onSoftSelectChange,
            onHardSelectChange: this.onHardSelectChange,
            onColumnMoved: this.onColumnMoved,
            onSortChanged: this.onSortChanged,
        };

        return (
            <Fragment>
                <div>
                    <Btn
                        className={css("btn-icon")}
                        svg={svgs.plus}
                        onClick={this.addNewColumnMapping}
                    >
                        Add New
                    </Btn>
                </div>
                <PcsGrid key="columnmappings-grid-key" {...gridProps} />
            </Fragment>
        );
    }
}
