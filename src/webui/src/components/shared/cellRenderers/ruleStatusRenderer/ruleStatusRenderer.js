// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import joinClasses from "utilities";
// import styles from "../cellRenderer.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const RuleStatusRenderer = ({ value, context: { t } }) => (
    <div
        className={joinClasses(
            css("pcs-renderer-cell"),
            value === "Enabled" ? css("highlight") : ""
        )}
    >
        <div className={css("pcs-renderer-text")}>{value}</div>
    </div>
);
