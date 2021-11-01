// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { epics as appEpics } from "store/reducers/appReducer";
import { DeviceJobDetail } from "./deviceJobDetail";

// Wrap the dispatch method
const mapDispatchToProps = (dispatch) => ({
    logEvent: (diagnosticsModel) =>
        dispatch(appEpics.actions.logEvent(diagnosticsModel)),
});

export const DeviceJobDetailsContainer = connect(
    null,
    mapDispatchToProps
)(DeviceJobDetail);
