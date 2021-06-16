// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./pageTitle.module.scss"));

/** A presentational component containing the title for a page */
export const PageTitle = ({
    titleValue,
    descriptionValue,
    className,
    hearderClassName,
    descriptionClassName,
}) => (
    <div className={joinClasses(css("page-title"), className)}>
        <h1 className={joinClasses(css("page-title-header"), hearderClassName)}>
            {titleValue}
        </h1>
        {descriptionValue && (
            <h5 className={joinClasses(descriptionClassName)}>
                {descriptionValue}
            </h5>
        )}
    </div>
);
