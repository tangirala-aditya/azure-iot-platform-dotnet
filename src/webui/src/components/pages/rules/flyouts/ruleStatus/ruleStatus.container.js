// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { RuleStatus } from "./ruleStatus";
import { redux as rulesRedux } from "store/reducers/rulesReducer";

// Wrap the dispatch method
const mapDispatchToProps = (dispatch) => ({
    modifyRules: (rules) => dispatch(rulesRedux.actions.modifyRules(rules)),
});

export const RuleStatusContainer = withTranslation()(
    connect(null, mapDispatchToProps)(RuleStatus)
);
