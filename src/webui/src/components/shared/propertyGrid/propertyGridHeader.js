// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./propertyGrid.module.scss"));

export const PropertyGridHeader = (props) => (
    <div className={css("grid-header", props.className)}>{props.children}</div>
);

PropertyGridHeader.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
