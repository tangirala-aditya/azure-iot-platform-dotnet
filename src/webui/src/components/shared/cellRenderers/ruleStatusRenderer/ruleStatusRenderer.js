// Copyright (c) Microsoft. All rights reserved.

import React from "react";
// import styles from "../cellRenderer.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const RuleStatusRenderer = ({ value, context: { t } }) => (
    <div
        className={css("pcs-renderer-cell", { highlight: value === "Enabled" })}
    >
        <div className={css("pcs-renderer-text")}>{value}</div>
    </div>
);
