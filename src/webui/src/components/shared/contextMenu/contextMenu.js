// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";

// import styles from "./contextMenu.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./contextMenu.module.scss"));

export const ContextMenu = ({ children, className }) => (
    <div className={joinClasses(css("context-menu-container"), className)}>
        {children}
    </div>
);
