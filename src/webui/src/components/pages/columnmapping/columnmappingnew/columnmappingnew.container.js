// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { ColumnMappingNew } from "./columnmappingnew";
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

export const ColumnMappingNewContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(ColumnMappingNew)
);
