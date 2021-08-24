// Copyright (c) Microsoft. All rights reserved.

import React, { Component, Fragment } from "react";
import { toDiagnosticsModel } from "services/models";
import { PcsGrid, Btn, ComponentArray } from "components/shared";
import {
    defaultColumnMappingGridProps,
    columnMappingGridColumnDefs,
    defaultColDef,
} from "./columnMappingGridConfig";
import { isFunc, translateColumnDefs, svgs } from "utilities";
import { ColumnMappingDeleteContainer } from "../flyouts/columnMappingDelete";
const classnames = require("classnames/bind");
const css = classnames.bind(require("../columnMapping.module.scss"));
const closedModalState = {
    openModalName: undefined,
};
/**
 * A grid for displaying users
 *
 * Encapsulates the PcsGrid props
 */
export class ColumnMappingsGrid extends Component {
    constructor(props) {
        super(props);

        // Set the initial state
        this.state = {
            ...closedModalState,
            hardSelectedMappings: [],
        };

        // Default user grid columns
        this.columnDefs = [
            columnMappingGridColumnDefs.checkboxColumn,
            columnMappingGridColumnDefs.name,
            columnMappingGridColumnDefs.createdBy,
            columnMappingGridColumnDefs.createdDate,
        ];

        props.onContextMenuChange(this.getSingleSelectionContextBtns(false));
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
     * @param {Array} selectedMappings A list of currently selected packages
     */
    onHardSelectChange = (selectedMappings) => {
        const { onContextMenuChange, onHardSelectChange } = this.props;
        if (isFunc(onContextMenuChange)) {
            this.setState({
                hardSelectedMappings: selectedMappings,
            });
            onContextMenuChange(
                selectedMappings.length >= 1
                    ? this.getSingleSelectionContextBtns(true)
                    : this.getSingleSelectionContextBtns(false)
            );
        }
        if (isFunc(onHardSelectChange)) {
            onHardSelectChange(selectedMappings);
        }
    };

    getSingleSelectionContextBtns = (isDelete = false) => {
        return (
            <ComponentArray>
                {isDelete && (
                    <Btn
                        svg={svgs.trash}
                        onClick={this.openModal("delete-mapping")}
                    >
                        {this.props.t("columnMapping.delete.deleteButton")}
                    </Btn>
                )}
                <Btn
                    className={css("btn-icon")}
                    svg={svgs.plus}
                    onClick={this.addNewColumnMapping}
                >
                    {this.props.t("columnMapping.delete.addNewButton")}
                </Btn>
            </ComponentArray>
        );
    };

    closeModal = () => this.setState(closedModalState);

    openModal = (modalName) => () =>
        this.setState({
            openModalName: modalName,
        });

    getOpenModal = () => {
        if (
            this.state.openModalName === "delete-mapping" &&
            this.state.hardSelectedMappings[0]
        ) {
            return (
                <ColumnMappingDeleteContainer
                    columnMappingId={this.state.hardSelectedMappings[0].id}
                    onClose={this.closeModal}
                    onDelete={this.closeModal}
                    title={this.props.t("deployments.modals.delete.title")}
                    deleteInfo={this.props.t(
                        "deployments.modals.delete.gridInfo"
                    )}
                />
            );
        }
        return null;
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
                <PcsGrid key="columnmappings-grid-key" {...gridProps} />
                {this.getOpenModal()}
            </Fragment>
        );
    }
}
