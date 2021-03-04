// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { Svg } from "components/shared/svg/svg";
import { svgs, joinClasses } from "utilities";

// import styles from "../cellRenderer.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const ConnectionStatusRenderer = ({ value, context: { t } }) => {
    const cellClasses = joinClasses(
        css("pcs-renderer-cell"),
        value ? css("highlight") : ""
    );

    return (
        <div className={cellClasses}>
            {value ? null : (
                <Svg src={svgs.disabled} className={css("pcs-renderer-icon")} />
            )}
            <div className={css("pcs-renderer-text")}>
                {value
                    ? t("devices.grid.connected")
                    : t("devices.grid.offline")}
            </div>
        </div>
    );
};
