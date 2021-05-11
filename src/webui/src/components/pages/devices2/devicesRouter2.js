// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Route, Redirect, Switch } from "react-router-dom";
import { DevicesContainer } from "./devices.container";
import { DeviceTelemetryContainer } from "./deviceTelemetry/deviceTelemetry.container";

export const DevicesRouter2 = () => (
    <Switch>
        <Route
            exact
            path={"/devices2"}
            render={(routeProps) => (
                <DevicesContainer {...routeProps} routeProps={routeProps} />
            )}
        />
        <Route
            exact
            path={"/devices2/telemetry"}
            render={(routeProps) => (
                <DeviceTelemetryContainer {...routeProps} />
            )}
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
        <Redirect to="/devices2" />
    </Switch>
);
