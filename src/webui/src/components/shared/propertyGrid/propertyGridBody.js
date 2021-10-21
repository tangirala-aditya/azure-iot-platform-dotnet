// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./propertyGrid.module.scss"));

export const PropertyGridBody = (props) => (
    <div className={css("grid-scrollable", props.className)}>
        {props.children}
    </div>
);

PropertyGridBody.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
