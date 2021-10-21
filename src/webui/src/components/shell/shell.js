// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { Route, Redirect, Switch, NavLink } from "react-router-dom";
import { Trans } from "react-i18next";
import { Shell as FluentShell } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Shell";

// App Components
import Main from "./main/main";
import { PageNotFoundContainer as PageNotFound } from "./pageNotFound";
import { Svg, Protected } from "components/shared";
import { permissions } from "services/models";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./shell.module.scss"));

/** The base component for the app shell */
class Shell extends Component {
    constructor(props) {
        super(props);

        this.state = {
            isNavExpanded: true,
            isMastheadMoreExpanded: false,
        };
    }

    componentDidMount() {
        const { history, registerRouteEvent } = this.props;
        // Initialize listener to inject the route change event into the epic action stream
        history.listen(({ pathname }) => registerRouteEvent(pathname));
    }

    render() {
        const { pagesConfig, theme, children, denyAccess } = this.props;
        return (
            <FluentShell
                theme={theme}
                isRtl={false}
                navigation={this.getNavProps()}
                masthead={this.getMastheadProps()}
            >
                {denyAccess && (
                    <div className={css("app")}>
                        <Main>
                            <div className={css("access-denied")}>
                                <Trans i18nKey={"accessDenied.message"}>
                                    You don't have permissions.
                                </Trans>
                            </div>
                            {children}
                        </Main>
                    </div>
                )}
                {!denyAccess && pagesConfig && (
                    <div className={css("app")}>
                        <Main>
                            <Switch>
                                <Redirect
                                    exact
                                    from="/"
                                    to={pagesConfig[0].to}
                                />
                                {pagesConfig.map(({ to, exact, component }) => (
                                    <Route
                                        key={to}
                                        exact={exact}
                                        path={to}
                                        component={component}
                                    />
                                ))}
                                <Route component={PageNotFound} />
                            </Switch>
                            {children}
                        </Main>
                    </div>
                )}
            </FluentShell>
        );
    }

    getNavProps() {
        const { pagesConfig, t, denyAccess, limitedAccess } = this.props;
        if (denyAccess || !pagesConfig || limitedAccess) {
            return null;
        }

        return {
            isExpanded: this.state.isNavExpanded,
            onClick: this.handleGlobalNavToggle,
            attr: {
                navButton: {
                    title: t(
                        this.state.isNavExpanded
                            ? "globalNav.collapse"
                            : "globalNav.expand"
                    ),
                },
            },
            children: pagesConfig.map((tabProps, i) => {
                const label = t(tabProps.labelId);
                return (
                    <Protected
                        permission={tabProps.permission ?? permissions.readAll}
                    >
                        <NavLink
                            key={i}
                            to={tabProps.to}
                            className={css("global-nav-item")}
                            activeClassName="global-nav-item-active"
                            title={label}
                            id={tabProps.labelId}
                        >
                            <Svg
                                src={tabProps.svg}
                                className={css("global-nav-item-icon")}
                            />
                            <div className={css("global-nav-item-text")}>
                                {label}
                            </div>
                        </NavLink>
                    </Protected>
                );
            }),
        };
    }

    getMastheadProps() {
        const {
            pagesConfig,
            t,
            denyAccess,
            openSystemSettings,
            openUserProfile,
            openHelpFlyout,
            openFlyout,
        } = this.props;

        let toolbarItems = [];

        toolbarItems.push({
            icon: "contact",
            label: t("profileFlyout.title"),
            selected: openFlyout === "profile",
            onClick: openUserProfile,
        });

        if (denyAccess) {
            return {
                branding: this.getMastheadBranding(),
                more: {
                    title: t("header.more"),
                    selected: this.state.isMastheadMoreExpanded,
                    onClick: this.handleMastheadMoreToggle,
                },
                toolbarItems: toolbarItems,
            };
        } else if (pagesConfig) {
            if (!this.props.limitedAccess) {
                toolbarItems.unshift({
                    icon: "settings",
                    label: t("settingsFlyout.title"),
                    selected: openFlyout === "settings",
                    onClick: openSystemSettings,
                });
            }

            toolbarItems.unshift({
                icon: "help",
                label: t("helpFlyout.title"),
                selected: openFlyout === "help",
                onClick: openHelpFlyout,
            });

            return {
                branding: this.getMastheadBranding(),
                more: {
                    title: t("header.more"),
                    selected: this.state.isMastheadMoreExpanded,
                    onClick: this.handleMastheadMoreToggle,
                },
                toolbarItems: toolbarItems,
            };
        }
        return null; // no masthead
    }

    getMastheadBranding() {
        const { appName, appLogo, getLogoError, isDefaultLogo, t } = this.props;
        return (
            <div className={css("nav-item")}>
                {(isDefaultLogo || getLogoError) && (
                    <Svg src={appLogo} className={css("nav-item-icon")} />
                )}
                {!isDefaultLogo && (
                    <div className={css("nav-item-icon")}>
                        <img src={appLogo} alt="Logo" />
                    </div>
                )}
                <div className={css("nav-item-text")}>{t(appName)}</div>
            </div>
        );
    }

    handleGlobalNavToggle = (e) => {
        e && e.stopPropagation();
        this.setState({
            isNavExpanded: !this.state.isNavExpanded,
        });
    };

    handleMastheadMoreToggle = (e) => {
        e && e.stopPropagation();
        this.setState({
            isMastheadMoreExpanded: !this.state.isMastheadMoreExpanded,
        });
    };
}

export default Shell;
