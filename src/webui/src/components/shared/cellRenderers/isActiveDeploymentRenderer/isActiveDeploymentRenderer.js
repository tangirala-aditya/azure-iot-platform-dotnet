// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import joinClasses from "utilities";
// import styles from "../cellRenderer.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../cellRenderer.module.scss"));

export const IsActiveDeploymentRenderer = ({ value, context: { t } }) => (
    <div className={joinClasses(css("pcs-renderr-cell"), css("highlight"))}>
        {value ? (
            <div className="small-green-circle"></div>
        ) : (
            <div className="small-black-circle"></div>
        )}
    </div>
);
