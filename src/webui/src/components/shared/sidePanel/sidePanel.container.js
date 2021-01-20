// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { epics as appEpics } from "store/reducers/appReducer";
import { SidePanel } from "./sidePanel";

const mapDispatchToProps = (dispatch) => ({
    logEvent: (diagnosticsModel) =>
        dispatch(appEpics.actions.logEvent(diagnosticsModel)),
});

export const SidePanelContainer = connect(
    null,
    mapDispatchToProps
)(SidePanel);
