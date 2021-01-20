// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { Administration } from "./administration";
import { withNamespaces } from "react-i18next";
import { epics as appEpics } from "store/reducers/appReducer";

// Wrap the dispatch method
const mapDispatchToProps = (dispatch) => ({
    logEvent: (diagnosticsModel) =>
        dispatch(appEpics.actions.logEvent(diagnosticsModel)),
});

export const AdministrationContainer = withNamespaces()(
    connect(null, mapDispatchToProps)(Administration)
);
