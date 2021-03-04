// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

import { joinClasses } from "utilities";
// import styles from "./styles/summarySection.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/summarySection.module.scss"));

export const SummaryCount = (props) => (
    <div className={joinClasses(css("summary-count"), props.className)}>
        {props.children}
    </div>
);

SummaryCount.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
