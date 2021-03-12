// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Svg } from "components/shared";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./statProperty.module.scss"));

const validSizes = new Set(["large", "medium", "small", "normal"]);

/** A presentational component containing statistics value, label and icon */
export const StatProperty = ({
    value,
    label,
    size,
    svg,
    className,
    svgClassName,
}) => {
    const sizeClass = validSizes.has(size) ? size : "normal";
    return (
        <div className={css("stat-property", className)}>
            <div className={css("stat-value", sizeClass)}>{value}</div>
            {svg && (
                <Svg src={svg} className={css("stat-icon", svgClassName)} />
            )}
            <div className={css("stat-label")}>{label}</div>
        </div>
    );
};
