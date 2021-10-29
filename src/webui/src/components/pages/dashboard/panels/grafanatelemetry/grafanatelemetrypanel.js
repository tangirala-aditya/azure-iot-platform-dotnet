// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import {
    Panel,
    PanelContent,
    PanelHeader,
    PanelHeaderLabel,
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
        this.setState({ from: getIntervalParams(props.timeInterval) });
        this.setState({ deviceGroupId: (props.activeDeviceGroup || {}).id });
    }

    render() {
        const { t, grafanaUrl, grafanaOrgId, user } = this.props,
            { deviceGroupId, from } = this.state;
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
                        src={`${Config.serviceUrls.grafana}d/${grafanaUrl}?from=${from}&to=now&orgId=${grafanaOrgId}&var-deviceGroupId=${deviceGroupId}&theme=light&refresh=10s&kiosk`}
                        width="100%"
                        height="100%"
                        frameborder="0"
                    ></iframe>
                    <iframe
                        title="Dashboard"
                        src={`${Config.serviceUrls.grafana}login/?authHeader=${user.token}`}
                        width="0"
                        height="0"
                        frameborder="0"
                    ></iframe>
                </PanelContent>
            </Panel>
        );
    }
}
