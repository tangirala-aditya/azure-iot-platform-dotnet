// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./grid.module.scss"));

export const Grid = ({ children }) => (
    <div className={css("grid-container")}>{children}</div>
);
