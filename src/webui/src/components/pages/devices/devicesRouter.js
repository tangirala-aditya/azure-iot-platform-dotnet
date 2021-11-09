// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Route, Redirect, Switch } from "react-router-dom";
import { DevicesContainer } from "./devices.container";
import { DeviceTelemetryContainer } from "./deviceTelemetry/deviceTelemetry.container";
import { EdgeDevicesContainer } from "./edgeDevices/edgeDevices.container";
import { ModuleDetailsContainer } from "./flyouts/moduleDetails/moduleDetails.container";

export const DevicesRouter = () => (
    <Switch>
        <Route
            exact
            path={"/devices"}
            render={(routeProps) => (
                <DevicesContainer {...routeProps} routeProps={routeProps} />
            )}
        />
        <Route
            exact
            path={"/devices/telemetry"}
            render={(routeProps) => (
                <DeviceTelemetryContainer {...routeProps} />
            )}
        />
        <Route
            exact
            path={"/devices/modulesLogs/:deviceId/:moduleId"}
            render={(routeProps) => <ModuleDetailsContainer {...routeProps} />}
        />
        <Route
            exact
            path={"/deviceSearch"}
            render={(routeProps) => (
                <DevicesContainer {...routeProps} routeProps={routeProps} />
            )}
        />
        <Route
            exact
            path={"/deviceSearch/telemetry"}
            render={(routeProps) => (
                <DeviceTelemetryContainer {...routeProps} />
            )}
        />
        <Route
            exact
            path={"/edge-devices"}
            render={(routeProps) => <EdgeDevicesContainer {...routeProps} />}
        />
        <Redirect to="/devices" />
    </Switch>
);
