// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { ColumnMapping } from "./columnmapping";
import {
    epics as appEpics,
    getColumnMappings,
} from "store/reducers/appReducer";

const mapStateToProps = (state) => ({
        columnMappings: getColumnMappings(state),
    }),
    mapDispatchToProps = (dispatch) => ({
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const ColumnMappingContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(ColumnMapping)
);
