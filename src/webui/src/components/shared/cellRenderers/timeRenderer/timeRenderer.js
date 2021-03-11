// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { formatTime } from "utilities";
import { EMPTY_FIELD_VAL } from "components/shared/pcsGrid/pcsGridConfig";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const TimeRenderer = ({ value }) => {
    const formattedTime = formatTime(value);
    return (
        <div className={css("pcs-renderer-cell")}>
            <div className={css("pcs-renderer-time-text")}>
                {formattedTime ? formattedTime : EMPTY_FIELD_VAL}
            </div>
        </div>
    );
};
