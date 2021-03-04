// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { Svg } from "components/shared/svg/svg";
import { svgs, joinClasses } from "utilities";

// import styles from "../cellRenderer.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const IsSimulatedRenderer = ({ value, context: { t } }) => (
    <div className={joinClasses(css("pcs-renderer-cell"), css("highlight"))}>
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
