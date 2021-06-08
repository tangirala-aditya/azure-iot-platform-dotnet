// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { DataExplorer } from "./dataexplorer";
// import {
//   epics as usersEpics,
//   getDevices,
//   getDevicesError,
//   getDevicesLastUpdated,
//   getDevicesPendingStatus
// } from 'store/reducers/devicesReducer';

// Pass the devices status
// const mapStateToProps = state => ({
//   users: getDevices(state),
//   userError: getDevicesError(state),
//   isPending: getDevicesPendingStatus(state),
//   lastUpdated: getDevicesLastUpdated(state)
// });
const mapStateToProps = (state) => ({}),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({});
export const DataExplorerContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(DataExplorer)
);
