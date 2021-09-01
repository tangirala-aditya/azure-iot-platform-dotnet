// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import {
    redux as appRedux,
    getActiveDeviceGroup,
    getTheme,
    getTimeInterval,
    getActiveDeviceQueryConditions,
    getGrafanaUrl,
    getGrafanaOrgId,
    getUser,
} from "store/reducers/appReducer";
import {
    getDevicesError,
    getDevicesPendingStatus,
    getEntities as getDeviceEntities,
} from "store/reducers/devicesReducer";

import { GrafanaDashboard } from "./grafanaDashboard";

const mapStateToProps = (state) => ({
        activeDeviceGroup: getActiveDeviceGroup(state),
        devices: getDeviceEntities(state),
        devicesError: getDevicesError(state),
        devicesIsPending: getDevicesPendingStatus(state),
        theme: getTheme(state),
        timeInterval: getTimeInterval(state),
        activeDeviceQueryConditions: getActiveDeviceQueryConditions(state),
        grafanaUrl: getGrafanaUrl(state),
        grafanaOrgId: getGrafanaOrgId(state),
        user: getUser(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        updateTimeInterval: (timeInterval) =>
            dispatch(appRedux.actions.updateTimeInterval(timeInterval)),
        updateCurrentWindow: (currentWindow) =>
            dispatch(appRedux.actions.updateCurrentWindow(currentWindow)),
        checkTenantAndSwitch: (payload) =>
            dispatch(appRedux.actions.checkTenantAndSwitch(payload)),
    });

export const GrafanaDashboardContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(GrafanaDashboard)
);
