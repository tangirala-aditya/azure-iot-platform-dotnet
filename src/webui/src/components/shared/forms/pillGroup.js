// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

import { Pill } from "./pill";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/pillGroup.module.scss"));

export const PillGroup = ({ pills, onSvgClick, svg, altSvgText }) => (
    <div className={css("pill-group")}>
        {pills.map((pill, idx) => (
            <Pill
                key={idx}
                label={pill}
                svg={svg}
                onSvgClick={onSvgClick(idx)}
                altSvgText={altSvgText}
            />
        ))}
    </div>
);

PillGroup.propTypes = {
    svg: PropTypes.string,
    onSvgClick: PropTypes.func,
    pills: PropTypes.arrayOf(PropTypes.string),
    altSvgText: PropTypes.string,
};
