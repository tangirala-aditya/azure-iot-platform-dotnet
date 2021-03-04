// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";

// import css from "./contextMenu.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./contextMenu.module.scss"));

export const ContextMenuAlign = ({ children, className, left }) => (
    <div
        className={joinClasses(
            css("context-menu-align-container"),
            left ? css("left") : css("right"),
            className
        )}
    >
        {children}
    </div>
);
