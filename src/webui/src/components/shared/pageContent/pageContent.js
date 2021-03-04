// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";

// import styles from "./pageContent.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./pageContent.module.scss"));

/** A presentational component containing the content for a page */
export const PageContent = ({ className, children }) => (
    <div className={joinClasses(css("page-content-container"), className)}>
        {children}
    </div>
);
