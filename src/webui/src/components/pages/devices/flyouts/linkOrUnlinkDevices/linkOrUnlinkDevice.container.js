// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { epics as simulationEpics } from "store/reducers/deviceSimulationReducer";
import {
    epics as devicesEpics,
    getDevices,
    redux as devicesRedux,
} from "store/reducers/devicesReducer";
import { epics as appEpics } from "store/reducers/appReducer";
import { LinkOrUnlinkDevice } from "./linkOrUnlinkDevices";
import { withTranslation } from "react-i18next";

// Pass the global info needed
const mapStateToProps = (state) => ({
        devices: getDevices(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        fetchDeviceModelOptions: () =>
            dispatch(
                simulationEpics.actions.fetchSimulationDeviceModelOptions()
            ),
        insertDevices: (devices) =>
            dispatch(devicesRedux.actions.insertDevices(devices)),
        fetchDeviceStatistics: () =>
            dispatch(devicesEpics.actions.fetchDeviceStatistics()),
        fetchDevices: () => dispatch(devicesEpics.actions.fetchDevices()),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const LinkOrUnlinkDeviceContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(LinkOrUnlinkDevice)
);
