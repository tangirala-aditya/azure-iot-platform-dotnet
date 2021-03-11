// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { ReactSVG } from "react-svg";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./svg.module.scss"));

/** Wraps an svg in a bounding box container for easier styling */
export const Svg = ({ className, onClick, ...props } = {}) => {
    // ReactSVG will cause an error in the tests because Jest JSDOM doesn't
    // support SVGs. This check will prevent the component from trying to
    // render an SVG if it isn't supported
    const svgsSupported = window && window.SVGSVGElement;
    return (
        <div className={css("svg-container", className)} onClick={onClick}>
            {svgsSupported && (
                <ReactSVG {...props} className={css(props.svgClassName)} />
            )}
        </div>
    );
};
