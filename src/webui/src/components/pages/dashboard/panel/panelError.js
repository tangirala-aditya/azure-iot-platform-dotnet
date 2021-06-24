// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { PanelOverlay } from "./panelOverlay";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./panel.module.scss"));

export const PanelError = ({ children, className }) => (
    <PanelOverlay className={css("error-overlay")}>
        <div className={css("panel-error-container", className)}>
            {children}
        </div>
    </PanelOverlay>
);
