// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./indicator.module.scss"));

const Dot = () => (
        <div className={css("dot")}>
            <span className={css("inner")} />
        </div>
    ),
    validSizes = new Set(["large", "medium", "normal", "small", "mini"]),
    validPatterns = new Set(["ring", "bar"]);

/** Creates a loading indicator */
export const Indicator = (props) => {
    const { size, pattern, className } = props,
        sizeClass = validSizes.has(size) ? css(size) : css("normal"),
        patternClass = validPatterns.has(pattern) ? css(pattern) : css("ring");
    return (
        <div
            className={css(
                "wait-indicator",
                sizeClass,
                patternClass,
                className
            )}
        >
            <Dot />
            <Dot />
            <Dot />
            <Dot />
            <Dot />
            <Dot />
        </div>
    );
};
