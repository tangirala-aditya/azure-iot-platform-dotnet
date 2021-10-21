// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

import { Svg } from "components/shared/svg/svg";
import { Icon } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Icon";

import "@microsoft/azure-iot-ux-fluent-controls/lib/components/Button";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/btn.module.scss"));

export const Btn = (props) => {
    const { svg, icon, children, className, primary, ...btnProps } = props;
    return (
        <button
            type="button"
            {...btnProps}
            className={css("btn", className, {
                "btn-primary": primary,
                "btn-secondary": !primary,
            })}
        >
            {props.svg && <Svg src={props.svg} className={css("btn-icon")} />}
            {props.icon && (
                <Icon icon={props.icon} className={css("btn-icon")} />
            )}
            {props.children && (
                <div className={css("btn-text")}>{props.children}</div>
            )}
        </button>
    );
};

Btn.propTypes = {
    children: PropTypes.node,
    className: PropTypes.string,
    primary: PropTypes.bool,
    svg: PropTypes.string,
    icon: PropTypes.string,
};
