// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import PropTypes from "prop-types";

import { Btn } from "components/shared";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/pill.module.scss"));

export const Pill = ({ svg, label, onSvgClick, altSvgText }) => (
    <div className={css("pill")}>
        {label}
        {svg && (
            <Btn
                onClick={onSvgClick}
                svg={svg}
                className={css("pill-icon")}
                alt={altSvgText}
            />
        )}
    </div>
);

Pill.propTypes = {
    svg: PropTypes.string,
    label: PropTypes.string,
    onSvgClick: PropTypes.func,
    altSvgText: PropTypes.string,
};
