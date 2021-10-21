// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { CloudToDeviceMessage } from "./cloudToDeviceMessage";
import { getTheme } from "store/reducers/appReducer";

const mapStateToProps = (state) => ({
        theme: getTheme(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({});

export const CloudToDeviceMessageContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(CloudToDeviceMessage)
);
