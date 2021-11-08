// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { permissions, toDiagnosticsModel } from "services/models";
import { PcsGrid } from "components/shared";
import {
    modulesColumnDefs,
    defaultModulesGridProps,
} from "./modulesGridConfig";
import {
    isFunc,
    translateColumnDefs,
    getFlyoutNameParam,
    getParamByName,
    getFlyoutLink,
    userHasPermission,
} from "utilities";
import { ModuleDetailsContainer } from "components/pages/devices/flyouts/moduleDetails/moduleDetails.container";

const closedFlyoutState = {
    openFlyoutName: undefined,
    softSelectedModuleId: undefined,
};

/**
 * A grid for displaying devices
 *
 * Encapsulates the PcsGrid props
 */
export class ModulesGrid extends Component {
    constructor(props) {
        super(props);

        // Set the initial state
        this.state = {
            ...closedFlyoutState,
        };

        // Default device grid columns
        this.columnDefs = [modulesColumnDefs.id];
    }

    onFirstDataRendered = () => {
        if (this.props.rowData && this.props.rowData.length > 0) {
            this.getDefaultFlyout(this.props.rowData);
        }
    };

    componentWillReceiveProps(nextProps) {
        if (this.state.flyoutOpened === false) {
            this.setState({ flyoutOpened: true });
            this.getDefaultFlyout(nextProps.rowData);
        }
    }

    getDefaultFlyout(rowData) {
        const { location, userPermissions } = this.props;
        const flyoutName = getFlyoutNameParam(location);
        var isUserHasPermission = true;
        if (
            flyoutName === "jobs" &&
            !userHasPermission(permissions.createJobs, userPermissions)
        ) {
            isUserHasPermission = false;
        }
        const deviceIds = this.getDeviceIdsArray(
            getParamByName(location, "deviceId")
        );
        let devices = deviceIds
            ? rowData.filter((device) => deviceIds.includes(device.id))
            : undefined;

        if (deviceIds && deviceIds.length > devices.length) {
            const conditions = this.createCondition(
                deviceIds,
                "deviceId",
                deviceIds.length > 1 ? "LK" : "EQ"
            );
            this.props.fetchDevicesByCondition({
                data: conditions,
                insertIntoGrid: true,
            });
            this.setState({ flyoutOpened: false });
        }
        if (
            location &&
            location.search &&
            !this.state.softSelectedModuleId &&
            devices &&
            devices.length > 0 &&
            deviceIds.length === devices.length &&
            isUserHasPermission
        ) {
            this.setState({
                softSelectedModuleId: deviceIds,
                openFlyoutName: flyoutName,
                selectedDevicesForFlyout: devices,
            });
            this.selectRows(deviceIds);
        }
    }

    createCondition(deviceIDs, key, operator) {
        let data = [];
        deviceIDs.forEach((element) => {
            data.push({
                key: key,
                operator: operator,
                value: element,
            });
        });
        return data;
    }

    getDeviceIdsArray(deviceIdString) {
        return deviceIdString ? deviceIdString.split("||") : undefined;
    }

    selectRows(deviceIds) {
        this.deviceGridApi.gridOptionsWrapper.gridOptions.api.forEachNode(
            (node) =>
                deviceIds.includes(node.id) ? node.setSelected(true) : null
        );
    }

    /**
     * Get the grid api options
     *
     * @param {Object} gridReadyEvent An object containing access to the grid APIs
     */
    onGridReady = (gridReadyEvent) => {
        this.deviceGridApi = gridReadyEvent.api;
        // Call the onReady props if it exists
        if (isFunc(this.props.onGridReady)) {
            this.props.onGridReady(gridReadyEvent);
        }
    };

    openFlyout = (flyoutName) => () =>
        this.setState({
            openFlyoutName: flyoutName,
            softSelectedModuleId: undefined,
        });

    getOpenFlyout = () => {
        console.log(this.state.openFlyoutName);
        var flyoutLink = undefined;
        switch (this.state.openFlyoutName) {
            case "details":
                flyoutLink = getFlyoutLink(
                    this.props.currentTenantId,
                    this.props.activeDeviceGroupId,
                    "deviceId",
                    this.state.softSelectedModuleId,
                    "details"
                );
                return (
                    <ModuleDetailsContainer
                        key="details-module-key"
                        onClose={this.closeFlyout}
                        moduleId={this.state.softSelectedModuleId}
                        flyoutLink={flyoutLink}
                    />
                );
            default:
                return null;
        }
    };

    closeFlyout = () => {
        if (this.props.location && this.props.location.search) {
            this.props.location.search = undefined;
        }
        this.setState(closedFlyoutState);
    };

    /**
     * Handles soft select props method
     *
     * @param deviceId The ID of the currently soft selected device
     */
    onSoftSelectChange = (deviceId) => {
        const { onSoftSelectChange } = this.props;
        if (deviceId) {
            this.setState({
                openFlyoutName: "details",
                softSelectedModuleId: deviceId,
            });
        } else {
            this.closeFlyout();
        }
        if (isFunc(onSoftSelectChange)) {
            onSoftSelectChange(deviceId);
        }
    };

    /**
     * Handles context filter changes and calls any hard select props method
     *
     * @param {Array} selectedDevices A list of currently selected devices
     */
    onHardSelectChange = (selectedDevices) => {
        const { onContextMenuChange, onHardSelectChange } = this.props;
        if (isFunc(onContextMenuChange)) {
            onContextMenuChange(
                selectedDevices.length > 0 ? this.contextBtns : null
            );
        }
        if (isFunc(onHardSelectChange)) {
            onHardSelectChange(selectedDevices);
        }
    };

    onColumnMoved = () => {
        this.props.logEvent(toDiagnosticsModel("Modules_ColumnArranged", {}));
    };

    onSortChanged = () => {
        this.props.logEvent(toDiagnosticsModel("Modules_Sort_Click", {}));
    };

    getSoftSelectId = ({ moduleId } = "") => moduleId;

    render() {
        const gridProps = {
            /* Grid Properties */
            ...defaultModulesGridProps,
            onFirstDataRendered: this.onFirstDataRendered,
            columnDefs: translateColumnDefs(this.props.t, this.columnDefs),
            sizeColumnsToFit: true,
            getSoftSelectId: this.getSoftSelectId,
            softSelectId: this.state.softSelectedModuleId || {},
            ...this.props, // Allow default property overrides
            deltaRowDataMode: true,
            enableSorting: true,
            unSortIcon: true,
            getRowNodeId: ({ moduleId }) => moduleId,
            context: {
                t: this.props.t,
            },
            /* Grid Events */
            onRowClicked: ({ node }) => node.setSelected(!node.isSelected()),
            onGridReady: this.onGridReady,
            onSoftSelectChange: this.onSoftSelectChange,
            onHardSelectChange: this.onHardSelectChange,
            onColumnMoved: this.onColumnMoved,
            onSortChanged: this.onSortChanged,
        };

        return [
            <PcsGrid key="modules-grid-key" {...gridProps} />,
            this.getOpenFlyout(),
        ];
    }
}
