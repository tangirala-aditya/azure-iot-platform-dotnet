// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import {
    ComponentArray,
    ContextMenu,
    ContextMenuAlign,
    PageContent,
    PageTitle,
    RefreshBarContainer as RefreshBar,
} from "components/shared";

import { TimeIntervalDropdownContainer as TimeIntervalDropdown } from "components/shell/timeIntervalDropdown";

import { toDiagnosticsModel } from "services/models";
import { DeviceJobGrid } from "../grids/deviceJobGrid";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../maintenance.module.scss"));

export class DeviceJobDetail extends Component {
    constructor(props) {
        super(props);
        debugger;
        this.state = {
            selectedDeviceJob: undefined,
            selectedDevices: undefined,
            contextBtns: undefined,
            refreshPending: true,
        };
    }

    componentDidMount() {
        debugger;
        this.handleNewProps(this.props);
        this.clearSubscription();
        this.props.logEvent(toDiagnosticsModel("JobDetails_Click", {}));
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
        this.handleNewProps(nextProps);
    }

    handleNewProps(nextProps) {
        if (
            (nextProps.match.params.id !==
                (this.state.selectedDeviceJob || {}).jobId ||
                this.state.timeIntervalChangePending ||
                this.state.refreshPending) &&
            nextProps.linkedJobs.length
        ) {
            const selectedDeviceJob = nextProps.linkedJobs.filter(
                ({ jobId }) => jobId === nextProps.match.params.id
            )[0];
            this.setState({
                selectedDeviceJob,
                refreshPending: false,
                timeIntervalChangePending: false,
            });
        }
    }

    clearSubscription() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    refreshData = () => {
        this.setState({ selectedDeviceJob: undefined, refreshPending: true });
        this.props.refreshData();
    };

    onContextMenuChange = (contextBtns) => this.setState({ contextBtns });

    onTimeIntervalChange = (timeInterval) => {
        this.setState({
            selectedDeviceJob: undefined,
            timeIntervalChangePending: true,
        });
        this.props.onTimeIntervalChange(timeInterval);
    };

    render() {
        const { isPending, lastUpdated, t, timeInterval } = this.props,
            selectedDeviceJob = this.state.selectedDeviceJob,
            deviceJobGridProps = {
                domLayout: "autoHeight",
                rowData: isPending
                    ? undefined
                    : selectedDeviceJob
                    ? [selectedDeviceJob]
                    : [],
                pagination: false,
                t,
                onColumnMoved: this.props.onColumnMoved,
            };

        return (
            <ComponentArray>
                <ContextMenu>
                    <ContextMenuAlign left={false}>
                        {this.state.contextBtns}
                        <TimeIntervalDropdown
                            onChange={this.onTimeIntervalChange}
                            value={timeInterval}
                            t={t}
                        />
                        <RefreshBar
                            refresh={this.refreshData}
                            time={lastUpdated}
                            isPending={isPending}
                            t={t}
                            isShowIconOnly={true}
                        />
                    </ContextMenuAlign>
                </ContextMenu>
                <PageContent className={css("maintenance-container")}>
                    <PageTitle
                        titleValue={
                            selectedDeviceJob ? selectedDeviceJob.jobId : ""
                        }
                    />
                    <div>
                        <DeviceJobGrid {...deviceJobGridProps} />
                    </div>
                </PageContent>
            </ComponentArray>
        );
    }
}
