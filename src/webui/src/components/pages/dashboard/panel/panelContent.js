// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./panel.module.scss"));

export const PanelContent = ({ children, className }) => (
    <div className={css("panel-content", className)}>{children}</div>
);
