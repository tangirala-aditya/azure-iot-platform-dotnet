// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./statSection.module.scss"));

/** A presentational component containing one or many StatGroup */
export const StatSection = ({ children, className }) => (
    <div className={joinClasses(css("stat-container"), className)}>
        {children}
    </div>
);
