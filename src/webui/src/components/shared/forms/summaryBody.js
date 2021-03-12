// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/summarySection.module.scss"));

export const SummaryBody = (props) => (
    <div className={css("summary-body", props.className)}>{props.children}</div>
);

SummaryBody.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
