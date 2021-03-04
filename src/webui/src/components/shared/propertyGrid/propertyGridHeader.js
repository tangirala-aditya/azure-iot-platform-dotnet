// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

import { joinClasses } from "utilities";

// import styles from "./propertyGrid.module.scss";
const classnames = require("classnames/bind");
const css = classnames.bind(require("./propertyGrid.module.scss"));

export const PropertyGridHeader = (props) => (
    <div className={joinClasses(css("grid-header"), props.className)}>
        {props.children}
    </div>
);

PropertyGridHeader.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
