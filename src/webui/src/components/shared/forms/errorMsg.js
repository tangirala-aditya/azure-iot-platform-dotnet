// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

import { Svg } from "components/shared/svg/svg";
import { joinClasses, svgs } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/errorMsg.module.scss"));

export const ErrorMsg = (props) => {
    const { children, className } = props;
    return (
        <div className={joinClasses(css("error-message"), className)}>
            <Svg src={svgs.error} className={css("error-icon")} />
            {children}
        </div>
    );
};

ErrorMsg.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
};
