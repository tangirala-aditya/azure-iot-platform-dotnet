// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { AlertGrid } from "components/pages/maintenance/grids";
import { AjaxError } from "components/shared";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./summary.module.scss"));
const maintenanceCss = classnames.bind(require("../maintenance.module.scss"));

export const Notifications = ({
    isPending,
    alerts,
    history,
    error,
    ...props
}) => {
    const gridProps = {
        ...props,
        rowData: isPending ? undefined : alerts,
        onRowClicked: ({ data: { ruleId } }) =>
            history.push(`/maintenance/rule/${ruleId}`),
    };
    return !error ? (
        !isPending && alerts.length === 0 ? (
            <div className={css("no-data-msg")}>
                {props.t("maintenance.noData")}
            </div>
        ) : (
            <AlertGrid {...gridProps} />
        )
    ) : (
        <AjaxError
            t={props.t}
            error={error}
            className={maintenanceCss("padded-error")}
        />
    );
};
