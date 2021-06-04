// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./panel.module.scss"));

export const Panel = ({ className, children }) => (
    <div className={joinClasses(css("panel-container"), className)}>
        {children}
    </div>
);
