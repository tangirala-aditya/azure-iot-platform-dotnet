// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { ColumnMappingsGrid } from "./columnMappingGrid";
import {
    epics as appEpics,
    getColumnMappingsList,
} from "store/reducers/appReducer";

// Wrap the dispatch method
const mapStateToProps = (state) => ({
        columnMappings: getColumnMappingsList(state),
    }),
    mapDispatchToProps = (dispatch) => ({
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const ColumnMappingsGridContainer = connect(
    mapStateToProps,
    mapDispatchToProps
)(ColumnMappingsGrid);
