// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

import { joinClasses } from "utilities";

// import styles from "./styles/formSection.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/formSection.module.scss"));

export const FormSection = (props) => (
    <div className={joinClasses(css("form-section"), props.className)}>
        {props.children}
    </div>
);

FormSection.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
