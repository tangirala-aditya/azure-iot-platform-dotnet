// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { epics as appEpics } from "store/reducers/appReducer";
import { EdgeDeviceDetails } from "./edgeDeviceDetails";
import { withTranslation } from "react-i18next";

const mapDispatchToProps = (dispatch) => ({
    logEvent: (diagnosticsModel) =>
        dispatch(appEpics.actions.logEvent(diagnosticsModel)),
});

export const EdgeDeviceDetailsContainer = withTranslation()(
    connect(null, mapDispatchToProps)(EdgeDeviceDetails)
);
