// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { Rules } from "./rules";
import {
    epics as rulesEpics,
    getRules,
    getEntities,
    getRulesError,
    getRulesLastUpdated,
    getRulesPendingStatus,
} from "store/reducers/rulesReducer";
import {
    epics as appEpics,
    redux as appRedux,
    getDeviceGroups,
    getApplicationPermissionsAssigned,
    getAlerting,
    getActiveDeviceQueryConditions,
    getActiveDeviceGroupId,
    getUser,
    getUserCurrentTenant,
} from "store/reducers/appReducer";

// Pass the devices status
const mapStateToProps = (state) => ({
        alerting: getAlerting(state),
        rules: getRules(state),
        entities: getEntities(state),
        error: getRulesError(state),
        isPending: getRulesPendingStatus(state),
        deviceGroups: getDeviceGroups(state),
        lastUpdated: getRulesLastUpdated(state),
        applicationPermissionsAssigned:
            getApplicationPermissionsAssigned(state),
        activeDeviceQueryConditions: getActiveDeviceQueryConditions(state),
        activeDeviceGroupId: getActiveDeviceGroupId(state),
        userPermissions: getUser(state).permissions,
        currentTenantId: getUserCurrentTenant(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        fetchRules: () => dispatch(rulesEpics.actions.fetchRules()),
        updateCurrentWindow: (currentWindow) =>
            dispatch(appRedux.actions.updateCurrentWindow(currentWindow)),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
        checkTenantAndSwitch: (payload) =>
            dispatch(appRedux.actions.checkTenantAndSwitch(payload)),
    });

export const RulesContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(Rules)
);
