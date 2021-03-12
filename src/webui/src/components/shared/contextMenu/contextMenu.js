// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./contextMenu.module.scss"));

export const ContextMenu = ({ children, className }) => (
    <div className={css("context-menu-container", className)}>{children}</div>
);
