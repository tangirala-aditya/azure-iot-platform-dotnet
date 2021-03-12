// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/btnToolbar.module.scss"));

export const BtnToolbar = (props) => (
    <div className={css("btn-toolbar", props.className)}>{props.children}</div>
);

BtnToolbar.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
