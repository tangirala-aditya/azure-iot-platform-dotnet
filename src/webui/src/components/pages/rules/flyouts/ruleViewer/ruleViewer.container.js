// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { RuleViewer } from "./ruleViewer";
import { getRuleById } from "store/reducers/rulesReducer";
import { getDeviceGroups } from "store/reducers/appReducer";

// Pass the devices status
const mapStateToProps = (state, props) => ({
    rule: getRuleById(state, props.ruleId),
    deviceGroups: getDeviceGroups(state),
});

export const RuleViewerContainer = withTranslation()(
    connect(mapStateToProps, null)(RuleViewer)
);
