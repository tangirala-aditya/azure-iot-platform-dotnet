// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import {
    epics as appEpics,
    redux as appRedux,
} from "store/reducers/appReducer";

import { CreateDeviceQueryBtn } from "./createDeviceQueryBtn";

const mapDispatchToProps = (dispatch) => ({
    openFlyout: () =>
        dispatch(appRedux.actions.setCreateDeviceQueryFlyoutStatus(true)),
    logEvent: (diagnosticsModel) =>
        dispatch(appEpics.actions.logEvent(diagnosticsModel)),
});

export const CreateDeviceQueryBtnContainer = withTranslation()(
    connect(null, mapDispatchToProps)(CreateDeviceQueryBtn)
);
