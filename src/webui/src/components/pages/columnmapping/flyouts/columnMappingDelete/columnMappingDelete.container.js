// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { ColumnMappingDelete } from "./columnMappingDelete";
import { getDeviceGroups, redux as appRedux } from "store/reducers/appReducer";

// Wrap the dispatch method
const mapStateToProps = (state) => ({
        deviceGroups: getDeviceGroups(state),
    }),
    mapDispatchToProps = (dispatch) => ({
        deleteColumnMappings: (ids) =>
            dispatch(appRedux.actions.deleteColumnMappings(ids)),
    });

export const ColumnMappingDeleteContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(ColumnMappingDelete)
);
