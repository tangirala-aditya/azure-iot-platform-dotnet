// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Route, Redirect, Switch } from "react-router-dom";
import { ColumnMappingNewContainer } from "./columnmappingnew/columnmappingnew.container";
import { ColumnMappingContainer } from "./columnmapping.container";

export const ColumnMappingsRouter = () => (
    <Switch>
        <Route
            exact
            path={"/columnMapping/edit/:id"}
            render={(routeProps) => (
                <ColumnMappingNewContainer isEdit={true} {...routeProps} />
            )}
        />
        <Route
            exact
            path={"/columnMapping/add"}
            render={(routeProps) => (
                <ColumnMappingNewContainer isAdd={true} {...routeProps} />
            )}
        />
        <Route
            path={"/columnMapping"}
            render={(routeProps) => <ColumnMappingContainer {...routeProps} />}
        />
        <Redirect to="/columnMapping" />
    </Switch>
);
