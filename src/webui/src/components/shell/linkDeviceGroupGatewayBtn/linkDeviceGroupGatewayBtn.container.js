// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import {
    epics as appEpics,
    redux as appRedux,
} from "store/reducers/appReducer";

import { LinkDeviceGroupGatewayBtn } from "./linkDeviceGroupGatewayBtn";

const mapDispatchToProps = (dispatch) => ({
    openFlyout: () =>
        dispatch(appRedux.actions.setlinkDeviceGroupGatewayFlyoutStatus(true)),
    logEvent: (diagnosticsModel) =>
        dispatch(appEpics.actions.logEvent(diagnosticsModel)),
});

export const LinkDeviceGroupGatewayBtnContainer = withTranslation()(
    connect(null, mapDispatchToProps)(LinkDeviceGroupGatewayBtn)
);
