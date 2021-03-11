// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import Config from "app.config";
import { Svg } from "components/shared/svg/svg";
import { svgs, joinClasses } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));
const severityCss = classnames.bind(require("./severityRenderer.module.scss"));

const getSvg = (value) => {
    if (value === Config.ruleSeverity.warning) {
        return svgs.warning;
    }
    if (value === Config.ruleSeverity.critical) {
        return svgs.critical;
    }
    return svgs.info;
};

export const SeverityRenderer = ({ value, context: { t }, iconOnly }) => {
    const cleanValue = (value || "").toLowerCase(),
        cellClasses = joinClasses(
            css("pcs-renderer-cell"),
            severityCss("severity"),
            cleanValue,
            cleanValue ? css("highlight") : ""
        );
    return (
        <div className={cellClasses}>
            <Svg
                src={getSvg(cleanValue)}
                className={css("pcs-renderer-icon")}
            />
            {!iconOnly && (
                <div className={css("pcs-renderer-text")}>
                    {t(`rules.severity.${cleanValue}`)}
                </div>
            )}
        </div>
    );
};

export default SeverityRenderer;
