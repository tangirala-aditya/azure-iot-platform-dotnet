// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { PageContent, PageTitle } from "components/shared";
import { LinkedComponent } from "utilities";
import { ColumnMapperContainer } from "../columnmapper.container";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../summary.module.scss"));
const columnMappingCss = classnames.bind(require("../mapping.module.scss"));

export class ColumnMappingNew extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = {
            mappingId: this.props.isEdit ? this.props.match.params.id : "",
        };
    }

    UNSAFE_componentWillReceiveProps(nextProps) {}

    render() {
        const { mappingId } = this.state;
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
                    <ColumnMapperContainer
                        mappingId={mappingId}
                        {...this.props}
                    />
                </div>
            </PageContent>
        );
    }
}
