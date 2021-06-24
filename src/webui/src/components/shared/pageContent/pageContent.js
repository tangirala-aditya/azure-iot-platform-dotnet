// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./pageContent.module.scss"));

/** A presentational component containing the content for a page */
export const PageContent = ({ className, children }) => (
    <div className={css("page-content-container", className)}>{children}</div>
);
