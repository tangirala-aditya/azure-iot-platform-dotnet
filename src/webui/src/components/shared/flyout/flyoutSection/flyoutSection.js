// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { AccordionProvider } from "./accordionProvider";
import { joinClasses } from "utilities";

// import styles from "./flyoutSection.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./flyoutSection.module.scss"));

export const FlyoutSection = ({ collapsable, className, children, closed }) => (
    <AccordionProvider isCollapsable={collapsable} isClosed={closed}>
        <div className={joinClasses(css("flyout-section"), className)}>
            {children}
        </div>
    </AccordionProvider>
);
