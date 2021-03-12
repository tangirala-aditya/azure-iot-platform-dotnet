// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { AccordionCollapsableContent } from "./accordionProvider";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./flyoutSection.module.scss"));

export const FlyoutSectionContent = ({ className, children }) => (
    <AccordionCollapsableContent>
        <div className={css("flyout-section-content", className)}>
            {children}
        </div>
    </AccordionCollapsableContent>
);
