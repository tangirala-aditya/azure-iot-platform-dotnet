// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import {
    epics as devicesEpics,
    getDevices,
} from "store/reducers/devicesReducer";
import {
    redux as appRedux,
    epics as appEpics,
    getActiveDeviceGroupId,
} from "store/reducers/appReducer";
import { LinkDeviceGroupGateway } from ".";

const mapStateToProps = (state) => ({
        devices: getDevices(state),
        activeDeviceGroupId: getActiveDeviceGroupId(state),
    }),
    mapDispatchToProps = (dispatch) => ({
        closeFlyout: () =>
            dispatch(
                appRedux.actions.setlinkDeviceGroupGatewayFlyoutStatus(false)
            ),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
        fetchDevices: () => dispatch(devicesEpics.actions.fetchDevices()),
    });

export const LinkDeviceGroupGatewayContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(LinkDeviceGroupGateway)
);
