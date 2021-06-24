// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./contextMenu.module.scss"));

export const ContextMenuAlign = ({ children, className, left }) => (
    <div
        className={css(
            "context-menu-align-container",
            { left: left, right: !left },
            className
        )}
    >
        {children}
    </div>
);
