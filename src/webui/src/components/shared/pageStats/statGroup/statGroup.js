// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./statGroup.module.scss"));

/** A presentational component containing one or many StatProperty */
export const StatGroup = ({ children, className }) => (
    <div className={joinClasses(css("stat-cell"), className)}>{children}</div>
);
