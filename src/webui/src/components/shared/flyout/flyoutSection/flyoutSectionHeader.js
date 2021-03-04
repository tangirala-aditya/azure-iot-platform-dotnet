// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { withAccordion } from "./accordionProvider";
import { Svg } from "components/shared/svg/svg";
import { svgs, joinClasses } from "utilities";
// import styles from "./flyoutSection.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./flyoutSection.module.scss"));

export const FlyoutSectionHeader = withAccordion(
    ({
        accordionIsCollapsable,
        className,
        children,
        toggleAccordion,
        accordionIsOpen,
    }) => {
        const sectionProps = {
            className: joinClasses(css("flyout-section-header"), className),
        };
        return accordionIsCollapsable ? (
            <button {...sectionProps} onClick={toggleAccordion}>
                {children}
                {accordionIsCollapsable && (
                    <Svg
                        src={svgs.chevron}
                        className={joinClasses(
                            css("collapse-section-icon"),
                            accordionIsOpen ? css("expanded") : css("collapsed")
                        )}
                    />
                )}
            </button>
        ) : (
            <div {...sectionProps}>{children}</div>
        );
    }
);
