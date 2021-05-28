// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./panel.module.scss"));

export const PanelHeaderLabel = ({ children, className }) => (
    <h2 className={css("panel-header-label", className)}>{children}</h2>
);
