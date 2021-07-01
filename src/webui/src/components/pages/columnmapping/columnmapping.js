// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Route, Redirect, Switch } from "react-router-dom";
import { NavLink } from "react-router-dom";

import { PageContent, PageTitle } from "components/shared";
import { LinkedComponent } from "utilities";
import { ColumnMapperContainer } from "./columnmapper.container";
import { ColumnMappingsGridContainer } from "../columnmapping/columnmappinggrid/columnMappingGrid.container";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./summary.module.scss"));
const columnMappingCss = classnames.bind(require("./mapping.module.scss"));

export class ColumnMapping extends LinkedComponent {
    constructor(props) {
        super(props);
    }

    UNSAFE_componentWillReceiveProps(nextProps) {}

    render() {
        return (
            <PageContent
                className={`${columnMappingCss("mapping-container")}  ${css(
                    "summary-container"
                )}`}
            >
                <div>
                    <PageTitle
                        titleValue="Column Mapping"
                        descriptionValue="Map the Device Properties to Columns for displaying in Grid"
                    />
                    <div className={columnMappingCss("tab-container")}>
                        <NavLink
                            to={"/columnMapping/default"}
                            className={columnMappingCss("tab")}
                            activeClassName={columnMappingCss("active")}
                        >
                            {this.props.t("Default")}
                        </NavLink>
                        <NavLink
                            to={"/columnMapping/custom"}
                            className={columnMappingCss("tab")}
                            activeClassName={columnMappingCss("active")}
                        >
                            {this.props.t("Custom")}
                        </NavLink>
                    </div>
                </div>
                <div className={css("grid-container")}>
                    <Switch>
                        <Route
                            exact
                            path={"/columnMapping/default"}
                            render={() => (
                                <ColumnMapperContainer
                                    mappingId={"Default"}
                                    isDefault={true}
                                    {...this.props}
                                />
                            )}
                        />
                        <Route
                            exact
                            path={"/columnMapping/custom"}
                            render={() => (
                                <ColumnMappingsGridContainer {...this.props} />
                            )}
                        />
                        <Redirect to="/columnMapping/default" />
                    </Switch>
                </div>
            </PageContent>
        );
    }
}
