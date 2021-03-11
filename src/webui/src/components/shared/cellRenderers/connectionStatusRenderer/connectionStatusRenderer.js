// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { Svg } from "components/shared/svg/svg";
import { svgs } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const ConnectionStatusRenderer = ({ value, context: { t } }) => {
    const cellClasses = css("pcs-renderer-cell", { highlight: value });

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
