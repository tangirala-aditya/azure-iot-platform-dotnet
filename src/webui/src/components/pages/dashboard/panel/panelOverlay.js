// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./panel.module.scss"));

export const PanelOverlay = ({ children, className }) => (
    <div className={css("panel-overlay-container", className)}>{children}</div>
);
