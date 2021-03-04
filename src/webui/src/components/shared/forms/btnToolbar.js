// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

import { joinClasses } from "utilities";

// import styles from "./styles/btnToolbar.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/btnToolbar.module.scss"));

export const BtnToolbar = (props) => (
    <div className={joinClasses(css("btn-toolbar"), props.className)}>
        {props.children}
    </div>
);

BtnToolbar.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
