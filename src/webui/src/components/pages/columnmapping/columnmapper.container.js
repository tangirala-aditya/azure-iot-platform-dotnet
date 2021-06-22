// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { ColumnMapper } from "./columnmapper";
import {
    epics as appEpics,
    getColumnMappingById,
    getDefaultColumnMapping,
} from "store/reducers/appReducer";

const mapStateToProps = (state, props) => ({
        columnMapping: getColumnMappingById(state, props.mappingName),
        defaultColumnMapping: getDefaultColumnMapping(state),
    }),
    mapDispatchToProps = (dispatch) => ({
        fetchColumnMappings: () =>
            dispatch(appEpics.actions.fetchColumnMappings()),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const ColumnMapperContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(ColumnMapper)
);
