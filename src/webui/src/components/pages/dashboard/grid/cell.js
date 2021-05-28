// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./grid.module.scss"));

export const Cell = ({ className, children }) => (
    <div className={css("grid-cell", className)}>
        <div className={css("grid-cell-contents")}>{children}</div>
    </div>
);
