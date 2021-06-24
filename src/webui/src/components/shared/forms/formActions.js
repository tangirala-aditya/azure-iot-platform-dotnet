// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/formActions.module.scss"));

export const FormActions = (props) => (
    <div className={css("form-actions-container", props.className)}>
        {props.children}
    </div>
);

FormActions.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
