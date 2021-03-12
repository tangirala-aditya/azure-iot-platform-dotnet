// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { withAccordion } from "./accordionProvider";
import { Svg } from "components/shared/svg/svg";
import { svgs } from "utilities";

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
            className: css("flyout-section-header", className),
        };
        return accordionIsCollapsable ? (
            <button {...sectionProps} onClick={toggleAccordion}>
                {children}
                {accordionIsCollapsable && (
                    <Svg
                        src={svgs.chevron}
                        className={css("collapse-section-icon", {
                            expanded: accordionIsOpen,
                            collapsed: !accordionIsOpen,
                        })}
                    />
                )}
            </button>
        ) : (
            <div {...sectionProps}>{children}</div>
        );
    }
);
