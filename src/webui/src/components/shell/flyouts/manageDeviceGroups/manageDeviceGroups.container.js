// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { ManageDeviceGroups } from "./manageDeviceGroups";
import {
    redux as appRedux,
    epics as appEpics,
    getDeviceGroups,
    getActiveDeviceGroupId,
    getColumnMappingsList,
} from "store/reducers/appReducer";

const mapStateToProps = (state) => ({
        deviceGroups: getDeviceGroups(state),
        activeDeviceGroupId: getActiveDeviceGroupId(state),
        columnMappings: getColumnMappingsList(state),
    }),
    mapDispatchToProps = (dispatch) => ({
        changeDeviceGroup: (id) =>
            dispatch(appEpics.actions.updateActiveDeviceGroup(id)),
        closeFlyout: () =>
            dispatch(appRedux.actions.setDeviceGroupFlyoutStatus(false)),
        deleteDeviceGroups: (ids) =>
            dispatch(appRedux.actions.deleteDeviceGroups(ids)),
        insertDeviceGroups: (deviceGroups) =>
            dispatch(appRedux.actions.insertDeviceGroups(deviceGroups)),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
        updateActiveDeviceGroup: (id) =>
            dispatch(appRedux.actions.updateActiveDeviceGroup(id)),
    });

export const ManageDeviceGroupsContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(ManageDeviceGroups)
);
