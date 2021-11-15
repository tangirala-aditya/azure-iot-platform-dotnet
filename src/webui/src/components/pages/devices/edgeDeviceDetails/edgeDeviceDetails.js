// Copyright (c) Microsoft. All rights reserved.

import React, { Fragment } from "react";
import { IoTHubManagerService } from "services";
import { NavLink } from "react-router-dom";
import { Route, Switch } from "react-router-dom";
import { LinkedComponent, svgs } from "utilities";
import {
    ComponentArray,
    Btn,
    ContextMenu,
    ContextMenuAlign,
    AjaxError,
    PageContent,
} from "components/shared";
import { ModuleDetailsContainer } from "../flyouts/moduleDetails/moduleDetails.container";
import { EdgeDeviceInfoDashboard } from "./edgeDeviceInfoDashboard";
const classnames = require("classnames/bind");
const css = classnames.bind(require("./edgeDeviceDetails.module.scss"));

export class EdgeDeviceDetails extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = {
            edgeModules: undefined,
            edgeDeviceStatus: 404,
            edgeDeviceStatusPending: true,
            error: undefined,
            deviceId: props.match.params.deviceId,
            moduleId: props.location.state.moduleId,
        };
    }

    componentDidMount() {
        this.fetchEdgeDeviceStatus(this.state.deviceId);
    }

    fetchEdgeDeviceStatus = (deviceId) => {
        IoTHubManagerService.getEdgeDeviceStatus(deviceId).subscribe(
            (response) => {
                this.setState(
                    {
                        edgeDeviceStatus: response.status,
                        edgeDeviceStatusPending: false,
                    },
                    (error) => {
                        this.setState({
                            error: error,
                        });
                    }
                );
            }
        );
    };
    navigateToDevices = () => {
        this.props.history.push("/devices");
    };

    render() {
        const { t } = this.props;
        const { error } = this.state;
        const isEdgeDeviceActive =
                this.state.edgeDeviceStatus === 200 ? true : false,
            edgeDeviceStatusPending = this.state.edgeDeviceStatusPending;

        return (
            <ComponentArray>
                <ContextMenu>
                    <ContextMenuAlign left={true}>
                        <Btn svg={svgs.return} onClick={this.navigateToDevices}>
                            {t("devices.returnToDevices")}
                        </Btn>
                    </ContextMenuAlign>
                </ContextMenu>
                {!isEdgeDeviceActive && !edgeDeviceStatusPending && (
                    <center>
                        <h1>Device is Offline</h1>
                        <h2>Please try after sometime.</h2>
                    </center>
                )}
                {isEdgeDeviceActive && (
                    <Fragment>
                        <PageContent
                            className={`${css("maintenance-container")}  ${css(
                                "summary-container"
                            )}`}
                        >
                            <div className={css("tab-container")}>
                                <NavLink
                                    to={`/devices/modulelogs/${this.state.deviceId}`}
                                    className={css("tab")}
                                    activeClassName={css("active")}
                                >
                                    {this.props.t(
                                        "edgeDeviceDetails.moduleInfo"
                                    )}
                                </NavLink>
                                <NavLink
                                    to={`/devices/deviceinfo/${this.state.deviceId}`}
                                    className={css("tab")}
                                    activeClassName={css("active")}
                                >
                                    {this.props.t(
                                        "edgeDeviceDetails.deviceInfo"
                                    )}
                                </NavLink>
                            </div>
                            <div className={css("grid-container")}>
                                <Switch>
                                    <Route
                                        exact
                                        path={"/devices/modulelogs/:deviceId"}
                                        render={() => (
                                            <div>
                                                <ModuleDetailsContainer
                                                    deviceId={
                                                        this.state.deviceId
                                                    }
                                                    moduleId={
                                                        this.state.moduleId
                                                    }
                                                />
                                            </div>
                                        )}
                                    />
                                    <Route
                                        exact
                                        path={"/devices/deviceinfo/:deviceId"}
                                        render={() => (
                                            <EdgeDeviceInfoDashboard
                                                deviceId={this.state.deviceId}
                                                t={this.props.t}
                                            />
                                        )}
                                    />
                                </Switch>
                            </div>
                        </PageContent>
                        {error && (
                            <AjaxError
                                className="devices-new-error"
                                t={t}
                                error={error}
                            />
                        )}
                    </Fragment>
                )}
            </ComponentArray>
        );
    }
}
