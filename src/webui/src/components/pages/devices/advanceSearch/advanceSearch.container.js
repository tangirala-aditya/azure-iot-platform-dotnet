// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { AdvanceSearch } from "./advanceSearch";
import {
    epics as devicesEpics,
    redux as devicesRedux,
} from "store/reducers/devicesReducer";
import { epics as appEpics } from "store/reducers/appReducer";

const mapDispatchToProps = (dispatch) => ({
    fetchDevicesByCondition: (data) =>
        dispatch(devicesEpics.actions.fetchDevicesByCondition(data)),
    logEvent: (diagnosticsModel) =>
        dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    resetDeviceByCondition: () =>
        dispatch(devicesRedux.actions.resetDeviceByCondition()),
});

export const AdvanceSearchContainer = withTranslation()(
    connect(null, mapDispatchToProps)(AdvanceSearch)
);
