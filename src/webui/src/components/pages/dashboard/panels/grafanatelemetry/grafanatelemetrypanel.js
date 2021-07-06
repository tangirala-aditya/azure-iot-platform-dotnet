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

const classnames = require("classnames/bind");
const css = classnames.bind(require("../telemetry/telemetryPanel.module.scss"));

export class GrafanaTelemetryPanel extends Component {
    constructor(props) {
        super(props);

        this.state = { deviceurl: "blank" };
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
        this.prepareUrl(nextProps);
    }

    prepareUrl(props) {
        const deviceIds = Object.keys(props.devices);
        if (deviceIds.length > 0) {
            var combinedUrl =
                "var-deviceid=" + deviceIds.join("&var-deviceid=");
            console.log(combinedUrl);
            this.setState({ deviceurl: combinedUrl });
        } else {
            this.setState({ deviceurl: "blank" });
        }
    }

    render() {
        const { t, isPending, lastRefreshed, error } = this.props,
            { deviceurl } = this.state,
            showOverlay = isPending && !lastRefreshed;
        return (
            <Panel>
                <PanelHeader>
                    <PanelHeaderLabel>
                        {t("dashboard.panels.telemetry.header")}
                    </PanelHeaderLabel>
                </PanelHeader>
                <PanelContent className={css("telemetry-panel-container")}>
                    <iframe
                        title="Telemetry"
                        src={`http://localhost:8080/grafana/d-solo/Sb-VAjknk/telemetry?orgId=1&${deviceurl}&theme=light&from=now-1h&to=now&refresh=1m&panelId=2`}
                        width="800"
                        height="400"
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
