// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { Svg } from "components/shared/svg/svg";
import { svgs } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const IsSimulatedRenderer = ({ value, context: { t } }) => (
    <div className={css("pcs-renderer-cell", "highlight")}>
        {value ? (
            <Svg
                src={svgs.simulatedDevice}
                className={css("pcs-renderer-icon")}
            />
        ) : null}
        <div className={css("pcs-renderer-text")}>
            {value ? t("devices.grid.yes") : t("devices.grid.no")}
        </div>
    </div>
);
