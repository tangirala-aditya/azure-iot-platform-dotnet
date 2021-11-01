// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { AjaxError } from "components/shared";
import { DeviceJobGrid } from "../grids/deviceJobGrid";
const classnames = require("classnames/bind");
const css = classnames.bind(require("./summary.module.scss"));
const maintenanceCss = classnames.bind(require("../maintenance.module.scss"));

export const DeviceJobs = ({
    isPending,
    linkedJobs,
    history,
    error,
    ...props
}) => {
    const gridProps = {
        ...props,
        rowData: isPending ? undefined : linkedJobs,
        onRowClicked: ({ data: { jobId } }) =>
            history.push(`/maintenance/deviceJob/${jobId}`),
    };
    return !error ? (
        !isPending && linkedJobs !== undefined && linkedJobs?.length === 0 ? (
            <div className={css("no-data-msg")}>
                {props.t("maintenance.noData")}
            </div>
        ) : (
            <DeviceJobGrid {...gridProps} />
        )
    ) : (
        <AjaxError
            t={props.t}
            error={error}
            className={maintenanceCss("padded-error")}
        />
    );
};
