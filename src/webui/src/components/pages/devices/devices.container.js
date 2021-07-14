// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { Devices } from "./devices";
import {
    epics as devicesEpics,
    redux as devicesRedux,
    getDevicesWithMappings,
    getDevicesError,
    getDevicesLastUpdated,
    getDevicesPendingStatus,
    getDevicesByCondition,
    getDevicesByConditionError,
    getDevicesByConditionPendingStatus,
    getLoadMoreToggleState,
} from "store/reducers/devicesReducer";
import {
    redux as appRedux,
    epics as appEpics,
    getDeviceGroups,
    getDeviceGroupError,
    getActiveDeviceQueryConditions,
    getActiveDeviceGroupConditions,
    getActiveDeviceGroupId,
    getColumnMappings,
    getColumnMappingPendingStatus,
    getColumnOptionsList,
    getColumnOptionsPendingStatus,
} from "store/reducers/appReducer";

// Pass the devices status
const mapStateToProps = (state) => ({
        devices: getDevicesWithMappings(state),
        deviceError: getDevicesError(state),
        isPending: getDevicesPendingStatus(state),
        devicesByCondition: getDevicesByCondition(state),
        devicesByConditionError: getDevicesByConditionError(state),
        isDevicesByConditionPanding: getDevicesByConditionPendingStatus(state),
        deviceGroups: getDeviceGroups(state),
        deviceGroupError: getDeviceGroupError(state),
        lastUpdated: getDevicesLastUpdated(state),
        activeDeviceQueryConditions: getActiveDeviceQueryConditions(state),
        activeDeviceGroupConditions: getActiveDeviceGroupConditions(state),
        loadMoreState: getLoadMoreToggleState(state),
        activeDeviceGroupId: getActiveDeviceGroupId(state),
        columnMappings: getColumnMappings(state),
        isColumnMappingsPending: getColumnMappingPendingStatus(state),
        columnOptions: getColumnOptionsList(state),
        isColumnOptionsPending: getColumnOptionsPendingStatus(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        fetchDevices: () => dispatch(devicesEpics.actions.fetchDevices()),
        fetchDevicesByCToken: () =>
            dispatch(devicesEpics.actions.fetchDevicesByCToken()),
        updateCurrentWindow: (currentWindow) =>
            dispatch(appRedux.actions.updateCurrentWindow(currentWindow)),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
        cancelDeviceCalls: (payload) =>
            dispatch(devicesRedux.actions.cancelDeviceCalls(payload)),
        checkTenantAndSwitch: (payload) =>
            dispatch(appRedux.actions.checkTenantAndSwitch(payload)),
        resetDeviceByCondition: () =>
            dispatch(devicesRedux.actions.resetDeviceByCondition()),
        insertColumnOptions: (columnOptions) =>
            dispatch(appRedux.actions.insertColumnOptions(columnOptions)),
    });

export const DevicesContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(Devices)
);
