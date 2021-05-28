// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./propertyGrid.module.scss"));

export const PropertyRow = (props) => (
    <div className={css("row", props.className)}> {props.children} </div>
);

PropertyRow.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
