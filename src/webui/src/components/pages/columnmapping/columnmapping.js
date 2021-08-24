// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Route, Redirect, Switch } from "react-router-dom";
import { NavLink } from "react-router-dom";

import {
    ComponentArray,
    PageContent,
    PageTitle,
    ContextMenu,
} from "components/shared";
import { LinkedComponent } from "utilities";
import { ColumnMapperContainer } from "./columnmapper.container";
import { ColumnMappingsGridContainer } from "../columnmapping/columnmappinggrid/columnMappingGrid.container";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./summary.module.scss"));
const columnMappingCss = classnames.bind(require("./mapping.module.scss"));

const closedFlyoutState = { openFlyoutName: undefined };
export class ColumnMapping extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = {
            ...closedFlyoutState,
            contextBtns: null,
            isDefault: true,
        };
    }

    onContextMenuChange = (contextBtns, isDefault = false) =>
        this.setState({
            contextBtns,
            isDefault,
        });

    onTabChange = (isDefault) => this.setState({ isDefault });

    render() {
        return (
            <ComponentArray>
                <ContextMenu>
                    {!this.state.isDefault && this.state.contextBtns}
                </ContextMenu>
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
                                onClick={() => this.onTabChange(true)}
                            >
                                {this.props.t("Default")}
                            </NavLink>
                            <NavLink
                                to={"/columnMapping/custom"}
                                className={columnMappingCss("tab")}
                                activeClassName={columnMappingCss("active")}
                                onClick={() => this.onTabChange(false)}
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
                                    <ColumnMappingsGridContainer
                                        {...this.props}
                                        onContextMenuChange={
                                            this.onContextMenuChange
                                        }
                                    />
                                )}
                            />
                            <Redirect to="/columnMapping/default" />
                        </Switch>
                    </div>
                </PageContent>
            </ComponentArray>
        );
    }
}
