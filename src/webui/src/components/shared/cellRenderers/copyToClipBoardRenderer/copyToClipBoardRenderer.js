// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Svg } from "components/shared/svg/svg";
import { svgs } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const CopyToClipBoardRenderer = ({ value, context: { t } }) => (
    <div className={css("pcs-renderer-cell", "highlight")}>
        <Svg path={svgs.copy} className={css("pcs-renderer-icon")} />
    </div>
);
