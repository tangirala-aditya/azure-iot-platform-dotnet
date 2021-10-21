// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { AgGridReact } from "ag-grid-react";
import * as Rx from "rxjs";
import Config from "app.config";
import { isFunc } from "utilities";
import { Indicator } from "../indicator/indicator";
import { ROW_HEIGHT } from "components/shared/pcsGrid/pcsGridConfig";
import { SearchInput } from "components/shared";

import "../../../../node_modules/ag-grid-community/dist/styles/ag-grid.scss";
import "../../../../node_modules/ag-grid-community/dist/styles/ag-theme-alpine/sass/ag-theme-alpine.scss";
import { ComponentArray } from "../componentArray/componentArray";
import { debounceTime, filter } from "rxjs/operators";
import { ActionButton } from "@fluentui/react/lib/Button";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./pcsGrid.module.scss"));

/**
 * PcsGrid is a helper wrapper around AgGrid. The primary functionality of this wrapper
 * is to allow easy reuse of the pcs dark grid theme. To see params, read the AgGrid docs.
 *
 * Props:
 *  getSoftSelectId: A method that when provided with the a row data object returns an id for that object
 *  softSelectId: The ID of the row data to be soft selected
 *  onHardSelectChange: Fires when rows are hard selected
 *  onSoftSelectChange: Fires when a row is soft selected
 * TODO (stpryor): Add design pagination
 */
export class PcsGrid extends Component {
    constructor(props) {
        super(props);
        this.state = {
            currentSoftSelectId: undefined,
        };

        this.defaultPcsGridProps = {
            suppressDragLeaveHidesColumns: true,
            suppressCellSelection: true,
            suppressClickEdit: true,
            suppressRowClickSelection: true, // Suppress so that a row is only selectable by checking the checkbox
            suppressLoadingOverlay: true,
            suppressNoRowsOverlay: true,
        };

        this.subscriptions = [];
        this.resizeEvents = new Rx.Subject();
    }

    componentDidMount() {
        this.subscriptions.push(
            this.resizeEvents
                .pipe(
                    debounceTime(Config.gridResizeDebounceTime),
                    filter(
                        () =>
                            !!this.gridApi &&
                            !!this.props.sizeColumnsToFit &&
                            window.outerWidth >= Config.gridMinResize
                    )
                )
                .subscribe(() => this.gridApi.sizeColumnsToFit())
        );
        window.addEventListener("resize", this.registerResizeEvent);
    }

    componentWillUnmount() {
        window.removeEventListener("resize", this.registerResizeEvent);
        this.subscriptions.forEach((sub) => sub.unsubscribe());
    }

    registerResizeEvent = () => this.resizeEvents.next("r");

    /** When new props are passed in, check if the soft select state needs to be updated */
    UNSAFE_componentWillReceiveProps(nextProps) {
        if (this.state.currentSoftSelectId !== nextProps.softSelectId) {
            this.setState(
                { currentSoftSelectId: nextProps.softSelectId },
                this.refreshRows
            );
        }
        // Resize the grid if updating from 0 row data to 1+ rowData
        if (
            (nextProps.rowData &&
                nextProps.rowData.length &&
                (!this.props.rowData || !this.props.rowData.length)) ||
            (nextProps.columnDefs &&
                nextProps.columnDefs.length > 0 &&
                JSON.stringify(nextProps.columnDefs) !==
                    JSON.stringify(this.props.columnDefs))
        ) {
            if (this.gridApi) {
                this.gridApi.setColumnDefs(nextProps.columnDefs);
            }
            this.resizeEvents.next("r");
        }
    }

    /** Save the gridApi locally on load */
    onGridReady = (gridReadyEvent) => {
        this.gridApi = gridReadyEvent.api;
        this.gridColumnApi = gridReadyEvent.columnApi;
        if (this.props.sizeColumnsToFit) {
            this.resizeEvents.next("r");
        }
        if (isFunc(this.props.onGridReady)) {
            this.props.onGridReady(gridReadyEvent);
        }
    };

    /** Invoked when data is rendered for the first time */
    onFirstDataRendered = () => {
        if (isFunc(this.props.onFirstDataRendered)) {
            this.props.onFirstDataRendered();
        }
    };

