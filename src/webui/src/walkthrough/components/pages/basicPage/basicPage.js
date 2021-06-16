// Copyright (c) Microsoft. All rights reserved.

// <page>
import React, { Component } from "react";

import { PageContent } from "components/shared";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./basicPage.module.scss"));

export class BasicPage extends Component {
    render() {
        const { t } = this.props;
        return (
            <PageContent className={css("basic-page-container")}>
                {t("walkthrough.basicPage.pagePlaceholder")}
            </PageContent>
        );
    }
}

// </page>
