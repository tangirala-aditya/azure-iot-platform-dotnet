// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import {
    epics as appEpics,
    getActiveDeviceGroupId,
    getUser,
    getUserCurrentTenant,
} from "store/reducers/appReducer";
import { epics as devicesEpics } from "store/reducers/devicesReducer";
import { EdgeDevicesGrid } from "./edgeDeviceGrid";

const mapStateToProps = (state) => ({
        activeDeviceGroupId: getActiveDeviceGroupId(state),
        userPermissions: getUser(state).permissions,
        currentTenantId: getUserCurrentTenant(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
        fetchDevicesByCondition: (data) =>
            dispatch(devicesEpics.actions.fetchDevicesByCondition(data)),
    });

export const EdgeDevicesGridContainer = connect(
    mapStateToProps,
    mapDispatchToProps
)(EdgeDevicesGrid);
