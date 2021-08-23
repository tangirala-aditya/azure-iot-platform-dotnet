// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { permissions, toDiagnosticsModel } from "services/models";
import { Btn, ComponentArray, PcsGrid, Protected } from "components/shared";
import {
    defaultDeviceColumns,
    deviceGridColumns,
    defaultDeviceGridProps,
    defaultColDef,
} from "./devicesGridConfig";
import { DeviceDeleteContainer } from "../flyouts/deviceDelete";
import { DeviceJobsContainer } from "../flyouts/deviceJobs";
import { DeviceDetailsContainer } from "../flyouts/deviceDetails";
import { CloudToDeviceMessageContainer } from "../flyouts/cloudToDeviceMessage";
import {
    isFunc,
    svgs,
    translateColumnDefs,
    getFlyoutNameParam,
    getParamByName,
    getFlyoutLink,
    userHasPermission,
} from "utilities";

const closedFlyoutState = {
    openFlyoutName: undefined,
    softSelectedDeviceId: undefined,
};

/**
 * A grid for displaying devices
 *
 * Encapsulates the PcsGrid props
 */
export class DevicesGrid extends Component {
    constructor(props) {
        super(props);

        // Set the initial state
        this.state = {
            ...closedFlyoutState,
            isDeviceSearch: false,
        };
    }

    contextBtns = () => (
        <ComponentArray>
            <Protected permission={permissions.createJobs}>
                <Btn svg={svgs.reconfigure} onClick={this.openFlyout("jobs")}>
                    {this.props.t("devices.flyouts.jobs.title")}
                </Btn>
            </Protected>
            <Protected permission={permissions.deleteDevices}>
                <Btn svg={svgs.trash} onClick={this.openFlyout("delete")}>
                    {this.props.t("devices.flyouts.delete.title")}
                </Btn>
            </Protected>
            <Protected permission={permissions.sendC2DMessage}>
                <Btn svg={svgs.email} onClick={this.openFlyout("c2dmessage")}>
                    {this.props.t("devices.flyouts.c2dMessage.sendMessage")}
                </Btn>
            </Protected>
            <Btn icon="areaChart" onClick={this.goToTelemetryScreen}>
                {this.props.t("devices.showTelemetry")}
            </Btn>
        </ComponentArray>
    );

    UNSAFE_componentWillMount() {
        if (
            this.props &&
            this.props.location &&
            this.props.location.pathname === "/deviceSearch"
        ) {
            this.setState({
                isDeviceSearch: true,
            });
        } else {
            this.setState({
                isDeviceSearch: false,
            });
        }
    }

    onFirstDataRendered = () => {
        if (this.props.rowData && this.props.rowData.length > 0) {
            this.getDefaultFlyout(this.props.rowData);
        }
    };

    UNSAFE_componentWillReceiveProps(nextProps) {
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
            !this.state.softSelectedDeviceId &&
            devices &&
            devices.length > 0 &&
            deviceIds.length === devices.length &&
            isUserHasPermission
        ) {
            this.setState({
                softSelectedDeviceId: deviceIds,
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
            softSelectedDeviceId: undefined,
        });

    getOpenFlyout = () => {
        var flyoutLink = undefined;
        switch (this.state.openFlyoutName) {
            case "delete":
                return (
                    <DeviceDeleteContainer
                        key="delete-device-key"
                        onClose={this.closeFlyout}
                        devices={this.deviceGridApi.getSelectedRows()}
                    />
                );
            case "jobs":
                const deviceIds = this.deviceGridApi
                    .getSelectedRows()
                    .map((d) => d.id)
                    .join("||");
                flyoutLink = getFlyoutLink(
                    this.props.currentTenantId,
                    this.props.activeDeviceGroupId,
                    "deviceId",
                    deviceIds ? deviceIds : this.state.softSelectedDeviceId,
                    "jobs"
                );
                return (
                    <DeviceJobsContainer
                        key="jobs-device-key"
                        onClose={this.closeFlyout}
                        devices={
                            this.deviceGridApi.getSelectedRows().length > 0
                                ? this.deviceGridApi.getSelectedRows()
                                : this.state.selectedDevicesForFlyout
                        }
                        openPropertyEditorModal={
                            this.props.openPropertyEditorModal
                        }
                        flyoutLink={flyoutLink}
                    />
                );
            case "details":
                flyoutLink = getFlyoutLink(
                    this.props.currentTenantId,
                    this.props.activeDeviceGroupId,
                    "deviceId",
                    this.state.softSelectedDeviceId,
                    "details"
                );
                return (
                    <DeviceDetailsContainer
                        key="details-device-key"
                        onClose={this.closeFlyout}
                        deviceId={this.state.softSelectedDeviceId}
                        flyoutLink={flyoutLink}
                        isDeviceSearch={this.state.isDeviceSearch}
                    />
                );
            case "c2dmessage":
                return (
                    <CloudToDeviceMessageContainer
                        onClose={this.closeFlyout}
                        devices={this.deviceGridApi.getSelectedRows()}
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

    goToTelemetryScreen = () => {
        const selectedDevices = this.deviceGridApi.getSelectedRows();
        if (this.state.isDeviceSearch) {
            this.props.history.push("/deviceSearch/telemetry", {
                deviceIds: selectedDevices.map(({ id }) => id),
            });
        } else {
            this.props.history.push("/devices/telemetry", {
                deviceIds: selectedDevices.map(({ id }) => id),
            });
        }
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
                softSelectedDeviceId: deviceId,
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
                selectedDevices.length > 0 ? this.contextBtns() : null
            );
        }
        if (isFunc(onHardSelectChange)) {
            onHardSelectChange(selectedDevices);
        }
    };

    onColumnMoved = () => {
        this.props.logEvent(toDiagnosticsModel("Devices_ColumnArranged", {}));
    };

    onSortChanged = () => {
        this.props.logEvent(toDiagnosticsModel("Devices_Sort_Click", {}));
    };

    getSoftSelectId = ({ id } = "") => id;

    render() {
        let columnDefs = null;

        if (this.props.useStaticCols) {
            columnDefs = deviceGridColumns;
        } else {
            columnDefs =
                this.props.columnDefs && this.props.columnDefs.length > 0
                    ? defaultDeviceColumns.concat(this.props.columnDefs)
                    : defaultDeviceColumns;
        }

        const gridProps = {
            /* Grid Properties */
            ...defaultDeviceGridProps,
            onFirstDataRendered: this.onFirstDataRendered,
            defaultColDef: defaultColDef,
            sizeColumnsToFit: true,
            getSoftSelectId: this.getSoftSelectId,
            softSelectId: this.state.softSelectedDeviceId || {},
            ...this.props, // Allow default property overrides
            columnDefs: translateColumnDefs(this.props.t, columnDefs),
            immutableData: true,
            getRowNodeId: ({ id }) => id,
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
            <PcsGrid key="device-grid-key" {...gridProps} />,
            this.getOpenFlyout(),
        ];
    }
}
