// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";
import { Svg } from "components/shared";
// import styles from "./statProperty.module.scss";

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
        <div className={joinClasses(css("stat-property"), className)}>
            <div className={joinClasses(css("stat-value"), css(sizeClass))}>
                {value}
            </div>
            {svg && (
                <Svg
                    src={svg}
                    className={joinClasses(css("stat-icon"), svgClassName)}
                />
            )}
            <div className={css("stat-label")}>{label}</div>
        </div>
    );
};
