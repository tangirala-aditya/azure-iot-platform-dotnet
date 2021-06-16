// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { JobGrid } from "components/pages/maintenance/grids";
import { AjaxError } from "components/shared";
const classnames = require("classnames/bind");
const css = classnames.bind(require("./summary.module.scss"));
const maintenanceCss = classnames.bind(require("../maintenance.module.scss"));

export const Jobs = ({ isPending, jobs, history, error, ...props }) => {
    const gridProps = {
        ...props,
        rowData: isPending ? undefined : jobs,
        onRowClicked: ({ data: { jobId } }) =>
            history.push(`/maintenance/job/${jobId}`),
    };
    return !error ? (
        !isPending && jobs.length === 0 ? (
            <div className={css("no-data-msg")}>
                {props.t("maintenance.noData")}
            </div>
        ) : (
            <JobGrid {...gridProps} />
        )
    ) : (
        <AjaxError
            t={props.t}
            error={error}
            className={maintenanceCss("padded-error")}
        />
    );
};
