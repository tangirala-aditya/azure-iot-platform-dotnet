// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { Subject } from "rxjs";
import Config from "app.config";
import { IdentityGatewayService } from "services";
import { permissions } from "services/models";
import { getDeviceGroupParam, getTenantIdParam } from "utilities";
import { Grid, Cell } from "./grid";
import { DeviceGroupDropdownContainer as DeviceGroupDropdown } from "components/shell/deviceGroupDropdown";
import { ManageDeviceGroupsBtnContainer as ManageDeviceGroupsBtn } from "components/shell/manageDeviceGroupsBtn";
import { TimeIntervalDropdownContainer as TimeIntervalDropdown } from "components/shell/timeIntervalDropdown";
import { ResetActiveDeviceQueryBtnContainer as ResetActiveDeviceQueryBtn } from "components/shell/resetActiveDeviceQueryBtn";
import { GrafanaTelemetryPanel, ExamplePanel } from "./panels";
import {
    ComponentArray,
    ContextMenu,
    ContextMenuAlign,
    PageContent,
    Protected,
    RefreshBarContainer as RefreshBar,
} from "components/shared";
import { CreateDeviceQueryBtnContainer as CreateDeviceQueryBtn } from "components/shell/createDeviceQueryBtn";

import { HttpClient } from "utilities/httpClient";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./dashboard.module.scss"));

const initialState = {
        lastRefreshed: undefined,
        selectedDeviceGroupId: undefined,
    },
    refreshEvent = (deviceIds = [], timeInterval) => ({
        deviceIds,
        timeInterval,
    });

export class GrafanaDashboard extends Component {
    constructor(props) {
        super(props);

        this.state = initialState;

        this.subscriptions = [];
        this.dashboardRefresh$ = new Subject();

        this.props.updateCurrentWindow("Dashboard");
    }

    UNSAFE_componentWillMount() {
        const redirectUrl = HttpClient.getLocalStorageValue("redirectUrl");
        HttpClient.removeLocalStorageItem("redirectUrl");
        if (redirectUrl) {
            window.location.href = redirectUrl;
        }

        if (this.props.location && this.props.location.search) {
            const tenantId = getTenantIdParam(this.props.location);
            this.props.checkTenantAndSwitch({
                tenantId: tenantId,
                redirectUrl: window.location.href,
            });
            this.setState({
                selectedDeviceGroupId: getDeviceGroupParam(this.props.location),
            });
        }
        IdentityGatewayService.VerifyAndRefreshCache();
    }

    componentDidMount() {
        if (this.state.selectedDeviceGroupId && this.props.location) {
            window.history.replaceState(
                {},
                document.title,
                this.props.location.pathname
            );
        }

        this.subscriptions.push(
            this.dashboardRefresh$.subscribe(() => this.setState(initialState))
        );

        // Start polling all panels
        if (this.props.deviceLastUpdated) {
            this.dashboardRefresh$.next(
                refreshEvent(
                    Object.keys(this.props.devices || {}),
                    this.props.timeInterval
                )
            );
        }
    }

    componentWillUnmount() {
        this.subscriptions.forEach((sub) => sub.unsubscribe());
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
        if (
            nextProps.deviceLastUpdated !== this.props.deviceLastUpdated ||
            nextProps.timeInterval !== this.props.timeInterval
        ) {
            this.dashboardRefresh$.next(
                refreshEvent(
                    Object.keys(nextProps.devices),
                    nextProps.timeInterval
                )
            );
        }
    }

    refreshDashboard = () =>
        this.dashboardRefresh$.next(
            refreshEvent(
                Object.keys(this.props.devices),
                this.props.timeInterval
            )
        );

    render() {
        const {
                timeInterval,
                devicesIsPending,
                activeDeviceGroup,
                t,
                grafanaUrl,
                grafanaOrgId,
                user,
            } = this.props,
            { lastRefreshed } = this.state;

        return (
            <ComponentArray>
                <ContextMenu>
                    <ContextMenuAlign left={true}>
                        <DeviceGroupDropdown
                            deviceGroupIdFromUrl={
                                this.state.selectedDeviceGroupId
                            }
                        />
                        <Protected permission={permissions.updateDeviceGroups}>
                            <ManageDeviceGroupsBtn />
                        </Protected>
                        {this.props.activeDeviceQueryConditions.length !== 0 ? (
                            <>
                                <CreateDeviceQueryBtn />
                                <ResetActiveDeviceQueryBtn />
                            </>
                        ) : null}
                    </ContextMenuAlign>
                    <ContextMenuAlign>
                        <TimeIntervalDropdown
                            onChange={this.props.updateTimeInterval}
                            value={timeInterval}
                            activeDeviceGroup={activeDeviceGroup}
                            t={t}
                        />
                        <RefreshBar
                            refresh={this.refreshDashboard}
                            time={lastRefreshed}
                            isPending={devicesIsPending}
                            t={t}
                            isShowIconOnly={true}
                        />
                    </ContextMenuAlign>
                </ContextMenu>
                <PageContent className={css("dashboard-container")}>
                    <Grid>
                        <Cell className={css("col-9")}>
                            <GrafanaTelemetryPanel
                                t={t}
                                grafanaUrl={grafanaUrl}
                                grafanaOrgId={grafanaOrgId}
                                activeDeviceGroup={activeDeviceGroup}
                                timeInterval={timeInterval}
                                user={user}
                            />
                        </Cell>
                        {Config.showWalkthroughExamples && (
                            <Cell className={css("col-4")}>
                                <ExamplePanel t={t} />
                            </Cell>
                        )}
                    </Grid>
                </PageContent>
            </ComponentArray>
        );
    }
}
