// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { AccordionProvider } from "./accordionProvider";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./flyoutSection.module.scss"));

export const FlyoutSection = ({ collapsable, className, children, closed }) => (
    <AccordionProvider isCollapsable={collapsable} isClosed={closed}>
        <div className={css("flyout-section", className)}>{children}</div>
    </AccordionProvider>
);
