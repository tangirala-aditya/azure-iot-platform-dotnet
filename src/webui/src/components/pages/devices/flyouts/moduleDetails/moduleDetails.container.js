// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { ModuleDetails } from "./moduleDetails";
import { epics as appEpics } from "store/reducers/appReducer";
import { getTheme } from "store/reducers/appReducer";

const mapStateToProps = (state) => ({
    theme: getTheme(state),
});
// Pass the device details
const mapDispatchToProps = (dispatch) => ({
    logEvent: (diagnosticsModel) =>
        dispatch(appEpics.actions.logEvent(diagnosticsModel)),
});

export const ModuleDetailsContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(ModuleDetails)
);
