// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";

// import styles from "./panel.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./panel.module.scss"));

export const Panel = ({ className, children }) => (
    <div className={joinClasses(css("panel-container"), className)}>
        {children}
    </div>
);
