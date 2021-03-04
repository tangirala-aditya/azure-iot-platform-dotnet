// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

import { joinClasses } from "utilities";

// import styles from "./styles/formActions.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/formActions.module.scss"));

export const FormActions = (props) => (
    <div
        className={joinClasses(css("form-actions-container"), props.className)}
    >
        {props.children}
    </div>
);

FormActions.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
