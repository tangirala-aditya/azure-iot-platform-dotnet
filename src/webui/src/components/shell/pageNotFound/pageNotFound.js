// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { PageContent } from "components/shared";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./pageNotFound.module.scss"));

export const PageNotFound = ({ t }) => (
    <PageContent className={css("page-not-found-container")}>
        {t("pageNotFound.title")}
        <br />
        <br />
        <span className={css("quote")}>{t("pageNotFound.message")}</span>
    </PageContent>
);
