// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { Svg } from "components/shared/svg/svg";
import { svgs } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const IsActivePackageRenderer = ({ value, context: { t } }) => (
    <div className={css("pcs-renderer-cell", "highlight")}>
        {value ? (
            <Svg src={svgs.checkmark} className={css("pcs-renderer-icon")} />
        ) : (
            <Svg src={svgs.cancelX} className={css("pcs-renderer-icon")} />
        )}
    </div>
);
