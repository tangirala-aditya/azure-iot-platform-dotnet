// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

import { joinClasses } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./propertyGrid.module.scss"));

export const PropertyGridBody = (props) => (
    <div className={joinClasses(css("grid-scrollable"), props.className)}>
        {props.children}
    </div>
);

PropertyGridBody.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
