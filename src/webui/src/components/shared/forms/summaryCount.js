// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/summarySection.module.scss"));

export const SummaryCount = (props) => (
    <div className={css("summary-count", props.className)}>
        {props.children}
    </div>
);

SummaryCount.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
