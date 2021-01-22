// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import {
    ComponentArray,
    SidePanelContainer as SidePanel,
    PageContent,
} from "components/shared";
import { Route, Redirect, Switch, NavLink } from "react-router-dom";
import { PageNotFoundContainer as PageNotFound } from "components/shell/pageNotFound";

import "./administration.scss";

export class Administration extends Component {
    constructor(props) {
        super(props);
        this.state = {
            isNavExpanded: true,
            currentLogo: this.props.logo,
            currentApplicationName: this.props.name,
            edit: false,
            previewLogo: this.props.logo,
            newLogoName: undefined,
            isDefaultLogo: this.props.isDefaultLogo,
            validating: false,
            isValidFile: false,
        };
    }

    handleNavToggle = (e) => {
        e && e.stopPropagation();
        this.setState({
            isNavExpanded: !this.state.isNavExpanded,
        });
    };

    render() {
        const { t } = this.props,
            pagesConfig = [
                {
                    to: "/admin/test",
                    labelId: "test",
                    component: PageNotFound,
                },
                {
                    to: "/admin/test1",
                    labelId: "test1",
                    component: PageNotFound,
                },
                {
                    to: "/admin/test2",
                    labelId: "test2",
                    component: PageNotFound,
                },
            ];
        return (
            <section className="admin-container">
                <SidePanel
                    isExpanded={this.state.isNavExpanded}
                    onClick={this.handleNavToggle}
                    titleName="Administration"
                    t={t}
                >{pagesConfig.map((tabProps, i) => {
                    const label = t(tabProps.labelId);
                    return (
                        <NavLink
                            key={i}
                            to={tabProps.to}
                            className="global-side-panel-item"
                            activeClassName="global-side-panel-item-active"
                            title={label}
                            id={tabProps.labelId}
                        >
                            <div className="global-side-panel-item-text">
                                {label}
                            </div>
                        </NavLink>
                    );
                })}
                </SidePanel>
                {pagesConfig && (
                    <Switch>
                        <Redirect
                            exact
                            from="/admin"
                            to={pagesConfig[0].to}
                        />
                        {pagesConfig.map(({ to, component }) => (
                            <Route
                                exact
                                key={to}
                                path={to}
                                component={component}
                            />
                        ))}
                    </Switch>
                )}
            </section>
        );
    }
}
