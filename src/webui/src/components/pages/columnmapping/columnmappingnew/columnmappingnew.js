// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { PageContent, PageTitle } from "components/shared";
import { LinkedComponent } from "utilities";
import { ColumnMapper } from "../columnmapper";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../summary.module.scss"));
const columnMappingCss = classnames.bind(require("../mapping.module.scss"));

export class ColumnMappingNew extends LinkedComponent {
    constructor(props) {
        super(props);
        debugger;
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
        console.log(nextProps);
    }

    render() {
        return (
            <PageContent
                className={`${columnMappingCss("mapping-container")}  ${css(
                    "summary-container"
                )}`}
            >
                <div>
                    <PageTitle
                        titleValue="Add/Edit Column Mapping"
                        descriptionValue="Map the Device Properties to Columns for displaying in Grid"
                    />
                    <ColumnMapper isEdit={true} {...this.props} />
                </div>
            </PageContent>
        );
    }
}
