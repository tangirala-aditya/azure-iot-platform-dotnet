// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";
// import styles from "./statPropertyPair.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./statPropertyPair.module.scss"));

/** A presentational component containing statistics value, label and icon */
export const StatPropertyPair = ({ label, value, className }) => {
    return (
        <div className={joinClasses(css("stat-property-pair"), className)}>
            <div className={css("stat-property-pair-label")}>{label}</div>
            <div className={css("stat-property-pair-value")}>{value}</div>
        </div>
    );
};
