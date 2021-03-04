// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { AccordionCollapsableContent } from "./accordionProvider";
import { joinClasses } from "utilities";
// import styles from "./flyoutSection.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./flyoutSection.module.scss"));

export const FlyoutSectionContent = ({ className, children }) => (
    <AccordionCollapsableContent>
        <div className={joinClasses(css("flyout-section-content"), className)}>
            {children}
        </div>
    </AccordionCollapsableContent>
);
