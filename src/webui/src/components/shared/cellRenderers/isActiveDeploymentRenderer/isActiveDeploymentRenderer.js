// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const IsActiveDeploymentRenderer = ({ value, context: { t } }) => (
    <div className={css("pcs-renderr-cell", "highlight")}>
        {value ? (
            <div className={css("small-green-circle")}></div>
        ) : (
            <div className={css("small-black-circle")}></div>
        )}
    </div>
);
