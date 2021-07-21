// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import { AjaxError, Indicator } from "components/shared";
import {
    Panel,
    PanelContent,
    PanelError,
    PanelHeader,
    PanelHeaderLabel,
    PanelOverlay,
} from "components/pages/dashboard/panel";
import Config from "app.config";
import "./grafana.scss";
const classnames = require("classnames/bind");
const css = classnames.bind(require("../telemetry/telemetryPanel.module.scss"));
export const getIntervalParams = (timeInterval) => {
    switch (timeInterval) {
        case "PT15M":
            return "now-15m";
        case "P1D":
            return "now-1d";
        case "P7D":
            return "now-7d";
        case "P1M":
            return "now-1M";
        default:
            // Use PT1H as the default case
            return "now-1h";
    }
};

export class GrafanaTelemetryPanel extends Component {
    constructor(props) {
        super(props);
        this.state = { deviceGroupId: "default", from: "now-1h" };
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
        this.prepareUrl(nextProps);
    }

    prepareUrl(props) {
        console.log(props.activeDeviceGroup);
        this.setState({ from: getIntervalParams(props.timeInterval) });
        this.setState({ deviceGroupId: (props.activeDeviceGroup || {}).id });
    }

    render() {
        const { t, isPending, lastRefreshed, error } = this.props,
            { deviceGroupId, from } = this.state,
            showOverlay = isPending && !lastRefreshed;
        return (
            <Panel>
                <PanelHeader>
                    <PanelHeaderLabel>
                        {t("dashboard.panels.dashboard.header")}
                    </PanelHeaderLabel>
                </PanelHeader>
                <PanelContent className={css("telemetry-panel-container")}>
                    <iframe
                        title="Dashboard"
                        src={`${Config.serviceUrls.grafana}d/Jh8M7Yinz/sample-dashboard?from=${from}&to=now&orgId=1&var-deviceGroupId=${deviceGroupId}&theme=light&refresh=10s&kiosk`}
                        width="100%"
                        height="100%"
                        frameborder="0"
                    ></iframe>
                </PanelContent>
                {showOverlay && (
                    <PanelOverlay>
                        <Indicator />
                    </PanelOverlay>
                )}
                {error && (
                    <PanelError>
                        <AjaxError t={t} error={error} />
                    </PanelError>
                )}
            </Panel>
        );
    }
}