    /**
     * Refreshes the grid to update soft select CSS states
     * Forces and update event
     */
    refreshRows = () => {
        if (this.gridApi && isFunc(this.gridApi.applyTransaction)) {
            this.gridApi.setQuickFilter("");
            this.gridApi.applyTransaction({ update: [] });
        }
    };

    /** When a row is hard selected, try to fire a hard select event, plus any props callbacks */
    onSelectionChanged = () => {
        const { onHardSelectChange, onSelectionChanged } = this.props;
        if (isFunc(onHardSelectChange)) {
            onHardSelectChange(this.gridApi.getSelectedRows());
        }
        if (isFunc(onSelectionChanged)) {
            onSelectionChanged();
        }
    };

    /** When a row is clicked, select the row unless a soft select link was clicked */
    onRowClicked = (rowEvent) => {
        const className = rowEvent.event.target.className;
        if (className.indexOf && className.indexOf("soft-select-link") === -1) {
            const { onRowClicked } = this.props;
            if (isFunc(onRowClicked)) {
                onRowClicked(rowEvent);
            }
        }
    };

    expandColumns = () => {
        var allColumnIds = [];
        var colTotalWidthAfterExpand = 0;
        this.gridColumnApi.getAllColumns().forEach(function (column) {
            allColumnIds.push(column.colId);
        });
        this.gridColumnApi.autoSizeColumns(allColumnIds, false);
        this.gridColumnApi.getAllColumns().forEach(function (column) {
            colTotalWidthAfterExpand =
                colTotalWidthAfterExpand + column.actualWidth;
        });
        if (
            colTotalWidthAfterExpand <= this.gridApi.gridPanel.getCenterWidth()
        ) {
            this.resizeEvents.next("r");
        }
    };

    searchOnChange = ({ target: { value } }) => {
        if (this.gridApi) {
            this.gridApi.setQuickFilter(value);
        }
    };

    render() {
        const {
                onSoftSelectChange,
                getSoftSelectId,
                softSelectId,
                context = {},
                style,
                gridControls,
                ...restProps
            } = this.props,
            gridParams = {
                ...this.defaultPcsGridProps,
                ...restProps,
                headerHeight: ROW_HEIGHT,
                rowHeight: ROW_HEIGHT,
                onGridReady: this.onGridReady,
                onFirstDataRendered: this.onFirstDataRendered,
                onSelectionChanged: this.onSelectionChanged,
                onRowClicked: this.onRowClicked,
                rowClassRules: {
                    "pcs-row-soft-selected": ({ data }) =>
                        isFunc(getSoftSelectId)
                            ? getSoftSelectId(data) === softSelectId
                            : false,
                },
                context: {
                    ...context,
                    onSoftSelectChange, // Pass soft select logic to cell renderers
                    getSoftSelectId, // Pass soft select id logic to cell renderers
                },
                accentedSort: true,
                applyColumnDefOrder: true,
            },
            { rowData, pcsLoadingTemplate } = this.props,
            loadingContainer = (
                <div className={css("pcs-grid-loading-container")}>
                    {!pcsLoadingTemplate ? <Indicator /> : pcsLoadingTemplate}
                </div>
            );
        return (
            <ComponentArray>
                {rowData && (
                    <div className={css("flex-container")}>
                        {this.props.searchPlaceholder && (
                            <div className={css("flex-child")}>
                                <SearchInput
                                    onChange={this.searchOnChange}
                                    placeholder={this.props.searchPlaceholder}
                                    aria-label={this.props.searchAreaLabel}
                                />
                            </div>
                        )}
                        <div className={css("flex-child")}>
                            {gridControls}
                            <ActionButton
                                iconProps={{ iconName: "ChevronRightMed" }}
                                onClick={this.expandColumns}
                                text="Expand Columns"
                                className={css("expand-columns")}
                            />
                        </div>
                    </div>
                )}
                <div
                    className={css("pcs-grid-container", "ag-theme-alpine", {
                        "movable-columns": !gridParams.suppressMovableColumns,
                    })}
                    style={style}
                >
                    {!rowData ? loadingContainer : ""}
                    <AgGridReact {...gridParams} />
                </div>
            </ComponentArray>
        );
    }
}
