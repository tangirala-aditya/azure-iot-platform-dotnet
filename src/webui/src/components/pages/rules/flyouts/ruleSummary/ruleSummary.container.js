// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { RuleSummary } from "./ruleSummary";
import { getDeviceGroups } from "store/reducers/appReducer";

const mapStateToProps = (state, props) => ({
    deviceGroups: getDeviceGroups(state),
});

export const RuleSummaryContainer = withTranslation()(
    connect(mapStateToProps, null)(RuleSummary)
);
