// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

import { joinClasses } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./propertyGrid.module.scss"));

export const PropertyGrid = (props) => (
    <div
        className={joinClasses(css("property-grid-container"), props.className)}
    >
        {props.children}
    </div>
);

PropertyGrid.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
