// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/formSection.module.scss"));

export const FormSection = (props) => (
    <div className={css("form-section", props.className)}>{props.children}</div>
);

FormSection.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
